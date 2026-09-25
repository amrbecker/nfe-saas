using System.Text.Json;
using MediatR;
using NfeSaas.Application.DTOs.Assistente;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Application.Assistente;

// =====================================================================================================
// Contratos compartilhados do assistente Ori. Cada interface tem UM dono de implementação; os demais
// pacotes só consomem. Ver docs/assistente/ (PESQUISA_REFINAMENTO.md, CONTEXTO_AGENTE.md).
// Regra transversal: nada que sai para o modelo contém dado pessoal — tudo passa por ISanitizadorIA.
// =====================================================================================================

/// <summary>Papéis (claim de role) usados pelas telas internas do assistente.</summary>
public static class PapeisAssistente
{
    /// <summary>Equipe NFeFlow (PO): triagem de chamados, saúde das contas, insights.</summary>
    public const string Plataforma = "Plataforma";
    /// <summary>Escritório parceiro: fila de curadoria da base de conhecimento. Não vê dados de clientes.</summary>
    public const string Curador = "Curador";
}

/// <summary>Configuração da seção <c>Assistente</c> (appsettings / env <c>Assistente__*</c>).</summary>
public class AssistenteOptions
{
    /// <summary>Liga/desliga a Ori inteira (inclusive recursos sem IA).</summary>
    public bool Habilitado { get; set; } = true;
    /// <summary>Se não vazio, só esses escritórios veem a Ori (piloto).</summary>
    public List<Guid> EscritoriosPiloto { get; set; } = new();
    /// <summary>Inclui artigos com status "rascunho" nas respostas (dev/homologação). Em produção: false.</summary>
    public bool IncluirRascunhosKb { get; set; }
    public int LimiteDiarioPorUsuario { get; set; } = 40;
    public int LimiteMensalBasico { get; set; } = 150;
    public int LimiteMensalProfissional { get; set; } = 600;
    public int LimiteMensalEnterprise { get; set; } = 2000;
    /// <summary>E-mail do PO para o relatório semanal de insights e alertas internos.</summary>
    public string? EmailPo { get; set; }
    /// <summary>E-mail do escritório parceiro (curador) para o resumo semanal da fila de curadoria.</summary>
    public string? EmailCurador { get; set; }
    /// <summary>URL base da WebUI (links em e-mails). Default: WebUI:BaseUrl.</summary>
    public string? WebUiBaseUrl { get; set; }
    public IaOptions Ia { get; set; } = new();
    public InlabsOptions Inlabs { get; set; } = new();
}

public class IaOptions
{
    /// <summary>Rota "Conversa" — tudo que envolve dado de cliente. Endpoint compatível com OpenAI (Microsoft Foundry).</summary>
    public EndpointIaOptions Conversa { get; set; } = new();
    /// <summary>Rota "FontesPublicas" — só dado público (DOU, NTs) ou já anonimizado. Pode ser a API direta da DeepSeek.</summary>
    public EndpointIaOptions FontesPublicas { get; set; } = new();
}

public class EndpointIaOptions
{
    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
    public string Modelo { get; set; } = "deepseek-flash";
    /// <summary>Preço por 1M tokens (US$) — para custo estimado e painel. Ajustar ao preço real do endpoint.</summary>
    public decimal PrecoEntradaSemCache { get; set; } = 0.15m;
    public decimal PrecoEntradaComCache { get; set; } = 0.003m;
    public decimal PrecoSaida { get; set; } = 0.60m;
}

public class InlabsOptions
{
    /// <summary>Credenciais do INLABS (Imprensa Nacional) para baixar o XML do DOU. Sem elas, a fonte DOU fica inativa.</summary>
    public string? Email { get; set; }
    public string? Senha { get; set; }
}

// ---------------------------------------------------------------- Base de conhecimento (dono: P1)

public record FonteArtigo(string Documento, string? Url, NivelFonte Nivel);

public record ArtigoKb(
    string Id,
    string Titulo,
    string Categoria,
    NivelFonte NivelFonte,
    IReadOnlyList<string> CodigosRejeicao,
    IReadOnlyList<string> CamposRelacionados,
    IReadOnlyList<string> Telas,
    IReadOnlyList<FonteArtigo> Fontes,
    DateOnly? VigenciaInicio,
    DateOnly? VigenciaFim,
    DateOnly? VerificadoEm,
    DateOnly? RevisarAte,
    string? Curador,
    string Status,          // rascunho | revisado | publicado | em_revisao
    string ResumoCurto,
    string Corpo);          // markdown sem o front matter

public record CriterioRoteamentoKb(
    IReadOnlyCollection<string>? CodigosRejeicao = null,
    string? Campo = null,
    string? Tela = null,
    string? Termo = null);

public interface IBaseConhecimento
{
    /// <summary>Todos os artigos, inclusive rascunhos (curadoria).</summary>
    IReadOnlyList<ArtigoKb> Todos();
    /// <summary>Artigos que podem ser usados em respostas: publicado e em_revisao (+ rascunho se AssistenteOptions.IncluirRascunhosKb).</summary>
    IReadOnlyList<ArtigoKb> Utilizaveis();
    ArtigoKb? Obter(string id);
    /// <summary>Roteamento determinístico (código de rejeição → campo → tela → termo), sem LLM. Até <paramref name="maximo"/> artigos.</summary>
    IReadOnlyList<ArtigoKb> Rotear(CriterioRoteamentoKb criterio, int maximo = 3);
    /// <summary>Extrai o código de rejeição (cStat) de um MotivoRejeicao, ex.: "Rejeição 778: ..." → "778".</summary>
    string? ExtrairCodigoRejeicao(string? motivoRejeicao);
}

