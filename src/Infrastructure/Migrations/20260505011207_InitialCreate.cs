using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NfeSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "empresas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RazaoSocial = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    NomeFantasia = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Cnpj = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    InscricaoEstadual = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    InscricaoMunicipal = table.Column<string>(type: "text", nullable: true),
                    Logradouro = table.Column<string>(type: "text", nullable: false),
                    Numero = table.Column<string>(type: "text", nullable: false),
                    Complemento = table.Column<string>(type: "text", nullable: true),
                    Bairro = table.Column<string>(type: "text", nullable: false),
                    Cidade = table.Column<string>(type: "text", nullable: false),
                    Uf = table.Column<string>(type: "text", nullable: false),
                    Cep = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    CodigoMunicipio = table.Column<string>(type: "text", nullable: false),
                    Telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RegimeTributario = table.Column<int>(type: "integer", nullable: false),
                    AmbienteSefaz = table.Column<int>(type: "integer", nullable: false),
                    UltimoNumeronFe = table.Column<int>(type: "integer", nullable: false),
                    UltimoNumeronFCe = table.Column<int>(type: "integer", nullable: false),
                    SerieNFe = table.Column<int>(type: "integer", nullable: false),
                    SerieNFCe = table.Column<int>(type: "integer", nullable: false),
                    CaminhoLogotipo = table.Column<string>(type: "text", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CertificadoBytes = table.Column<byte[]>(type: "bytea", nullable: true),
                    CertificadoSenha = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CertificadoValidade = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CertificadoCnpj = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_empresas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "notas_fiscais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Serie = table.Column<int>(type: "integer", nullable: false),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    ChaveAcesso = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: true),
                    Protocolo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DataAutorizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Finalidade = table.Column<int>(type: "integer", nullable: false),
                    TipoOperacao = table.Column<int>(type: "integer", nullable: false),
                    Ambiente = table.Column<int>(type: "integer", nullable: false),
                    Situacao = table.Column<int>(type: "integer", nullable: false),
                    DestinatarioCpfCnpj = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    DestinatarioRazaoSocial = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    DestinatarioEmail = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DestinatarioLogradouro = table.Column<string>(type: "text", nullable: true),
                    DestinatarioNumero = table.Column<string>(type: "text", nullable: true),
                    DestinatarioComplemento = table.Column<string>(type: "text", nullable: true),
                    DestinatarioBairro = table.Column<string>(type: "text", nullable: true),
                    DestinatarioCidade = table.Column<string>(type: "text", nullable: true),
                    DestinatarioUf = table.Column<string>(type: "text", nullable: true),
                    DestinatarioCep = table.Column<string>(type: "text", nullable: true),
                    DestinatarioCodigoMunicipio = table.Column<string>(type: "text", nullable: true),
                    DestinatarioInscricaoEstadual = table.Column<string>(type: "text", nullable: true),
                    DestinatarioTipoPessoa = table.Column<int>(type: "integer", nullable: false),
                    TotalProdutos = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: false),
                    TotalDesconto = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalIcms = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: false),
                    TotalIcmsSt = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalPis = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalCofins = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalFrete = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalSeguro = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalOutrasDespesas = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalNota = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: false),
                    ModalidadeFrete = table.Column<int>(type: "integer", nullable: false),
                    TransportadoraCpfCnpj = table.Column<string>(type: "text", nullable: true),
                    TransportadoraRazaoSocial = table.Column<string>(type: "text", nullable: true),
                    FormaPagemento = table.Column<string>(type: "text", nullable: false),
                    ValorPagamento = table.Column<decimal>(type: "numeric", nullable: false),
                    XmlEnvio = table.Column<string>(type: "text", nullable: true),
                    XmlRetorno = table.Column<string>(type: "text", nullable: true),
                    XmlCancelamento = table.Column<string>(type: "text", nullable: true),
                    MotivoRejeicao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InformacoesAdicionais = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DataEmissao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notas_fiscais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notas_fiscais_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SenhaHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    RefreshToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RefreshTokenExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_usuarios_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "itens_nota_fiscal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NotaFiscalId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroItem = table.Column<int>(type: "integer", nullable: false),
                    CodigoProduto = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CodigoEan = table.Column<string>(type: "text", nullable: true),
                    Ncm = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Cest = table.Column<string>(type: "text", nullable: true),
                    Cfop = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    UnidadeComercial = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    Quantidade = table.Column<decimal>(type: "numeric", nullable: false),
                    ValorUnitario = table.Column<decimal>(type: "numeric(15,4)", precision: 15, scale: 4, nullable: false),
                    ValorTotal = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: false),
                    ValorDesconto = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: false),
                    OrigemMercadoria = table.Column<int>(type: "integer", nullable: false),
                    CstIcms = table.Column<int>(type: "integer", nullable: false),
                    BaseCalculoIcms = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: false),
                    AliquotaIcms = table.Column<decimal>(type: "numeric", nullable: false),
                    ValorIcms = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: false),
                    BaseCalculoIcmsReducao = table.Column<decimal>(type: "numeric", nullable: true),
                    ValorIcmsSt = table.Column<decimal>(type: "numeric", nullable: true),
                    BaseCalculoIcmsSt = table.Column<decimal>(type: "numeric", nullable: true),
                    AliquotaIcmsSt = table.Column<decimal>(type: "numeric", nullable: true),
                    CstPis = table.Column<int>(type: "integer", nullable: false),
                    BaseCalculoPis = table.Column<decimal>(type: "numeric", nullable: false),
                    AliquotaPis = table.Column<decimal>(type: "numeric", nullable: false),
                    ValorPis = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: false),
                    CstCofins = table.Column<int>(type: "integer", nullable: false),
                    BaseCalculoCofins = table.Column<decimal>(type: "numeric", nullable: false),
                    AliquotaCofins = table.Column<decimal>(type: "numeric", nullable: false),
                    ValorCofins = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: false),
                    CstIpi = table.Column<string>(type: "text", nullable: true),
                    AliquotaIpi = table.Column<decimal>(type: "numeric", nullable: true),
                    ValorIpi = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itens_nota_fiscal", x => x.Id);
                    table.ForeignKey(
                        name: "FK_itens_nota_fiscal_notas_fiscais_NotaFiscalId",
                        column: x => x.NotaFiscalId,
                        principalTable: "notas_fiscais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_empresas_Cnpj",
                table: "empresas",
                column: "Cnpj",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_itens_nota_fiscal_NotaFiscalId",
                table: "itens_nota_fiscal",
                column: "NotaFiscalId");

            migrationBuilder.CreateIndex(
                name: "IX_notas_fiscais_ChaveAcesso",
                table: "notas_fiscais",
                column: "ChaveAcesso");

            migrationBuilder.CreateIndex(
                name: "IX_notas_fiscais_EmpresaId",
                table: "notas_fiscais",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_Email",
                table: "usuarios",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_EmpresaId",
                table: "usuarios",
                column: "EmpresaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "itens_nota_fiscal");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "notas_fiscais");

            migrationBuilder.DropTable(
                name: "empresas");
        }
    }
}
