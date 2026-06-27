# Plan de implementación: Globalización — RentaFácil

**Fecha:** 2026-06-26
**Estado:** en ejecución (después del merge de `feature/migracion-sqlserver`)
**Rama:** `feature/globalizacion` (creada desde `main`)
**Archivo destino en repo:** `docs/superpowers/plans/2026-06-26-globalizacion.md`

> **Nota de corrección (vs. la versión original del plan):** el borrador inicial usaba nombres de campo que no existen en el código (`Contrato.MontoMensual`, `Pago.Monto`). Los campos reales son `Contrato.Monto`/`Contrato.Garantia`, `Pago.TotalMonto`/`Pago.ACuenta`/`Pago.Servicios`, `Inmueble.MontoRenta`, `Unidad.MontoRenta`. Esta versión usa los nombres reales en todas las tasks.

---

## Contexto y prerequisitos

Este spec arranca **después** de que la migración SQL Server esté mergeada (ya lo está, `c6278ce`). Eso significa:

- La BD ya es SQL Server con schemas `auth`, `renta`, `config`, `audit`.
- Los 7 campos monetarios (`Contrato.Monto`/`Garantia`, `Pago.TotalMonto`/`ACuenta`/`Servicios`, `Inmueble.MontoRenta`, `Unidad.MontoRenta`) ya tienen `[Column(TypeName = "decimal(18,2)")]` — **no se vuelven a migrar aquí**.
- Los DTOs en `RentaFacil.Shared/Models/` (`ContratoDto`, `PagoDto`, etc.) **ya usan `decimal` puro**, nunca `string`, para dinero — verificado antes de escribir este plan.

Lo que este spec agrega encima de esa base: `InvariantCulture` en la API, `MoneyFormatter` centralizado, infraestructura `.resx` para multiidioma, y corrección del formateo de **visualización** en MAUI y QuestPDF.

**Hallazgo adicional (verificado en el código antes de ejecutar):** los inputs de monto en MAUI usan `type="number"`/`InputNumber` de Blazor, que ya parsean en formato invariante (punto decimal) independientemente de la cultura del dispositivo — es un comportamiento intencional de Blazor para inputs HTML5 `number`. **No hay que tocarlos ni migrarlos a `type="text"` con parseo manual** (el plan original lo sugería; sería una regresión, no una mejora). El bug real está solo en la **visualización**: varias vistas usan `ToString()` sin formato o `"F2"`, que sí dependen de la cultura del hilo (`CurrentCulture`), tanto en MAUI como en `ReciboService` (PDF).

---

## Objetivo

Evitar bugs de puntos/comas en decimales y dejar la infraestructura lista para multiidioma (es-EC default, en-US futuro, otros LATAM) sin tener que tocar código de negocio cuando se agreguen nuevas culturas.

**Regla de oro que guía todo el plan:**
- La **BD y la API** siempre hablan `InvariantCulture` (punto decimal, sin separador de miles) — esto ya es así para el JSON (`System.Text.Json` serializa `decimal` como número, no como string formateado), pero hay que fijarlo también para cualquier parseo/formateo de texto que haga la API (recibos PDF).
- El **cliente MAUI** muestra en `es-EC` al usuario en las vistas de *lectura* (listados, detalle); los *inputs* ya son seguros por diseño de Blazor (no se tocan).
- Los textos de UI van en archivos `.resx` desde el principio, aunque hoy solo haya español.

---

## Orden de dependencias

```
Task 1 → InvariantCulture global en la API
Task 2 → Verificar DTOs: decimal puro, nunca string para dinero (ya cumplido, solo confirmar)
Task 3 → MoneyFormatter centralizado en Shared
Task 4 → AddLocalization + culturas soportadas (API y MAUI)
Task 5 → Archivos .resx (es-EC default, en-US vacío/futuro)
Task 6 → MAUI: usar MoneyFormatter en las vistas de lectura (no tocar inputs)
Task 7 → QuestPDF: MoneyFormatter + fecha explícita en ReciboService
Task 8 → Tests de globalización
Task 9 → Verificación final + actualizar CLAUDE.md y docs/contexto
```

