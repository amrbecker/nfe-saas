using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using NfeSaas.Application.Assistente;
using NfeSaas.Application.DTOs.Assistente;

namespace NfeSaas.WebUI.Services.Assistente;

/// <summary>
/// Contexto da sessão para a Ori (CAPTURA_CONTEXTO.md): rastro semântico circular, campo com foco e últimos erros da API.
/// Tudo só em memória — nada vai para localStorage e nada sai do navegador até o usuário chamar a Ori.
/// </summary>
public class ContextoAssistente : IContextoAssistente, IDisposable
{
    private const int TamanhoRastro = 50;
    private const int TamanhoErros = 5;

    private readonly NavigationManager _nav;
    private readonly LinkedList<string> _rastro = new();
    private readonly LinkedList<(ErroApiDto Erro, DateTime Quando)> _erros = new();
    private readonly List<IContextoTela> _telas = new();

    public ContextoAssistente(NavigationManager nav)
    {
        _nav = nav;
        _nav.LocationChanged += AoNavegar;
        RegistrarAcao($"abriu {TelaDaRota(_nav.Uri)}");
    }

    public event Action? OnFocoAlterado;
    public FocoDto? FocoAtual { get; private set; }

    public void RegistrarTela(IContextoTela tela)
    {
        _telas.Remove(tela);
        _telas.Add(tela);
    }

    public void RemoverTela(IContextoTela tela) => _telas.Remove(tela);

    public void RegistrarAcao(string descricao)
    {
        if (string.IsNullOrWhiteSpace(descricao)) return;
        _rastro.AddLast(descricao.Length > 120 ? descricao[..120] : descricao);
        while (_rastro.Count > TamanhoRastro) _rastro.RemoveFirst();
    }

    public void RegistrarFoco(string campo, string? rotulo, string? valor, int? item = null)
    {
        // A lista de bloqueio também é aplicada no JS; aqui é a segunda barreira.
        FocoAtual = new FocoDto(campo, rotulo, CamposOri.EhSensivel(campo) ? null : valor, item);
        OnFocoAlterado?.Invoke();
    }

    public void RegistrarErroApi(ErroApiDto erro)
    {
        _erros.AddLast((erro, DateTime.UtcNow));
        while (_erros.Count > TamanhoErros) _erros.RemoveFirst();
    }

    public ContextoTelaDto Capturar()
    {
        var tela = _telas.LastOrDefault()?.ObterContexto();
        var agora = DateTime.UtcNow;
        string? sentry = null;
        try { var id = Sentry.SentrySdk.LastEventId; if (id != Sentry.SentryId.Empty) sentry = id.ToString(); } catch { /* SDK inativo */ }

        return new ContextoTelaDto(
            Tela: tela?.Tela ?? TelaDaRota(_nav.Uri),
            Op: tela?.Op,
            Foco: FocoAtual,
            Dest: tela?.Dest,
            Erros: tela?.Erros,
            Api: _erros.Select(e => e.Erro with { HaSegundos = (int)(agora - e.Quando).TotalSeconds }).ToList(),
            NotaId: tela?.NotaId,
            Rastro: _rastro.TakeLast(8).ToList(),
            SentryEventId: sentry);
    }

    private void AoNavegar(object? sender, LocationChangedEventArgs e)
    {
        FocoAtual = null;
        RegistrarAcao($"abriu {TelaDaRota(e.Location)}");
    }

    /// <summary>Rota → ID canônico de tela (CONTRATOS_TECNICOS.md §3).</summary>
    public static string TelaDaRota(string uri)
    {
        var caminho = new Uri(uri, UriKind.RelativeOrAbsolute).IsAbsoluteUri ? new Uri(uri).AbsolutePath : uri.Split('?')[0];
        caminho = caminho.Trim('/').ToLowerInvariant();
        return caminho switch
        {
            "" => "dashboard",
            "emitir" => "emitir-nfe",
            "notas" => "notas",
            _ when caminho.StartsWith("notas/") => "nota-detalhe",
            "produtos" or "clientes" or "empresa" or "empresas" or "configuracao-inicial" or "certificado"
                or "inutilizacoes" or "usuarios" or "escritorio-como-empresa" or "lembretes" or "login" => caminho,
            _ => caminho.Split('/')[0]
        };
    }

    public void Dispose() => _nav.LocationChanged -= AoNavegar;
}
