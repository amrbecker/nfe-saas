using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NfeSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCnaeAndNotaFiscalDedupIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notas_fiscais_EmpresaId",
                table: "notas_fiscais");

            migrationBuilder.AddColumn<string>(
                name: "Cnae",
                table: "empresas",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_notas_fiscais_dedup",
                table: "notas_fiscais",
                columns: new[] { "EmpresaId", "Tipo", "Serie", "Numero", "Ambiente" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_notas_fiscais_dedup",
                table: "notas_fiscais");

            migrationBuilder.DropColumn(
                name: "Cnae",
                table: "empresas");

            migrationBuilder.CreateIndex(
                name: "IX_notas_fiscais_EmpresaId",
                table: "notas_fiscais",
                column: "EmpresaId");
        }
    }
}
