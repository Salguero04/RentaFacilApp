using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentaFacil.API.Migrations
{
    /// <inheritdoc />
    public partial class ModuloInquilino : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UsuarioCuentaId",
                schema: "renta",
                table: "Inquilinos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CodigosVinculacion",
                schema: "renta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    ContratoId = table.Column<int>(type: "int", nullable: false),
                    InquilinoId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaExpiracion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsadoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodigosVinculacion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportesPago",
                schema: "renta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContratoId = table.Column<int>(type: "int", nullable: false),
                    InquilinoId = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Comentario = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FotoComprobante = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    FechaReporte = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    CuentaInquilinoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportesPago", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inquilinos_UsuarioCuentaId",
                schema: "renta",
                table: "Inquilinos",
                column: "UsuarioCuentaId");

            migrationBuilder.CreateIndex(
                name: "IX_CodigosVinculacion_Codigo",
                schema: "renta",
                table: "CodigosVinculacion",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CodigosVinculacion_UsuarioId",
                schema: "renta",
                table: "CodigosVinculacion",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportesPago_CuentaInquilinoId",
                schema: "renta",
                table: "ReportesPago",
                column: "CuentaInquilinoId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportesPago_UsuarioId",
                schema: "renta",
                table: "ReportesPago",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CodigosVinculacion",
                schema: "renta");

            migrationBuilder.DropTable(
                name: "ReportesPago",
                schema: "renta");

            migrationBuilder.DropIndex(
                name: "IX_Inquilinos_UsuarioCuentaId",
                schema: "renta",
                table: "Inquilinos");

            migrationBuilder.DropColumn(
                name: "UsuarioCuentaId",
                schema: "renta",
                table: "Inquilinos");
        }
    }
}
