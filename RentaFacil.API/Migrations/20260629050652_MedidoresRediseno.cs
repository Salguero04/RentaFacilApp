using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentaFacil.API.Migrations
{
    /// <inheritdoc />
    public partial class MedidoresRediseno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CostosServicio",
                schema: "renta");

            migrationBuilder.DropTable(
                name: "ServiciosContrato",
                schema: "renta");

            migrationBuilder.CreateTable(
                name: "Medidores",
                schema: "renta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    InmuebleId = table.Column<int>(type: "int", nullable: false),
                    Modo = table.Column<int>(type: "int", nullable: false),
                    SubConsumoHabilitado = table.Column<bool>(type: "bit", nullable: false),
                    Tarifa = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    CreadoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModificadoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medidores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Medidores_Inmuebles_InmuebleId",
                        column: x => x.InmuebleId,
                        principalSchema: "renta",
                        principalTable: "Inmuebles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificacionesPendientes",
                schema: "renta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContratoId = table.Column<int>(type: "int", nullable: false),
                    InquilinoId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Detalle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notificado = table.Column<bool>(type: "bit", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificacionesPendientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FacturasMedidor",
                schema: "renta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MedidorId = table.Column<int>(type: "int", nullable: false),
                    Mes = table.Column<int>(type: "int", nullable: false),
                    Anio = table.Column<int>(type: "int", nullable: false),
                    MontoReal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    CreadoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModificadoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturasMedidor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacturasMedidor_Medidores_MedidorId",
                        column: x => x.MedidorId,
                        principalSchema: "renta",
                        principalTable: "Medidores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedidoresInquilino",
                schema: "renta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MedidorId = table.Column<int>(type: "int", nullable: false),
                    InquilinoId = table.Column<int>(type: "int", nullable: false),
                    ContratoId = table.Column<int>(type: "int", nullable: true),
                    MetodoCobro = table.Column<int>(type: "int", nullable: false),
                    MontoFijo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LecturaAnterior = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LecturaActual = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    CreadoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModificadoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedidoresInquilino", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedidoresInquilino_Inquilinos_InquilinoId",
                        column: x => x.InquilinoId,
                        principalSchema: "renta",
                        principalTable: "Inquilinos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedidoresInquilino_Medidores_MedidorId",
                        column: x => x.MedidorId,
                        principalSchema: "renta",
                        principalTable: "Medidores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FacturasMedidor_MedidorId",
                schema: "renta",
                table: "FacturasMedidor",
                column: "MedidorId");

            migrationBuilder.CreateIndex(
                name: "IX_FacturasMedidor_UsuarioId",
                schema: "renta",
                table: "FacturasMedidor",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Medidores_InmuebleId",
                schema: "renta",
                table: "Medidores",
                column: "InmuebleId");

            migrationBuilder.CreateIndex(
                name: "IX_Medidores_UsuarioId",
                schema: "renta",
                table: "Medidores",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_MedidoresInquilino_InquilinoId",
                schema: "renta",
                table: "MedidoresInquilino",
                column: "InquilinoId");

            migrationBuilder.CreateIndex(
                name: "IX_MedidoresInquilino_MedidorId",
                schema: "renta",
                table: "MedidoresInquilino",
                column: "MedidorId");

            migrationBuilder.CreateIndex(
                name: "IX_MedidoresInquilino_UsuarioId",
                schema: "renta",
                table: "MedidoresInquilino",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificacionesPendientes_UsuarioId",
                schema: "renta",
                table: "NotificacionesPendientes",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FacturasMedidor",
                schema: "renta");

            migrationBuilder.DropTable(
                name: "MedidoresInquilino",
                schema: "renta");

            migrationBuilder.DropTable(
                name: "NotificacionesPendientes",
                schema: "renta");

            migrationBuilder.DropTable(
                name: "Medidores",
                schema: "renta");

            migrationBuilder.CreateTable(
                name: "CostosServicio",
                schema: "renta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InmuebleId = table.Column<int>(type: "int", nullable: false),
                    Anio = table.Column<int>(type: "int", nullable: false),
                    CreadoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Mes = table.Column<int>(type: "int", nullable: false),
                    ModificadoPorId = table.Column<int>(type: "int", nullable: true),
                    MontoReal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostosServicio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostosServicio_Inmuebles_InmuebleId",
                        column: x => x.InmuebleId,
                        principalSchema: "renta",
                        principalTable: "Inmuebles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiciosContrato",
                schema: "renta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContratoId = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    CreadoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Modalidad = table.Column<int>(type: "int", nullable: false),
                    ModificadoPorId = table.Column<int>(type: "int", nullable: true),
                    MontoFijo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiciosContrato", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiciosContrato_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalSchema: "renta",
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CostosServicio_InmuebleId",
                schema: "renta",
                table: "CostosServicio",
                column: "InmuebleId");

            migrationBuilder.CreateIndex(
                name: "IX_CostosServicio_UsuarioId",
                schema: "renta",
                table: "CostosServicio",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiciosContrato_ContratoId",
                schema: "renta",
                table: "ServiciosContrato",
                column: "ContratoId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiciosContrato_UsuarioId",
                schema: "renta",
                table: "ServiciosContrato",
                column: "UsuarioId");
        }
    }
}
