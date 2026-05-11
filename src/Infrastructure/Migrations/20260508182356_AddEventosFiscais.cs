using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NfeSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventosFiscais : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "eventos_fiscais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Ambiente = table.Column<int>(type: "integer", nullable: false),
                    ChaveAcesso = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: true),
                    SequencialCce = table.Column<int>(type: "integer", nullable: true),
                    AnoInutilizacao = table.Column<int>(type: "integer", nullable: true),
                    TipoNotaInutilizacao = table.Column<int>(type: "integer", nullable: true),
                    SerieInutilizacao = table.Column<int>(type: "integer", nullable: true),
                    NumeroInicialInutilizacao = table.Column<int>(type: "integer", nullable: true),
                    NumeroFinalInutilizacao = table.Column<int>(type: "integer", nullable: true),
                    Justificativa = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Situacao = table.Column<int>(type: "integer", nullable: false),
                    Protocolo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    XmlEvento = table.Column<string>(type: "text", nullable: true),
                    XmlRetorno = table.Column<string>(type: "text", nullable: true),
                    MotivoRejeicao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DataEvento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataRetorno = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eventos_fiscais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_eventos_fiscais_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_eventos_fiscais_EmpresaId_ChaveAcesso",
                table: "eventos_fiscais",
                columns: new[] { "EmpresaId", "ChaveAcesso" });

            migrationBuilder.CreateIndex(
                name: "IX_eventos_fiscais_EmpresaId_Tipo_DataEvento",
                table: "eventos_fiscais",
                columns: new[] { "EmpresaId", "Tipo", "DataEvento" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "eventos_fiscais");
        }
    }
}
