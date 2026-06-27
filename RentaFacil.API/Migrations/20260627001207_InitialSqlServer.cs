using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentaFacil.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqlServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "renta");

            migrationBuilder.EnsureSchema(
                name: "auth");

            migrationBuilder.CreateTable(
                name: "Inmuebles",
                schema: "renta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    MontoRenta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    CreadoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModificadoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inmuebles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Inquilinos",
                schema: "renta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreCompleto = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Identificacion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FotoUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    CreadoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModificadoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inquilinos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreUsuario = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Unidades",
                schema: "renta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MontoRenta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Ocupada = table.Column<bool>(type: "bit", nullable: false),
                    InmuebleId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    CreadoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModificadoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Unidades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Unidades_Inmuebles_InmuebleId",
                        column: x => x.InmuebleId,
                        principalSchema: "renta",
                        principalTable: "Inmuebles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Contratos",
                schema: "renta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InquilinoId = table.Column<int>(type: "int", nullable: false),
                    UnidadId = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Garantia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Frecuencia = table.Column<int>(type: "int", nullable: false),
                    DuracionMeses = table.Column<int>(type: "int", nullable: false),
                    DiaPago = table.Column<int>(type: "int", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    CreadoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModificadoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contratos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contratos_Inquilinos_InquilinoId",
                        column: x => x.InquilinoId,
                        principalSchema: "renta",
                        principalTable: "Inquilinos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contratos_Unidades_UnidadId",
                        column: x => x.UnidadId,
                        principalSchema: "renta",
                        principalTable: "Unidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pagos",
                schema: "renta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContratoId = table.Column<int>(type: "int", nullable: false),
                    TotalMonto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ACuenta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Servicios = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FechaPago = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Periodo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Facturado = table.Column<bool>(type: "bit", nullable: false),
                    Completado = table.Column<bool>(type: "bit", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    CreadoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModificadoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pagos_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalSchema: "renta",
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contratos_InquilinoId",
                schema: "renta",
                table: "Contratos",
                column: "InquilinoId");

            migrationBuilder.CreateIndex(
                name: "IX_Contratos_UnidadId",
                schema: "renta",
                table: "Contratos",
                column: "UnidadId");

            migrationBuilder.CreateIndex(
                name: "IX_Contratos_UsuarioId",
                schema: "renta",
                table: "Contratos",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Inmuebles_UsuarioId",
                schema: "renta",
                table: "Inmuebles",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Inquilinos_UsuarioId",
                schema: "renta",
                table: "Inquilinos",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_ContratoId",
                schema: "renta",
                table: "Pagos",
                column: "ContratoId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_UsuarioId",
                schema: "renta",
                table: "Pagos",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Unidades_InmuebleId",
                schema: "renta",
                table: "Unidades",
                column: "InmuebleId");

            migrationBuilder.CreateIndex(
                name: "IX_Unidades_UsuarioId",
                schema: "renta",
                table: "Unidades",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_NombreUsuario",
                schema: "auth",
                table: "Usuarios",
                column: "NombreUsuario",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pagos",
                schema: "renta");

            migrationBuilder.DropTable(
                name: "Usuarios",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "Contratos",
                schema: "renta");

            migrationBuilder.DropTable(
                name: "Inquilinos",
                schema: "renta");

            migrationBuilder.DropTable(
                name: "Unidades",
                schema: "renta");

            migrationBuilder.DropTable(
                name: "Inmuebles",
                schema: "renta");
        }
    }
}
