using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NfeSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogAndContingencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TipoEmissao",
                table: "notas_fiscais",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    Acao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ChaveNFe = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: true),
                    Detalhes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IpOrigem = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_EmpresaId",
                table: "audit_logs",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_Timestamp",
                table: "audit_logs",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropColumn(
                name: "TipoEmissao",
                table: "notas_fiscais");
        }
    }
}