// ---------------------------------------------------------------- IA (dono: P3)

public enum RotaIa
{
    /// <summary>Endpoint aprovado para dado de cliente (Microsoft Foundry).</summary>
    Conversa = 1,
    /// <summary>Somente dado público ou já anonimizado (DOU, NTs, sinais agregados).</summary>
    FontesPublicas = 2
}

public enum PapelIa { Sistema = 1, Usuario = 2, Assistente = 3 }

public record MensagemIa(PapelIa Papel, string Texto);

/// <summary>
/// Ferramenta exposta ao modelo. <see cref="Executar"/> roda no servidor com o escopo (EmpresaId etc.) já
/// capturado na closure — NUNCA vindo dos argumentos do modelo. O retorno deve ser texto/JSON já sanitizado.
/// </summary>
public record FerramentaIa(
    string Nome,
    string Descricao,
    string JsonSchemaParametros,
    Func<JsonElement, CancellationToken, Task<string>> Executar);

public record OpcoesIa(
    int MaxTokens,
    bool Raciocinio,
    IReadOnlyList<FerramentaIa>? Ferramentas = null,
    int MaxIteracoesFerramentas = 5,
    IProgress<ChamadaFerramentaIa>? Progresso = null);

public record UsoIa(long TokensEntrada, long TokensCache, long TokensSaida)
{
    public static readonly UsoIa Zero = new(0, 0, 0);
    public UsoIa Somar(UsoIa o) => new(TokensEntrada + o.TokensEntrada, TokensCache + o.TokensCache, TokensSaida + o.TokensSaida);
}

public record ChamadaFerramentaIa(string Nome, string ArgumentosJson, string Resultado);

public record RespostaIa(string Texto, UsoIa Uso, string Modelo, IReadOnlyList<ChamadaFerramentaIa> Ferramentas);

public interface IAssistenteIA
{
    /// <summary>False quando o endpoint/chave da rota não está configurado — recursos com IA ficam ocultos.</summary>
    bool EstaHabilitado(RotaIa rota);
    string Modelo(RotaIa rota);
    /// <summary>Uma resposta completa (sem streaming de tokens: toda resposta fiscal passa pelo verificador antes de ser exibida).</summary>
    Task<RespostaIa> CompletarAsync(RotaIa rota, IReadOnlyList<MensagemIa> mensagens, OpcoesIa opcoes, CancellationToken ct = default);
    decimal CustoEstimadoUsd(RotaIa rota, UsoIa uso);
}

// ---------------------------------------------------------------- Sanitização (dono: P3)

public sealed record ResultadoSanitizacao(string Texto, IReadOnlyDictionary<string, string> Substituicoes, int Quantidade);

/// <summary>Atributos de uma pessoa/empresa que o modelo pode ver no lugar do documento (PESQUISA_REFINAMENTO.md §2.6).</summary>
public record AtributosPessoa(string Tipo, bool DocValido, string? Uf, bool IePresente, bool? IeFormatoValidoUf, int? IndIe);

public interface ISanitizadorIA
{
    /// <summary>
    /// Troca CPF, CNPJ, e-mail, telefone, chave de acesso e CEP por marcadores ([CPF_1], [EMAIL_1]...).
    /// <paramref name="mapa"/> permite reaproveitar marcadores dentro da mesma requisição (mesmo valor → mesmo marcador).
    /// </summary>
    ResultadoSanitizacao Sanitizar(string? texto, IDictionary<string, string>? mapa = null);
    /// <summary>Troca marcadores pelos valores reais — só para exibir ao usuário, nunca para enviar ao modelo.</summary>
    string Reidratar(string texto, IReadOnlyDictionary<string, string> substituicoes);
    AtributosPessoa DescreverPessoa(string? cpfCnpj, string? uf, string? inscricaoEstadual, int? indicadorIe = null);
}

// ---------------------------------------------------------------- Cotas (dono: P3)

public interface ICotaAssistente
{
    Task<StatusCotaDto> ObterAsync(Guid usuarioId, Guid escritorioId, CancellationToken ct = default);
    Task RegistrarAsync(Guid usuarioId, Guid escritorioId, UsoIa uso, decimal custoUsd, CancellationToken ct = default);
}

// ---------------------------------------------------------------- Requests MediatR entre pacotes

/// <summary>A6 — padrões de preenchimento da empresa (dono: P5). Usado pela ferramenta consultar_padroes (P3).</summary>
public record GetPadroesPreenchimentoQuery(Guid EmpresaId, string? DestinatarioCpfCnpj, string? ProdutoCodigo) : IRequest<PadroesPreenchimentoDto>;

/// <summary>Registra sinal de produto já sanitizado (dono: P7). Usado pela ferramenta registrar_sinal (P3) e pela UI.</summary>
public record RegistrarSinalCommand(Guid? EscritorioId, Guid? EmpresaId, Guid? UsuarioId, TipoSinalProduto Tipo,
    string Resumo, string? Tela, string? Tema) : IRequest<Guid>;

/// <summary>Cria chamado confirmado pelo usuário (dono: P7).</summary>
public record CriarChamadoCommand(Guid EscritorioId, Guid EmpresaId, Guid UsuarioId, CriarChamadoDto Dto) : IRequest<Guid>;
