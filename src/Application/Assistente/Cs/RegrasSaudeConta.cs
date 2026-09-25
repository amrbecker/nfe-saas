using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces.Assistente;
using NfeSaas.Domain.Services;

namespace NfeSaas.Application.Assistente.Cs;

public record AlertaProposto(Guid EscritorioId, Guid? EmpresaId, TipoAlertaCs Tipo, string Chave, string Mensagem, string? Link,
    bool Interno = false);

/// <summary>Fotografia de uma empresa para avaliação das regras (montada pelo worker).</summary>
public record FotoEmpresa(EmpresaSaudeAssistente Empresa, IReadOnlyList<NotaResumoAssistente> NotasRecentes,
    bool TeveAutorizada, bool TeveAutorizadaHomologacao, bool TeveAutorizadaProducao);

/// <summary>
/// Gatilhos proativos de Customer Success (CONTEXTO_AGENTE.md §6) — determinísticos, sem LLM. Cada alerta tem uma
/// chave estável: a mesma situação nunca gera dois alertas.
/// </summary>
public static class RegrasSaudeConta
{
    public static readonly int[] MarcosCertificado = { 30, 15, 7, 1 };
    public static readonly int[] MarcosTrial = { 7, 2 };

    public static IEnumerable<AlertaProposto> AvaliarEmpresa(FotoEmpresa f, DateTime agora, Func<string?, string?> extrairCodigo)
    {
        var e = f.Empresa;
        var hoje = agora.Date;

        // Certificado vencendo (30/15/7/1) ou vencido.
        if (e.CertificadoValidade is { } validade)
        {
            var dias = (int)Math.Floor((validade.Date - hoje).TotalDays);
            if (dias < 0)
                yield return new(e.EscritorioId, e.EmpresaId, TipoAlertaCs.CertificadoVencendo, $"cert:{e.EmpresaId}:{validade:yyyyMMdd}:vencido",
                    $"O certificado digital de {e.Nome} venceu. Sem ele não é possível emitir notas.", "/certificado");
            else if (MarcosCertificado.Where(m => dias <= m).DefaultIfEmpty(-1).Min() is var marco and >= 0)
                yield return new(e.EscritorioId, e.EmpresaId, TipoAlertaCs.CertificadoVencendo, $"cert:{e.EmpresaId}:{validade:yyyyMMdd}:{marco}",
                    dias == 0 ? $"O certificado digital de {e.Nome} vence hoje." : $"O certificado digital de {e.Nome} vence em {dias} dia{(dias == 1 ? "" : "s")}.",
                    "/certificado");
        }

        // Mesma rejeição 3+ vezes em 7 dias.
        var semana = ISOWeek.GetYear(hoje) * 100 + ISOWeek.GetWeekOfYear(hoje);
        foreach (var g in f.NotasRecentes
                     .Where(n => n.Situacao == SituacaoNota.Rejeitada && n.DataEmissao >= agora.AddDays(-7))
                     .GroupBy(n => extrairCodigo(n.MotivoRejeicao))
                     .Where(g => g.Key != null && g.Count() >= 3))
        {
            yield return new(e.EscritorioId, e.EmpresaId, TipoAlertaCs.RejeicaoRepetida, $"rej:{e.EmpresaId}:{g.Key}:{semana}",
                $"A rejeição {g.Key} apareceu {g.Count()} vezes esta semana em {e.Nome}. Quer ver como resolver?", "/notas");
        }

        // Onboarding parado: 3 dias sem configuração concluída ou sem nenhuma nota autorizada.
        if (e.CriadaEm < agora.AddDays(-3) && (!e.ConfiguracaoConcluida || !f.TeveAutorizada))
        {
            var faltam = new List<string>();
            if (!e.TemCertificado) faltam.Add("certificado");
            if (!e.ConfiguracaoConcluida) faltam.Add("configuração inicial");
            if (!f.TeveAutorizada) faltam.Add("primeira nota");
            yield return new(e.EscritorioId, e.EmpresaId, TipoAlertaCs.OnboardingParado, $"onb:{e.EmpresaId}",
                $"{e.Nome}: falta{(faltam.Count > 1 ? "m" : "")} {string.Join(", ", faltam)} para começar a emitir.", "/");
        }

        // Só homologação há mais de 14 dias.
        if (e.Ambiente == AmbienteSefaz.Homologacao && e.CriadaEm < agora.AddDays(-14) && f.TeveAutorizadaHomologacao && !f.TeveAutorizadaProducao)
            yield return new(e.EscritorioId, e.EmpresaId, TipoAlertaCs.SoHomologacao, $"homo:{e.EmpresaId}",
                $"{e.Nome} já emite em homologação há algum tempo. Pronto para produção?", "/configuracao-inicial");

        // Nota recorrente (mesmo destinatário e mesmos produtos, intervalo mensal).
        foreach (var recorrente in DetectarRecorrencias(f.NotasRecentes))
            yield return new(e.EscritorioId, e.EmpresaId, TipoAlertaCs.NotaRecorrente, $"rec:{e.EmpresaId}:{recorrente.Assinatura}",
                $"A nota {recorrente.UltimaNumero} parece se repetir todo mês. Quer um lembrete para emitir de novo?",
                $"/lembretes?notaModelo={recorrente.UltimaId}");

        // Uso caindo (interno): última semana < 50% da média das 4 anteriores.
        var autorizadas = f.NotasRecentes.Where(n => n.Situacao == SituacaoNota.Autorizada).ToList();
        var ultimaSemana = autorizadas.Count(n => n.DataEmissao >= agora.AddDays(-7));
        var media4 = autorizadas.Count(n => n.DataEmissao < agora.AddDays(-7) && n.DataEmissao >= agora.AddDays(-35)) / 4.0;
        if (media4 >= 5 && ultimaSemana < media4 * 0.5)
            yield return new(e.EscritorioId, e.EmpresaId, TipoAlertaCs.UsoCaindo, $"uso:{e.EmpresaId}:{semana}",
                $"{e.Nome}: {ultimaSemana} notas na última semana contra média de {media4:0.#}.", null, Interno: true);

        // A12: notas rejeitadas ou pendentes no fim do mês.
        if (hoje.Day >= 25)
        {
            var pendentes = f.NotasRecentes.Count(n => n.DataEmissao.Month == hoje.Month && n.DataEmissao.Year == hoje.Year
                && n.Situacao is SituacaoNota.Rejeitada or SituacaoNota.PendenteRetransmissao);
            if (pendentes > 0)
                yield return new(e.EscritorioId, e.EmpresaId, TipoAlertaCs.RejeicaoRepetida, $"a12:{e.EmpresaId}:{hoje:yyyyMM}",
                    $"{e.Nome} tem {pendentes} nota{(pendentes > 1 ? "s" : "")} rejeitada{(pendentes > 1 ? "s" : "")} ou pendente{(pendentes > 1 ? "s" : "")} neste mês.", "/notas");
        }
    }

