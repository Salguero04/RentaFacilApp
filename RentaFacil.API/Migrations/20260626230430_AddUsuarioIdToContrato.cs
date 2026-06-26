using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentaFacil.API.Migrations
{
    /// <inheritdoc />
    public partial class AddUsuarioIdToContrato : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UsuarioId",
                table: "Contratos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Contratos");
        }
    }
}