---

## Task 1 — `InvariantCulture` global en la API

**Archivo:** `RentaFacil.API/Program.cs`

Al inicio del archivo, antes de `var builder = WebApplication.CreateBuilder(args)`:

```csharp
using System.Globalization;
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
```

**Por qué:** si el servidor de producción corre en Linux con `LANG=es_EC.UTF-8` (o cualquier locale con coma decimal), sin esto cualquier formateo de texto en el servidor (recibos PDF) puede salir con coma en vez de punto, o viceversa al parsear. Esto fija el comportamiento igual en Windows (trabajo/casa) y en cualquier servidor futuro.

**Verificación:** `dotnet build RentaFacil.API` sin errores; `dotnet run --project RentaFacil.API` → login funciona, `/api/inquilinos` devuelve datos.

---

## Task 2 — Verificar DTOs: `decimal` puro, nunca `string` para dinero

**Archivos:** `RentaFacil.Shared/Models/ContratoDto.cs`, `PagoDto.cs`, `InmuebleDto.cs`

Tarea de verificación, no de creación. Ya confirmado antes de escribir este plan: `ContratoDto`/`CrearContratoDto` usan `decimal Monto, decimal Garantia`; `PagoDto`/`CrearPagoDto` usan `decimal TotalMonto, decimal ACuenta, decimal Servicios`; `InmuebleDto` usa `decimal MontoRenta`. `System.Text.Json` serializa `decimal` como número JSON (`1500.50`), sin depender de cultura.

**Verificación:** `dotnet build RentaFacil.Shared` sin errores. No se espera ningún cambio de código en esta task — si al revisar aparece algún campo monetario como `string`, ahí sí corregirlo.

---

## Task 3 — `MoneyFormatter` centralizado en `Shared`

**Archivo nuevo:** `RentaFacil.Shared/Globalization/MoneyFormatter.cs`

```csharp
using System.Globalization;

namespace RentaFacil.Shared.Globalization;

public static class MoneyFormatter
{
    private static readonly CultureInfo EcuadorCulture = new("es-EC");

    public static string Mostrar(decimal monto, string? cultura = null)
    {
        var ci = cultura is null ? EcuadorCulture : new CultureInfo(cultura);
        return monto.ToString("C2", ci);
    }

    public static string MostrarNumero(decimal monto, string? cultura = null)
    {
        var ci = cultura is null ? EcuadorCulture : new CultureInfo(cultura);
        return monto.ToString("N2", ci);
    }

    public static decimal? Parsear(string input, string? cultura = null)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var ci = cultura is null ? EcuadorCulture : new CultureInfo(cultura);
        if (decimal.TryParse(input, NumberStyles.Number, ci, out var result))
            return result;
        return null;
    }
}
```

`Parsear` se incluye por completitud de la API pública del helper (y para Task 8), aunque hoy no se use en los inputs de MAUI (ver nota en "Contexto").

**Verificación:** `dotnet build RentaFacil.Shared`.

---

## Task 4 — `AddLocalization` + culturas soportadas

**Archivos:** `RentaFacil.API/Program.cs`, `RentaFacil.MAUI/MauiProgram.cs`

### En la API (`Program.cs`):

```csharp
builder.Services.AddLocalization(options =>
    options.ResourcesPath = "Globalization/Resources");

// En el pipeline, después de UseAuthentication()/UseAuthorization(), antes de MapControllers():
var culturasSoportadas = new[] { "es-EC", "es", "en-US" };
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("es-EC")
    .AddSupportedCultures(culturasSoportadas)
    .AddSupportedUICultures(culturasSoportadas));
```

### En MAUI (`MauiProgram.cs`):

```csharp
builder.Services.AddLocalization(options =>
    options.ResourcesPath = "Globalization/Resources");
```

**Verificación:** API arranca sin errores. `dotnet build RentaFacil.MAUI -f net10.0-android` sin errores.

---

## Task 5 — Archivos `.resx` (estructura multiidioma)

