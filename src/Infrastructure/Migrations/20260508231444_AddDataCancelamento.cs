using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NfeSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDataCancelamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataCancelamento",
                table: "notas_fiscais",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataCancelamento",
                table: "notas_fiscais");
        }
    }
}
