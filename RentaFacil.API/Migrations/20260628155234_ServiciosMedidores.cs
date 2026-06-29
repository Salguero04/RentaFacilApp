using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentaFacil.API.Migrations
{
    /// <inheritdoc />
    public partial class ServiciosMedidores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CostosServicio",
                schema: "renta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InmuebleId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
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
                name: "DetallesServicioPago",
                schema: "renta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PagoId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesServicioPago", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetallesServicioPago_Pagos_PagoId",
                        column: x => x.PagoId,
                        principalSchema: "renta",
                        principalTable: "Pagos",
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
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Modalidad = table.Column<int>(type: "int", nullable: false),
                    MontoFijo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    CreadoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModificadoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                name: "IX_DetallesServicioPago_PagoId",
                schema: "renta",
                table: "DetallesServicioPago",
                column: "PagoId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesServicioPago_UsuarioId",
                schema: "renta",
                table: "DetallesServicioPago",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CostosServicio",
                schema: "renta");

            migrationBuilder.DropTable(
                name: "DetallesServicioPago",
                schema: "renta");

            migrationBuilder.DropTable(
                name: "ServiciosContrato",
                schema: "renta");
        }
    }
}
