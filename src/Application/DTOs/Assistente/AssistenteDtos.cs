using NfeSaas.Domain.Enums;

namespace NfeSaas.Application.DTOs.Assistente;

// DTOs do assistente Ori compartilhados entre API e WebUI (a WebUI referencia Application).
// Cada pacote pode criar DTOs próprios em arquivos novos nesta pasta; estes são os contratos comuns.

// ---------------------------------------------------------------- Contexto da sessão (CAPTURA_CONTEXTO.md §2)

public record OperacaoDto(string Tipo, string? Etapa = null, int? Item = null, int? Itens = null, int? Modelo = null);

/// <summary>Campo com foco. <see cref="Valor"/> só é enviado para campos fiscais (CFOP, CST, NCM…); nunca para documento/nome/endereço.</summary>
public record FocoDto(string Campo, string? Rotulo, string? Valor, int? Item = null);

/// <summary>Destinatário como atributos (nunca documento ou nome).</summary>
public record DestinatarioAtributosDto(string? Tipo, bool? DocValido, string? Uf, int? IndIe, bool? IeFormatoValidoUf);

public record ErroCampoDto(string Campo, string Mensagem, int? Item = null);

public record ErroApiDto(string Rota, int Status, string? Codigo, string? Mensagem, int HaSegundos);

public record ContextoTelaDto(
    string Tela,
    OperacaoDto? Op = null,
    FocoDto? Foco = null,
    DestinatarioAtributosDto? Dest = null,
    List<ErroCampoDto>? Erros = null,
    List<ErroApiDto>? Api = null,
    Guid? NotaId = null,
    List<string>? Rastro = null,
    string? SentryEventId = null);

// ---------------------------------------------------------------- Base de conhecimento (P1)

public record ArtigoResumoDto(
    string Id, string Titulo, string Categoria, NivelFonte NivelFonte, string Status, string ResumoCurto,
    List<string> CodigosRejeicao, List<string> Campos, List<string> Telas, DateOnly? VerificadoEm);

public record ArtigoDetalheDto(ArtigoResumoDto Resumo, string CorpoMarkdown, List<FonteArtigoDto> Fontes, string? Curador,
    DateOnly? VigenciaInicio, DateOnly? VigenciaFim, DateOnly? RevisarAte);

public record FonteArtigoDto(string Documento, string? Url, NivelFonte Nivel);

public record MapaKbDto(List<ArtigoResumoDto> Artigos);

// ---------------------------------------------------------------- Respostas da Ori (P3 ↔ P4b)

public record StatusCotaDto(bool Permitido, int UsadasHoje, int LimiteDia, int UsadasMes, int LimiteMes, string? Motivo);

public record StatusAssistenteDto(bool Habilitado, bool IaHabilitada, bool DicasSilenciadas, List<string> ChavesBloqueadas, StatusCotaDto Cota);

public record CitacaoDto(string Id, string Titulo, NivelFonte Nivel, DateOnly? VerificadoEm, string Status);

/// <summary>
/// Ação que a Ori PREPARA para o usuário concluir (nunca executa). Tipos:
/// "preparar_nota" (origemNotaId, ajustes), "preparar_cadastro" (tipo=Cliente|Produto, origemNotaId, itens),
/// "propor_lembrete" (origemNotaId, periodicidade, dia), "sugerir_personalizacao" (flag, valor, motivo),
/// "confirmar_chamado" (titulo, descricao, passos, severidade, notaId), "confirmar_sinal" (tipo, resumo, tela),
/// "abrir_rota" (rota).
/// </summary>
public record AcaoUiDto(string Tipo, string Resumo, Dictionary<string, string> Parametros);

public record RespostaOriDto(
    Guid? InteracaoId,
    string Texto,                   // markdown já reidratado para exibição
    NivelFonte Selo,
    List<CitacaoDto> Citacoes,
    bool VerificacaoFalhou,
    bool RespondidaSemModelo,
    int DadosPessoaisRemovidos,
    List<AcaoUiDto> Acoes,
    StatusCotaDto? Cota);

public record PerguntaOriDto(string Pergunta, ContextoTelaDto Contexto);

public record ExplicarRejeicaoDto(ContextoTelaDto? Contexto);

public record AvaliacaoDto(int Valor);

public record ConversaResumoDto(Guid Id, string Titulo, DateTime UltimaMensagemEm);

public record MensagemConversaDto(string Papel, string Texto, DateTime CriadaEm);

public record NovaMensagemDto(string Texto, ContextoTelaDto Contexto);

/// <summary>Evento SSE da conversa: "status" (texto de progresso), "ferramenta" (nome), "resposta" (final), "erro".</summary>
public record EventoConversaDto(string Tipo, string? Texto, RespostaOriDto? Resposta);

// ---------------------------------------------------------------- Preferências e dicas (P4a)

public record PreferenciasOriDto(bool DicasSilenciadas);

public record SugestaoIgnoradaDto(string Chave, bool Definitivo);

// ---------------------------------------------------------------- Automações (P5)

public record PadraoItemDto(string? ProdutoCodigo, string Cfop, string? CstCsosn, string? NaturezaOperacao, int Frequencia, int Total);

public record PadroesPreenchimentoDto(List<PadraoItemDto> Padroes, int NotasAnalisadas);

// ---------------------------------------------------------------- CS, alertas, lembretes, telemetria (P6)

public record AlertaCsDto(Guid Id, TipoAlertaCs Tipo, string Mensagem, string? LinkAcao, DateTime CriadoEm, bool Visto, Guid? EmpresaId);

public record LembreteDto(Guid Id, Guid NotaModeloId, string Descricao, PeriodicidadeLembrete Periodicidade, int Dia, DateTime ProximaEm, bool Ativo);

public record CriarLembreteDto(Guid NotaModeloId, string Descricao, PeriodicidadeLembrete Periodicidade, int Dia);

public record EventoProdutoDto(string Tipo, string? Tela, string? DadosJson, DateTime OcorridoEm);

public record LoteEventosDto(List<EventoProdutoDto> Eventos);

// ---------------------------------------------------------------- Chamados, sinais, curadoria (P7)

public record CriarChamadoDto(string Titulo, string Descricao, string? Passos, SeveridadeChamado Severidade, Guid? NotaId, ContextoTelaDto? Contexto);

public record CriarSinalDto(TipoSinalProduto Tipo, string Resumo, string? Tela);
