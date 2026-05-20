using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NfeSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTrialEAtivacaoPlano : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PlanoAtivoAteEm",
                table: "escritorios",
                type: "timestamp with time zone",
                nullable: true);

            // Trial: NOT NULL com default = NOW() para que registros existentes (seed/legacy)
            // recebam um trial fresh ao migrar, em vez de virem com "trial expirado".
            // O default fica como NOW() (gravado uma vez no momento da migration); novos inserts
            // calculam TrialInicioEm/TrialFimEm pela entidade (Escritorio.Criar).
            migrationBuilder.AddColumn<DateTime>(
                name: "TrialInicioEm",
                table: "escritorios",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.AddColumn<DateTime>(
                name: "TrialFimEm",
                table: "escritorios",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW() + INTERVAL '30 days'");

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimoPagamentoEm",
                table: "escritorios",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlanoAtivoAteEm",
                table: "escritorios");

            migrationBuilder.DropColumn(
                name: "TrialFimEm",
                table: "escritorios");

            migrationBuilder.DropColumn(
                name: "TrialInicioEm",
                table: "escritorios");

            migrationBuilder.DropColumn(
                name: "UltimoPagamentoEm",
                table: "escritorios");
        }
    }
}
