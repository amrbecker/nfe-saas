using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NfeSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCnaesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cnaes",
                columns: table => new
                {
                    Codigo = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Secao = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    Divisao = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cnaes", x => x.Codigo);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cnaes_Ativo",
                table: "cnaes",
                column: "Ativo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cnaes");
        }
    }
}
