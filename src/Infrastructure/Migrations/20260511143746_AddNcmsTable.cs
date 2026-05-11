using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NfeSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNcmsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ncms",
                columns: table => new
                {
                    Codigo = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CategoriaCapitulo = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    Posicao = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    AliquotaIpiPadrao = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    ExigeCest = table.Column<bool>(type: "boolean", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    VersaoTabela = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ncms", x => x.Codigo);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ncms_Ativo",
                table: "ncms",
                column: "Ativo");

            migrationBuilder.CreateIndex(
                name: "ix_ncms_codigo_prefix",
                table: "ncms",
                column: "Codigo");

            migrationBuilder.CreateIndex(
                name: "IX_ncms_Posicao",
                table: "ncms",
                column: "Posicao");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ncms");
        }
    }
}
