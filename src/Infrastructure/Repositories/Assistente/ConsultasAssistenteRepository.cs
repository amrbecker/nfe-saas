using Microsoft.EntityFrameworkCore;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces.Assistente;
using NfeSaas.Infrastructure.Data;

namespace NfeSaas.Infrastructure.Repositories.Assistente;

public class ConsultasAssistenteRepository : IConsultasAssistenteRepository
{
    private readonly NfeDbContext _ctx;
    public ConsultasAssistenteRepository(NfeDbContext ctx) => _ctx = ctx;

    public async Task<List<NotaResumoAssistente>> NotasAsync(Guid empresaId, DateTime desde, SituacaoNota? situacao, int limite,
        CancellationToken ct = default) =>
        await _ctx.NotasFiscais.AsNoTracking()
            .Where(n => n.EmpresaId == empresaId && n.DataEmissao >= desde && (situacao == null || n.Situacao == situacao))
            .OrderByDescending(n => n.DataEmissao)
            .Take(limite)
            .Select(n => new NotaResumoAssistente(n.Id, n.EmpresaId, n.Tipo, n.Numero, n.DestinatarioCpfCnpj, n.DestinatarioUf,
                n.Situacao, n.Ambiente, n.DataEmissao, n.MotivoRejeicao,
                n.Itens.OrderBy(i => i.NumeroItem).Select(i => new ItemResumoAssistente(i.NumeroItem, i.CodigoProduto, i.Descricao,
                    i.Ncm, i.Cest, i.Cfop, (int)i.CstIcms, (int?)i.CsosnIcms, i.UnidadeComercial)).ToList()))
            .ToListAsync(ct);

    public async Task<List<EmpresaSaudeAssistente>> EmpresasAtivasAsync(CancellationToken ct = default)
    {
        var configuradas = _ctx.ConfiguracoesEmpresa.Where(c => c.ConcluidoEm != null).Select(c => c.EmpresaId);
        return await _ctx.Empresas.AsNoTracking()
            .Where(e => e.Ativo)
            .Select(e => new EmpresaSaudeAssistente(e.Id, e.EscritorioId, e.NomeFantasia != "" ? e.NomeFantasia : e.RazaoSocial, e.CreatedAt, e.CertificadoValidade,
                e.CertificadoBytes != null, configuradas.Contains(e.Id), e.AmbienteSefaz))
            .ToListAsync(ct);
    }

    public Task<List<Escritorio>> EscritoriosAtivosAsync(CancellationToken ct = default) =>
        _ctx.Escritorios.AsNoTracking().Where(e => e.Ativo).ToListAsync(ct);

    public Task<List<string>> EmailsAdminsAsync(Guid escritorioId, CancellationToken ct = default) =>
        _ctx.Usuarios.AsNoTracking()
            .Where(u => u.EscritorioId == escritorioId && u.Ativo && u.Role == "Admin")
            .Select(u => u.Email).ToListAsync(ct);

    public Task<bool> TeveNotaAutorizadaAsync(Guid empresaId, AmbienteSefaz? ambiente, CancellationToken ct = default) =>
        _ctx.NotasFiscais.AsNoTracking().AnyAsync(n => n.EmpresaId == empresaId && n.Situacao == SituacaoNota.Autorizada
            && (ambiente == null || n.Ambiente == ambiente), ct);

    public async Task<HashSet<string>> CodigosProdutosExistentesAsync(Guid empresaId, IEnumerable<string> codigos, CancellationToken ct = default)
    {
        var lista = codigos.Distinct().ToList();
        var existentes = await _ctx.Produtos.AsNoTracking()
            .Where(p => p.EmpresaId == empresaId && lista.Contains(p.Codigo))
            .Select(p => p.Codigo).ToListAsync(ct);
        return existentes.ToHashSet();
    }
}
