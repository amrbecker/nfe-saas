using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NfeSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCscNFCe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CscId",
                table: "empresas",
                type: "character varying(6)",
                maxLength: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CscToken",
                table: "empresas",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CscId",
                table: "empresas");

            migrationBuilder.DropColumn(
                name: "CscToken",
                table: "empresas");
        }
    }
}
