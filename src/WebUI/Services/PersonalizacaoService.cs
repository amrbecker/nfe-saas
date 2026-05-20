using NfeSaas.Application.DTOs;
using NfeSaas.Domain.Enums;

namespace NfeSaas.WebUI.Services;

/// <summary>
/// Carrega e cacheia a ConfiguracaoEmpresa da empresa selecionada e expõe **flags semânticas**
/// que a UI usa para esconder/mostrar funcionalidades. Filosofia: configuração simples → UI simples.
///
/// Quando o usuário ainda não passou pelo wizard (ConfiguracaoEmpresa = null), assumimos o modo
/// mais simples possível — nada de ICMS-ST, nada de NFC-e, nada de relatórios avançados.
/// </summary>
public interface IPersonalizacaoService
{
    Task<PerfilSimplificado> ObterPerfilAsync(bool forceRefresh = false);
    Task InvalidarAsync();
}

public record PerfilSimplificado(
    bool ConfiguracaoConcluida,
    PerfilCliente PerfilCliente,
    TipoProduto TipoProduto,
    VolumeNotas VolumeNotas,
    NivelAutomacao NivelAutomacao,
    NivelRelatorio NivelRelatorio,
    bool EmiteParaConsumidorFinal,
    bool OperaIcmsSt)
{
    // ============== Flags derivadas (semânticas) ==============

    // Esconde funcionalidades complexas se o perfil é "pequenas empresas simples"
    // E o usuário não opera com ST nem com consumidor final.
    public bool ModoSimplificado =>
        PerfilCliente == PerfilCliente.PequenasEmpresasSimples
        && !OperaIcmsSt
        && !EmiteParaConsumidorFinal;

    // Mostrar emissão NFC-e (modelo 65) apenas se cliente disse que vende a consumidor final
    public bool MostrarNFCe => EmiteParaConsumidorFinal;

    // Mostrar bloco ICMS-ST nos formulários só se cliente opera com ST
    public bool MostrarIcmsSt => OperaIcmsSt;

    // Mostrar campos avançados (IPI, FCP, DIFAL, observações fiscais detalhadas) apenas para
    // perfil complexo OU produtos complexos
    public bool MostrarTributacaoAvancada =>
        PerfilCliente == PerfilCliente.ClientesExigentesComplexos
        || TipoProduto == TipoProduto.ProdutosComplexos;

    // Inutilização de numeração: feature de operação fiscal "séria", esconder no modo simples
    public bool MostrarInutilizacoes => !ModoSimplificado;

    // Cadastro de Produtos: faz sentido quando o cliente reaproveita itens (semi/automático).
    // No modo manual + serviços, o usuário emite descrição livre — pouca reutilização.
    public bool MostrarCadastroProdutos =>
        NivelAutomacao != NivelAutomacao.Manual
        || TipoProduto != TipoProduto.ServicosBasicos;

    // Cadastro de Clientes/Destinatários: relevante quando volume > 100 ou quando vende a PJ
    public bool MostrarCadastroClientes =>
        VolumeNotas != VolumeNotas.Ate100
        || NivelAutomacao != NivelAutomacao.Manual;

    // Relatórios avançados (SPED, livros fiscais) só no nível Avançado
    public bool MostrarRelatoriosAvancados => NivelRelatorio == NivelRelatorio.Avancado;

    public bool MostrarRelatoriosIntermediarios => NivelRelatorio != NivelRelatorio.Basico;

    // Contingência (SVC-AN/RS, FS-DA): conceito complexo, esconder para usuários simples
    public bool MostrarContingencia => !ModoSimplificado;

    // Carta de Correção: feature útil para todos, mas a UI exibe explicação maior no modo simples
    public bool ExplicacoesDetalhadas => ModoSimplificado;

    public static PerfilSimplificado Default() => new(
        ConfiguracaoConcluida: false,
        PerfilCliente: PerfilCliente.PequenasEmpresasSimples,
        TipoProduto: TipoProduto.ServicosBasicos,
        VolumeNotas: VolumeNotas.Ate100,
        NivelAutomacao: NivelAutomacao.Manual,
        NivelRelatorio: NivelRelatorio.Basico,
        EmiteParaConsumidorFinal: false,
        OperaIcmsSt: false);
}

public class PersonalizacaoService : IPersonalizacaoService
{
    private readonly IConfiguracaoEmpresaService _configService;
    private PerfilSimplificado? _cache;

    public PersonalizacaoService(IConfiguracaoEmpresaService configService) => _configService = configService;

    public async Task<PerfilSimplificado> ObterPerfilAsync(bool forceRefresh = false)
    {
        if (_cache != null && !forceRefresh) return _cache;

        try
        {
            var dto = await _configService.GetAsync();
            if (dto == null)
            {
                _cache = PerfilSimplificado.Default();
                return _cache;
            }

            _cache = new PerfilSimplificado(
                ConfiguracaoConcluida: dto.ConcluidoEm.HasValue,
                PerfilCliente: (PerfilCliente)dto.PerfilCliente,
                TipoProduto: (TipoProduto)dto.TipoProduto,
                VolumeNotas: (VolumeNotas)dto.VolumeNotas,
                NivelAutomacao: (NivelAutomacao)dto.NivelAutomacao,
                NivelRelatorio: (NivelRelatorio)dto.NivelRelatorio,
                EmiteParaConsumidorFinal: dto.EmiteParaConsumidorFinal,
                OperaIcmsSt: dto.OperaIcmsSt);
            return _cache;
        }
        catch
        {
            _cache = PerfilSimplificado.Default();
            return _cache;
        }
    }

    public Task InvalidarAsync()
    {
        _cache = null;
        return Task.CompletedTask;
    }
}
