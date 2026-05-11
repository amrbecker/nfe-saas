using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NfeSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProdutoCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoPessoa = table.Column<int>(type: "integer", nullable: false),
                    CpfCnpj = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    RazaoSocial = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    NomeFantasia = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Logradouro = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Complemento = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Bairro = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Cidade = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Cep = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    CodigoMunicipio = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    InscricaoEstadual = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IndicadorIe = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clientes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_clientes_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "produtos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Ncm = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Cest = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    CfopPadrao = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    UnidadeComercial = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    OrigemMercadoria = table.Column<int>(type: "integer", nullable: false),
                    ValorUnitarioPadrao = table.Column<decimal>(type: "numeric(15,4)", precision: 15, scale: 4, nullable: false),
                    CodigoEan = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    CodigoAnp = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_produtos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_produtos_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_clientes_empresa_cpfcnpj",
                table: "clientes",
                columns: new[] { "EmpresaId", "CpfCnpj" });

            migrationBuilder.CreateIndex(
                name: "ix_produtos_empresa_codigo",
                table: "produtos",
                columns: new[] { "EmpresaId", "Codigo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "clientes");

            migrationBuilder.DropTable(
                name: "produtos");
        }
    }
}
