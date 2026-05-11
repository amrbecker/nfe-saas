using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NfeSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConfiguracaoEmpresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "configuracoes_empresa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    PerfilCliente = table.Column<int>(type: "integer", nullable: false),
                    TipoProduto = table.Column<int>(type: "integer", nullable: false),
                    VolumeNotas = table.Column<int>(type: "integer", nullable: false),
                    NivelAutomacao = table.Column<int>(type: "integer", nullable: false),
                    EmiteParaConsumidorFinal = table.Column<bool>(type: "boolean", nullable: false),
                    OperaIcmsSt = table.Column<bool>(type: "boolean", nullable: false),
                    NivelRelatorio = table.Column<int>(type: "integer", nullable: false),
                    ConcluidoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuracoes_empresa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_configuracoes_empresa_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_configuracoes_empresa_EmpresaId",
                table: "configuracoes_empresa",
                column: "EmpresaId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "configuracoes_empresa");
        }
    }
}
