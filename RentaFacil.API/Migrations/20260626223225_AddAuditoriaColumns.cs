using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentaFacil.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditoriaColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreadoPorId",
                table: "Unidades",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCreacion",
                table: "Unidades",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaModificacion",
                table: "Unidades",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModificadoPorId",
                table: "Unidades",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreadoPorId",
                table: "Pagos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCreacion",
                table: "Pagos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaModificacion",
                table: "Pagos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModificadoPorId",
                table: "Pagos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreadoPorId",
                table: "Inquilinos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCreacion",
                table: "Inquilinos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaModificacion",
                table: "Inquilinos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModificadoPorId",
                table: "Inquilinos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreadoPorId",
                table: "Inmuebles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCreacion",
                table: "Inmuebles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaModificacion",
                table: "Inmuebles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModificadoPorId",
                table: "Inmuebles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreadoPorId",
                table: "Contratos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCreacion",
                table: "Contratos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaModificacion",
                table: "Contratos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModificadoPorId",
                table: "Contratos",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreadoPorId",
                table: "Unidades");

            migrationBuilder.DropColumn(
                name: "FechaCreacion",
                table: "Unidades");

            migrationBuilder.DropColumn(
                name: "FechaModificacion",
                table: "Unidades");

            migrationBuilder.DropColumn(
                name: "ModificadoPorId",
                table: "Unidades");

            migrationBuilder.DropColumn(
                name: "CreadoPorId",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "FechaCreacion",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "FechaModificacion",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "ModificadoPorId",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "CreadoPorId",
                table: "Inquilinos");

            migrationBuilder.DropColumn(
                name: "FechaCreacion",
                table: "Inquilinos");

            migrationBuilder.DropColumn(
                name: "FechaModificacion",
                table: "Inquilinos");

            migrationBuilder.DropColumn(
                name: "ModificadoPorId",
                table: "Inquilinos");

            migrationBuilder.DropColumn(
                name: "CreadoPorId",
                table: "Inmuebles");

            migrationBuilder.DropColumn(
                name: "FechaCreacion",
                table: "Inmuebles");

            migrationBuilder.DropColumn(
                name: "FechaModificacion",
                table: "Inmuebles");

            migrationBuilder.DropColumn(
                name: "ModificadoPorId",
                table: "Inmuebles");

            migrationBuilder.DropColumn(
                name: "CreadoPorId",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "FechaCreacion",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "FechaModificacion",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "ModificadoPorId",
                table: "Contratos");
        }
    }
}
