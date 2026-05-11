using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NfeSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTributacaoAvancada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalFcp",
                table: "notas_fiscais",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalIcmsUfDestino",
                table: "notas_fiscais",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalIcmsUfRemetente",
                table: "notas_fiscais",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalIpi",
                table: "notas_fiscais",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AliquotaFcp",
                table: "itens_nota_fiscal",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AliquotaInterestadual",
                table: "itens_nota_fiscal",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AliquotaInternaUfDestino",
                table: "itens_nota_fiscal",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseCalculoDifal",
                table: "itens_nota_fiscal",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseCalculoFcp",
                table: "itens_nota_fiscal",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseCalculoIpi",
                table: "itens_nota_fiscal",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorFcp",
                table: "itens_nota_fiscal",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorIcmsUfDestino",
                table: "itens_nota_fiscal",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorIcmsUfRemetente",
                table: "itens_nota_fiscal",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalFcp",
                table: "notas_fiscais");

            migrationBuilder.DropColumn(
                name: "TotalIcmsUfDestino",
                table: "notas_fiscais");

            migrationBuilder.DropColumn(
                name: "TotalIcmsUfRemetente",
                table: "notas_fiscais");

            migrationBuilder.DropColumn(
                name: "TotalIpi",
                table: "notas_fiscais");

            migrationBuilder.DropColumn(
                name: "AliquotaFcp",
                table: "itens_nota_fiscal");

            migrationBuilder.DropColumn(
                name: "AliquotaInterestadual",
                table: "itens_nota_fiscal");

            migrationBuilder.DropColumn(
                name: "AliquotaInternaUfDestino",
                table: "itens_nota_fiscal");

            migrationBuilder.DropColumn(
                name: "BaseCalculoDifal",
                table: "itens_nota_fiscal");

            migrationBuilder.DropColumn(
                name: "BaseCalculoFcp",
                table: "itens_nota_fiscal");

            migrationBuilder.DropColumn(
                name: "BaseCalculoIpi",
                table: "itens_nota_fiscal");

            migrationBuilder.DropColumn(
                name: "ValorFcp",
                table: "itens_nota_fiscal");

            migrationBuilder.DropColumn(
                name: "ValorIcmsUfDestino",
                table: "itens_nota_fiscal");

            migrationBuilder.DropColumn(
                name: "ValorIcmsUfRemetente",
                table: "itens_nota_fiscal");
        }
    }
}