    public static IEnumerable<AlertaProposto> AvaliarEscritorio(Escritorio esc, DateTime agora)
    {
        if (esc.CalcularStatusAssinatura(agora) != StatusAssinaturaEscritorio.TrialAtivo) yield break;
        var dias = esc.DiasRestantesTrial(agora);
        var marco = MarcosTrial.Where(m => dias <= m).DefaultIfEmpty(-1).Min();
        if (marco >= 0)
            yield return new(esc.Id, null, TipoAlertaCs.TrialAcabando, $"trial:{esc.Id}:{marco}",
                dias <= 0 ? "Seu período de teste termina hoje." : $"Seu período de teste termina em {dias} dia{(dias == 1 ? "" : "s")}.", "/");
    }

    public record Recorrencia(string Assinatura, Guid UltimaId, int UltimaNumero);

    /// <summary>Mesmo destinatário + mesmo conjunto de produtos, ≥ 3 ocorrências com intervalos de 25–35 dias.</summary>
    public static IEnumerable<Recorrencia> DetectarRecorrencias(IEnumerable<NotaResumoAssistente> notas)
    {
        var grupos = notas.Where(n => n.Situacao == SituacaoNota.Autorizada && !string.IsNullOrWhiteSpace(n.DestinatarioCpfCnpj))
            .GroupBy(n => CnpjValidator.ApenasDigitos(n.DestinatarioCpfCnpj) + "|" + string.Join(",", n.Itens.Select(i => i.CodigoProduto).Distinct().OrderBy(c => c)));
        foreach (var g in grupos)
        {
            var ordenadas = g.OrderBy(n => n.DataEmissao).ToList();
            if (ordenadas.Count < 3) continue;
            var intervalos = ordenadas.Zip(ordenadas.Skip(1), (a, b) => (b.DataEmissao - a.DataEmissao).TotalDays).ToList();
            if (intervalos.All(d => d is >= 25 and <= 35))
            {
                var ultima = ordenadas[^1];
                yield return new Recorrencia(Hash(g.Key), ultima.Id, ultima.Numero);
            }
        }
    }

    private static string Hash(string s) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)))[..16];
}
