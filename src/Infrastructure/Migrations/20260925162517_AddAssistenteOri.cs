using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NfeSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssistenteOri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "alertas_cs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EscritorioId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: true),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Chave = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Mensagem = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    LinkAcao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Interno = table.Column<bool>(type: "boolean", nullable: false),
                    VistoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DispensadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmailEnviadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alertas_cs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chamados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EscritorioId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Passos = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Severidade = table.Column<int>(type: "integer", nullable: false),
                    NotaFiscalId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContextoTecnicoJson = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IssueUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NotaTriagem = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chamados", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "conversas_assistente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EscritorioId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ResumoAnterior = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    UltimaMensagemEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalTurnos = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversas_assistente", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "eventos_produto",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EscritorioId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: true),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Tela = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    DadosJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    OcorridoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eventos_produto", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "fontes_monitoradas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Nivel = table.Column<int>(type: "integer", nullable: false),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Mecanismo = table.Column<int>(type: "integer", nullable: false),
                    FrequenciaHoras = table.Column<int>(type: "integer", nullable: false),
                    TermosFiltro = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UltimoHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    UltimaLeituraEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UltimaMudancaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FalhasSeguidas = table.Column<int>(type: "integer", nullable: false),
                    Ativa = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fontes_monitoradas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "interacoes_assistente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EscritorioId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversaId = table.Column<Guid>(type: "uuid", nullable: true),
                    NotaFiscalId = table.Column<Guid>(type: "uuid", nullable: true),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    PerguntaSanitizada = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    RespostaSanitizada = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: false),
                    ArtigosCitados = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Selo = table.Column<int>(type: "integer", nullable: false),
                    Modelo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TokensEntrada = table.Column<long>(type: "bigint", nullable: false),
                    TokensCache = table.Column<long>(type: "bigint", nullable: false),
                    TokensSaida = table.Column<long>(type: "bigint", nullable: false),
                    CustoEstimadoUsd = table.Column<decimal>(type: "numeric(12,6)", precision: 12, scale: 6, nullable: false),
                    VerificacaoFalhou = table.Column<bool>(type: "boolean", nullable: false),
                    Avaliacao = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interacoes_assistente", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "lembretes_emissao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriadoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotaModeloId = table.Column<Guid>(type: "uuid", nullable: false),
                    Descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Periodicidade = table.Column<int>(type: "integer", nullable: false),
                    Dia = table.Column<int>(type: "integer", nullable: false),
                    ProximaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UltimoAvisoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lembretes_emissao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "preferencias_assistente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    DicasSilenciadas = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_preferencias_assistente", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sinais_produto",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EscritorioId = table.Column<Guid>(type: "uuid", nullable: true),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: true),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    ResumoSanitizado = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Tela = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Tema = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sinais_produto", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sugestoes_dispensadas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: true),
                    Chave = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    VezesIgnorada = table.Column<int>(type: "integer", nullable: false),
                    DispensadaDefinitivamente = table.Column<bool>(type: "boolean", nullable: false),
                    SuspensaAte = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sugestoes_dispensadas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "usos_assistente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    EscritorioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Dia = table.Column<DateOnly>(type: "date", nullable: false),
                    Mensagens = table.Column<int>(type: "integer", nullable: false),
                    TokensEntrada = table.Column<long>(type: "bigint", nullable: false),
                    TokensCache = table.Column<long>(type: "bigint", nullable: false),
                    TokensSaida = table.Column<long>(type: "bigint", nullable: false),
                    CustoEstimadoUsd = table.Column<decimal>(type: "numeric(12,6)", precision: 12, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usos_assistente", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "mensagens_conversa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Papel = table.Column<int>(type: "integer", nullable: false),
                    ConteudoSanitizado = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: false),
                    FerramentasJson = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: true),
                    CriadaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mensagens_conversa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mensagens_conversa_conversas_assistente_ConversaId",
                        column: x => x.ConversaId,
                        principalTable: "conversas_assistente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "publicacoes_detectadas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FonteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DetectadaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Relevante = table.Column<bool>(type: "boolean", nullable: true),
                    Resumo = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    VigenciaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ArtigosAfetados = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ImpactoTecnico = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    NotaCurador = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_publicacoes_detectadas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_publicacoes_detectadas_fontes_monitoradas_FonteId",
                        column: x => x.FonteId,
                        principalTable: "fontes_monitoradas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alertas_cs_Chave",
                table: "alertas_cs",
                column: "Chave",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_alertas_cs_EscritorioId_EmpresaId_DispensadoEm",
                table: "alertas_cs",
                columns: new[] { "EscritorioId", "EmpresaId", "DispensadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_chamados_EscritorioId",
                table: "chamados",
                column: "EscritorioId");

            migrationBuilder.CreateIndex(
                name: "IX_chamados_Status_CreatedAt",
                table: "chamados",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_conversas_assistente_UltimaMensagemEm",
                table: "conversas_assistente",
                column: "UltimaMensagemEm");

            migrationBuilder.CreateIndex(
                name: "IX_conversas_assistente_UsuarioId_EmpresaId_UltimaMensagemEm",
                table: "conversas_assistente",
                columns: new[] { "UsuarioId", "EmpresaId", "UltimaMensagemEm" });

            migrationBuilder.CreateIndex(
                name: "IX_eventos_produto_EscritorioId_OcorridoEm",
                table: "eventos_produto",
                columns: new[] { "EscritorioId", "OcorridoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_eventos_produto_Tipo_OcorridoEm",
                table: "eventos_produto",
                columns: new[] { "Tipo", "OcorridoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_fontes_monitoradas_Url",
                table: "fontes_monitoradas",
                column: "Url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interacoes_assistente_EscritorioId_CreatedAt",
                table: "interacoes_assistente",
                columns: new[] { "EscritorioId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_interacoes_assistente_UsuarioId",
                table: "interacoes_assistente",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_lembretes_emissao_Ativo_ProximaEm",
                table: "lembretes_emissao",
                columns: new[] { "Ativo", "ProximaEm" });

            migrationBuilder.CreateIndex(
                name: "IX_lembretes_emissao_EmpresaId_NotaModeloId",
                table: "lembretes_emissao",
                columns: new[] { "EmpresaId", "NotaModeloId" });

            migrationBuilder.CreateIndex(
                name: "IX_mensagens_conversa_ConversaId_CriadaEm",
                table: "mensagens_conversa",
                columns: new[] { "ConversaId", "CriadaEm" });

            migrationBuilder.CreateIndex(
                name: "IX_preferencias_assistente_UsuarioId",
                table: "preferencias_assistente",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_publicacoes_detectadas_FonteId_Hash",
                table: "publicacoes_detectadas",
                columns: new[] { "FonteId", "Hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_publicacoes_detectadas_Status_DetectadaEm",
                table: "publicacoes_detectadas",
                columns: new[] { "Status", "DetectadaEm" });

            migrationBuilder.CreateIndex(
                name: "IX_sinais_produto_Tipo_CreatedAt",
                table: "sinais_produto",
                columns: new[] { "Tipo", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_sugestoes_dispensadas_UsuarioId_Chave",
                table: "sugestoes_dispensadas",
                columns: new[] { "UsuarioId", "Chave" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usos_assistente_EscritorioId_Dia",
                table: "usos_assistente",
                columns: new[] { "EscritorioId", "Dia" });

            migrationBuilder.CreateIndex(
                name: "IX_usos_assistente_UsuarioId_Dia",
                table: "usos_assistente",
                columns: new[] { "UsuarioId", "Dia" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alertas_cs");

            migrationBuilder.DropTable(
                name: "chamados");

            migrationBuilder.DropTable(
                name: "eventos_produto");

            migrationBuilder.DropTable(
                name: "interacoes_assistente");

            migrationBuilder.DropTable(
                name: "lembretes_emissao");

            migrationBuilder.DropTable(
                name: "mensagens_conversa");

            migrationBuilder.DropTable(
                name: "preferencias_assistente");

            migrationBuilder.DropTable(
                name: "publicacoes_detectadas");

            migrationBuilder.DropTable(
                name: "sinais_produto");

            migrationBuilder.DropTable(
                name: "sugestoes_dispensadas");

            migrationBuilder.DropTable(
                name: "usos_assistente");

            migrationBuilder.DropTable(
                name: "conversas_assistente");

            migrationBuilder.DropTable(
                name: "fontes_monitoradas");
        }
    }
}
