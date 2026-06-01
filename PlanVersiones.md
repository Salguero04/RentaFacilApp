# Plan de Control de Versiones: RentaFacilApp 🚀

Este documento define la estrategia, nomenclatura y hoja de ruta para el ciclo de vida y despliegue del proyecto **RentaFacilApp** y su API (**RentaFacil.API**). El sistema está inspirado en hitos de contenido temático, dividiendo el desarrollo en fases claras de evolución.

---

## 📌 1. Regla de Nomenclatura (SemVer Adaptado)

Utilizaremos una variación del Sementic Versioning (`X.Y.Z`):
- **X (Mayor):** Grandes cambios de arquitectura, cambio de entorno/servidor clave o rediseño total.
- **Y (Menor):** Nuevas funcionalidades completas (como una actualización de bioma).
- **Z (Parche):** Corrección de errores (*bugfixes*) y optimizaciones menores.

---

## 🗺️ 2. Hoja de Ruta y Fases de Despliegue

### 🥑 Fase 1: El Despertar del Proyecto (Entorno Local)
*El proyecto está tomando forma, se prioriza la estabilidad de las funciones base.*

* **Identificador:** `RentaFacilApp beta V1.0.1`
* **Estado:** Desarrollo Activo.
* **Entorno:** Localhost (Simuladores, base de datos local / SQLite / Docker local).
* **Objetivo:** * Estabilizar la lógica del backend (`RentaFacil.API`) y la interfaz de la app móvil.
    * Implementar las funciones esenciales de renta y usuarios.
* **Control de Cambios (Sucesivos):** Las siguientes versiones locales avanzarán como `beta V1.0.2`, `beta V1.1.0` (si se añade un módulo nuevo), hasta que el núcleo sea completamente funcional.

---

### ☁️ Fase 2: La Conexión a la Nube (Entorno de Pruebas - Render)
*Hito equivalente a preparar la infraestructura de red. La API se independiza de la máquina local.*

* **Identificador:** `RentaFacilApp_V1.0.1` (Se remueve la etiqueta *beta* de desarrollo local).
* **Estado:** Pruebas de Integración y Conectividad.
* **Entorno:** **Render** (Servidor de pruebas gratuito/económico).
* **Objetivo:**
    * Desplegar `RentaFacil.API` en Render.
    * Configurar variables de entorno reales (CORS, strings de conexión seguros).
    * Apuntar la aplicación móvil hacia la URL de Render para pruebas de consumo real de datos en redes móviles o emuladores externos.
* **Control de Cambios:** Cualquier corrección detectada durante las pruebas en Render incrementará el parche: `RentaFacilApp_V1.0.2`, `V1.0.3`, etc.

---

### 🏛️ Fase 3: La "Nether Update" - Despliegue de Producción (Oracle Cloud)
*El gran salto. Equivalente a la versión 1.16 de Minecraft: un cambio masivo de infraestructura, rendimiento y disponibilidad permanente.*

* **Identificador:** `RentaFacilApp_V2.0.1` (Salto mayor a **V2** debido al cambio crítico de servidor e infraestructura definitiva).
* **Estado:** Producción / Lanzamiento Estable.
* **Entorno:** **Oracle Cloud Always Free** (Instancia Compute Linux / Docker / Base de datos gestionada).
* **Objetivo:**
    * Migración de los servicios de la API al entorno gratuito de Oracle Cloud para garantizar disponibilidad 24/7 sin suspensiones por inactividad.
    * Optimización de recursos aprovechando la arquitectura de Oracle.
    * Fijar la base para futuras actualizaciones temáticas de contenido masivo.

---

## 🛠️ 3. Flujo de Trabajo en Git (Workflow)

Para mantener el orden en este plan, el repositorio seguirá las siguientes ramas:

1.  **`main` / `master`:** Contiene estrictamente el código que ya está desplegado (en Render durante la Fase 2, y en Oracle en la Fase 3).
2.  **`develop`:** Centraliza las versiones `beta`. Es donde se prueban las funciones antes de enviarlas a los servidores.
3.  **`feature/nombre-funcion`:** Ramas temporales creadas para programar características específicas (ej: `feature/login`, `feature/filtro-rentas`). Una vez terminadas, se fusionan a `develop`.

**Regla de Respaldo Automático:**
* Cada vez que se genere un nuevo archivo `.apk` y se incremente la versión, se debe realizar un `git commit` y (opcionalmente) un `git push` de manera obligatoria para asegurar un respaldo en GitHub del estado exacto del código que originó dicha APK.

---

## 📋 4. Lista de Verificación para Cambios de Fase

Antes de pasar de una versión a otra, se debe cumplir:

- [ ] **De Beta a Render (V1.0.1):** El código no debe compilar con errores locales, y las credenciales secretas deben estar fuera del código (`.env`).
- [ ] **De Render a Oracle (V2.0.1):** La app debe haber superado las pruebas de latencia en Render y los scripts de migración de base de datos deben estar listos para producción.