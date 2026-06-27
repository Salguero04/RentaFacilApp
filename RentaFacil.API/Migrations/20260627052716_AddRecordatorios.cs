using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentaFacil.API.Migrations
{
    /// <inheritdoc />
    public partial class AddRecordatorios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Recordatorios",
                schema: "renta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InquilinoId = table.Column<int>(type: "int", nullable: false),
                    ContratoId = table.Column<int>(type: "int", nullable: true),
                    Detalle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FechaProgramada = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    CreadoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModificadoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recordatorios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recordatorios_Inquilinos_InquilinoId",
                        column: x => x.InquilinoId,
                        principalSchema: "renta",
                        principalTable: "Inquilinos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Recordatorios_InquilinoId",
                schema: "renta",
                table: "Recordatorios",
                column: "InquilinoId");

            migrationBuilder.CreateIndex(
                name: "IX_Recordatorios_UsuarioId",
                schema: "renta",
                table: "Recordatorios",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Recordatorios",
                schema: "renta");
        }
    }
}