**Archivos nuevos en `RentaFacil.Shared/Globalization/Resources/`:**
- `SharedResources.cs` — clase marcador vacía para `IStringLocalizer<SharedResources>`.
- `SharedResources.es.resx` — español (default, mensajes transversales: símbolo/nombre de moneda, errores de formato de monto/identificación/campo requerido).
- `SharedResources.en.resx` — vacío por ahora (estructura lista, se llena cuando se implemente selector de idioma).

**Verificación:** `dotnet build RentaFacil.Shared`.

---

## Task 6 — MAUI: `MoneyFormatter` en las vistas de lectura

**Archivos reales a modificar** (confirmados grepeando el código, no inventados):
- `RentaFacil.MAUI/Components/Pages/Contratos.razor` — `$@c.Monto.ToString("F2")` → `@MoneyFormatter.Mostrar(c.Monto)`
- `RentaFacil.MAUI/Components/Pages/Pagos.razor` — `$@p.TotalMonto` / `$@p.ACuenta` (sin formato) → `@MoneyFormatter.Mostrar(p.TotalMonto)` / `@MoneyFormatter.Mostrar(p.ACuenta)`
- `RentaFacil.MAUI/Components/Pages/DetallePagos.razor` — `$@pago.ACuenta.ToString("F2")` → `@MoneyFormatter.Mostrar(pago.ACuenta)`
- `RentaFacil.MAUI/Components/Pages/Unidades.razor` — `$@uni.MontoRenta.ToString("F2")` → `@MoneyFormatter.Mostrar(uni.MontoRenta)`

**Lo que NO se toca:** los inputs (`CrearContrato.razor` con `type="number" @bind="monto"`, `CrearPago.razor` con `type="number" @bind="montoAPagar"`, `CrearInmueble.razor` con `<InputNumber @bind-Value="montoRenta">`) — ya son culture-safe por diseño de Blazor. Tocarlos sería una regresión, no una mejora (ver nota en "Contexto y prerequisitos").

**Verificación:** `dotnet build RentaFacil.MAUI -f net10.0-android` sin errores. Smoke test manual: la vista de Contratos/Pagos/Unidades/DetallePagos muestra montos con formato `$1.500,50` (coma decimal, es-EC) en vez del valor crudo o `F2` dependiente de cultura del hilo.

---

## Task 7 — QuestPDF: `MoneyFormatter` y fecha explícita en `ReciboService`

**Archivo:** `RentaFacil.API/Services/ReciboService.cs`

Reemplazar (nombres reales del código, confirmados):

```csharp
// Antes — depende de CurrentCulture del hilo del servidor:
text.Span($"{pago.FechaPago:dd/MM/yyyy}");
// ...
table.Cell().AlignRight().Text($"${pago.TotalMonto}");
table.Cell().AlignRight().Text($"${pago.Servicios}");
table.Cell().PaddingTop(10).AlignRight().Text($"${pago.ACuenta}").SemiBold();
table.Cell().AlignRight().Text($"${(restante > 0 ? restante : 0)}");

// Después — explícito, independiente del SO que hostee la API:
using RentaFacil.Shared.Globalization;
using System.Globalization;

private static readonly CultureInfo _culturaEC = new("es-EC");

text.Span(pago.FechaPago.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
// ...
table.Cell().AlignRight().Text(MoneyFormatter.Mostrar(pago.TotalMonto));
table.Cell().AlignRight().Text(MoneyFormatter.Mostrar(pago.Servicios));
table.Cell().PaddingTop(10).AlignRight().Text(MoneyFormatter.Mostrar(pago.ACuenta)).SemiBold();
var restante = (pago.TotalMonto + pago.Servicios) - pago.ACuenta;
table.Cell().AlignRight().Text(MoneyFormatter.Mostrar(restante > 0 ? restante : 0));
```

**Verificación:** generar un recibo PDF (`GET /api/pagos/{id}/recibo/carta`) con un pago real → el PDF muestra los montos con formato `$X.XXX,XX` consistente.

---

## Task 8 — Tests de globalización

**Archivo nuevo:** `RentaFacil.Tests/MoneyFormatterTests.cs`

