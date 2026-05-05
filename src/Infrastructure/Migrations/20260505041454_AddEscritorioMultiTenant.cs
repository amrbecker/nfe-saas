using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NfeSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEscritorioMultiTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_usuarios_empresas_EmpresaId",
                table: "usuarios");

            migrationBuilder.RenameColumn(
                name: "EmpresaId",
                table: "usuarios",
                newName: "EscritorioId");

            migrationBuilder.RenameIndex(
                name: "IX_usuarios_EmpresaId",
                table: "usuarios",
                newName: "IX_usuarios_EscritorioId");

            migrationBuilder.AddColumn<Guid>(
                name: "EscritorioId",
                table: "empresas",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "escritorios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RazaoSocial = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    NomeFantasia = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Cnpj = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Plano = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_escritorios", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_empresas_EscritorioId",
                table: "empresas",
                column: "EscritorioId");

            migrationBuilder.CreateIndex(
                name: "IX_escritorios_Cnpj",
                table: "escritorios",
                column: "Cnpj",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_empresas_escritorios_EscritorioId",
                table: "empresas",
                column: "EscritorioId",
                principalTable: "escritorios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_usuarios_escritorios_EscritorioId",
                table: "usuarios",
                column: "EscritorioId",
                principalTable: "escritorios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_empresas_escritorios_EscritorioId",
                table: "empresas");

            migrationBuilder.DropForeignKey(
                name: "FK_usuarios_escritorios_EscritorioId",
                table: "usuarios");

            migrationBuilder.DropTable(
                name: "escritorios");

            migrationBuilder.DropIndex(
                name: "IX_empresas_EscritorioId",
                table: "empresas");

            migrationBuilder.DropColumn(
                name: "EscritorioId",
                table: "empresas");

            migrationBuilder.RenameColumn(
                name: "EscritorioId",
                table: "usuarios",
                newName: "EmpresaId");

            migrationBuilder.RenameIndex(
                name: "IX_usuarios_EscritorioId",
                table: "usuarios",
                newName: "IX_usuarios_EmpresaId");

            migrationBuilder.AddForeignKey(
                name: "FK_usuarios_empresas_EmpresaId",
                table: "usuarios",
                column: "EmpresaId",
                principalTable: "empresas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
