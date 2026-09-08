using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NfeSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIbsCbsItemNotaFiscal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalCbs",
                table: "notas_fiscais",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalIbs",
                table: "notas_fiscais",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AliquotaCbs",
                table: "itens_nota_fiscal",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AliquotaIbsMun",
                table: "itens_nota_fiscal",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AliquotaIbsUf",
                table: "itens_nota_fiscal",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseCalculoIbsCbs",
                table: "itens_nota_fiscal",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClassTribIbsCbs",
                table: "itens_nota_fiscal",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CstIbsCbs",
                table: "itens_nota_fiscal",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorCbs",
                table: "itens_nota_fiscal",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorIbs",
                table: "itens_nota_fiscal",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorIbsMun",
                table: "itens_nota_fiscal",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorIbsUf",
                table: "itens_nota_fiscal",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalCbs",
                table: "notas_fiscais");

            migrationBuilder.DropColumn(
                name: "TotalIbs",
                table: "notas_fiscais");

            migrationBuilder.DropColumn(
                name: "AliquotaCbs",
                table: "itens_nota_fiscal");

            migrationBuilder.DropColumn(
                name: "AliquotaIbsMun",
                table: "itens_nota_fiscal");

            migrationBuilder.DropColumn(
                name: "AliquotaIbsUf",
                table: "itens_nota_fiscal");

            migrationBuilder.DropColumn(
                name: "BaseCalculoIbsCbs",
                table: "itens_nota_fiscal");

            migrationBuilder.DropColumn(
                name: "ClassTribIbsCbs",
                table: "itens_nota_fiscal");

            migrationBuilder.DropColumn(
                name: "CstIbsCbs",
                table: "itens_nota_fiscal");

            migrationBuilder.DropColumn(
                name: "ValorCbs",
                table: "itens_nota_fiscal");

            migrationBuilder.DropColumn(
                name: "ValorIbs",
                table: "itens_nota_fiscal");

            migrationBuilder.DropColumn(
                name: "ValorIbsMun",
                table: "itens_nota_fiscal");

            migrationBuilder.DropColumn(
                name: "ValorIbsUf",
                table: "itens_nota_fiscal");
        }
    }
}