```csharp
using System.Globalization;
using FluentAssertions;
using RentaFacil.Shared.Globalization;

namespace RentaFacil.Tests;

public class MoneyFormatterTests
{
    [Theory]
    [InlineData(1500.50, "es-EC", "$1.500,50")]
    [InlineData(1500.50, "en-US", "$1,500.50")]
    [InlineData(0,       "es-EC", "$0,00")]
    [InlineData(250.00,  "es-EC", "$250,00")]
    public void Mostrar_DevuelveFormatoSegunCultura(decimal monto, string cultura, string esperado)
        => MoneyFormatter.Mostrar(monto, cultura).Should().Be(esperado);

    [Theory]
    [InlineData("1500,50",  "es-EC", 1500.50)]
    [InlineData("1.500,50", "es-EC", 1500.50)]
    [InlineData("1500.50",  "en-US", 1500.50)]
    [InlineData("250",      "es-EC", 250.00)]
    [InlineData("abc",      "es-EC", null)]
    [InlineData("",         "es-EC", null)]
    [InlineData("  ",       "es-EC", null)]
    public void Parsear_DevuelveDecimalONull(string input, string cultura, decimal? esperado)
        => MoneyFormatter.Parsear(input, cultura).Should().Be(esperado);

    [Fact]
    public void InvariantCulture_UsaPuntoComoDecimal()
        => (1500.50m).ToString(CultureInfo.InvariantCulture).Should().Be("1500.50");

    [Fact]
    public void JsonDecimal_SerializaConPuntoSinImportarCultura()
    {
        var dto = new { Monto = 1500.50m };
        var json = System.Text.Json.JsonSerializer.Serialize(dto);
        json.Should().Contain("1500.5");
    }
}
```

**Verificación:** `dotnet test RentaFacil.Tests` → todos en verde, incluyendo los nuevos.

---

## Task 9 — Verificación final + actualizar `CLAUDE.md` y `docs/contexto/`

**Checklist:**
- [ ] `dotnet build RentaFacil.API` sin errores
- [ ] `dotnet build RentaFacil.MAUI -f net10.0-android` sin errores
- [ ] `dotnet test RentaFacil.Tests` → todos en verde
- [ ] Smoke test manual: crear un pago, ver su monto en `Pagos.razor`/`DetallePagos.razor` con formato `$X.XXX,XX`, generar su recibo PDF y confirmar el mismo formato ahí
- [ ] **Aplicar la regla de verificación de docs** (`CLAUDE.md` → "Regla: verificar `docs/contexto/` al cerrar cualquier tarea"): grep de "globaliz", "cultura", "MoneyFormatter" en todos los `.md` de contexto antes de cerrar.
- [ ] Actualizar `CLAUDE.md`: sección "Pendiente" (si existía algo de globalización, no lo había explícito — confirmar) y "Último Contexto" con el resumen de las 9 tasks.
- [ ] Actualizar `docs/contexto/decisiones.md` con la decisión de `InvariantCulture` + `MoneyFormatter` + es-EC como cultura default.

**Commit final:** `feat: globalización — InvariantCulture API, MoneyFormatter es-EC, infraestructura i18n .resx, QuestPDF con formato explícito`

---

## Fuera de alcance (explícitamente)

- `decimal(18,2)` en modelos y su migración SQL Server — ya hecho en `feature/migracion-sqlserver`.
- Migrar los inputs de monto a `type="text"` con parseo manual — **no aplica**, ya son culture-safe (ver nota en "Contexto y prerequisitos"); sería una regresión.
- Selector de idioma en la UI — infraestructura lista (`.resx`), el toggle es funcionalidad futura.
- Soporte LATAM adicional (es-CO, es-PE, etc.) — se agrega en `AddSupportedCultures` sin tocar código de negocio.
- Conversión de divisas — todo es USD en Ecuador, no aplica.
- Formateo de teléfonos o direcciones — fuera de alcance de globalización monetaria.
- Traducciones al inglés — `SharedResources.en.resx` queda vacío hasta que se decida implementar.
