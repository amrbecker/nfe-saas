using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Domain.Interfaces;

public interface INotaFiscalRepository
{
    Task<NotaFiscal?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<NotaFiscal?> GetByChaveAcessoAsync(string chaveAcesso, CancellationToken ct = default);
    Task<NotaFiscal?> GetBySerieNumeroAsync(Guid empresaId, TipoNota tipo, int serie, int numero, AmbienteSefaz ambiente, CancellationToken ct = default);
    Task<IEnumerable<NotaFiscal>> GetByEmpresaAsync(Guid empresaId, int pagina, int tamanhoPagina, CancellationToken ct = default);
    Task<int> CountByEmpresaAsync(Guid empresaId, CancellationToken ct = default);
    Task<IEnumerable<NotaFiscal>> GetByPeriodoAsync(Guid empresaId, DateTime inicio, DateTime fim, CancellationToken ct = default);
    Task<decimal> GetTotalEmitidoMesAsync(Guid empresaId, int ano, int mes, CancellationToken ct = default);
    Task<Dictionary<SituacaoNota, int>> GetContagemPorSituacaoAsync(Guid empresaId, CancellationToken ct = default);
    Task<IEnumerable<NotaFiscal>> GetElegiveisDescarteAsync(Guid empresaId, CancellationToken ct = default);
    Task AddAsync(NotaFiscal nota, CancellationToken ct = default);
    Task UpdateAsync(NotaFiscal nota, CancellationToken ct = default);
}

public interface IEmpresaRepository
{
    Task<Empresa?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Empresa?> GetByCnpjAsync(string cnpj, CancellationToken ct = default);
    Task<IEnumerable<Empresa>> GetByEscritorioAsync(Guid escritorioId, CancellationToken ct = default);
    Task AddAsync(Empresa empresa, CancellationToken ct = default);
    Task UpdateAsync(Empresa empresa, CancellationToken ct = default);
}

public interface IEscritorioRepository
{
    Task<Escritorio?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Escritorio?> GetByCnpjAsync(string cnpj, CancellationToken ct = default);
    Task AddAsync(Escritorio escritorio, CancellationToken ct = default);
    Task UpdateAsync(Escritorio escritorio, CancellationToken ct = default);
}

public interface IUsuarioRepository
{
    Task<Usuario?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<Usuario?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<IEnumerable<Usuario>> GetByEscritorioAsync(Guid escritorioId, CancellationToken ct = default);
    Task AddAsync(Usuario usuario, CancellationToken ct = default);
    Task UpdateAsync(Usuario usuario, CancellationToken ct = default);
}

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log, CancellationToken ct = default);
    Task<IEnumerable<AuditLog>> GetByEmpresaAsync(Guid empresaId, int pagina, int tamanho, CancellationToken ct = default);
}

public interface IProdutoRepository
{
    Task<Produto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Produto?> GetByCodigoAsync(Guid empresaId, string codigo, CancellationToken ct = default);
    Task<IEnumerable<Produto>> GetByEmpresaAsync(Guid empresaId, bool apenasAtivos, CancellationToken ct = default);
    Task AddAsync(Produto produto, CancellationToken ct = default);
    Task UpdateAsync(Produto produto, CancellationToken ct = default);
}

public interface IClienteRepository
{
    Task<Cliente?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Cliente?> GetByCpfCnpjAsync(Guid empresaId, string cpfCnpj, CancellationToken ct = default);
    Task<IEnumerable<Cliente>> GetByEmpresaAsync(Guid empresaId, bool apenasAtivos, CancellationToken ct = default);
    Task AddAsync(Cliente cliente, CancellationToken ct = default);
    Task UpdateAsync(Cliente cliente, CancellationToken ct = default);
}

public interface IEventoFiscalRepository
{
    Task<EventoFiscal?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<EventoFiscal>> GetByChaveAcessoAsync(Guid empresaId, string chaveAcesso, CancellationToken ct = default);
    Task<int> CountCcePorChaveAsync(Guid empresaId, string chaveAcesso, CancellationToken ct = default);
    Task<IEnumerable<EventoFiscal>> GetInutilizacoesAsync(Guid empresaId, AmbienteSefaz ambiente, CancellationToken ct = default);
    Task<EventoFiscal?> GetInutilizacaoConflitoAsync(Guid empresaId, AmbienteSefaz ambiente, int ano, TipoNota tipo, int serie, int numIni, int numFin, CancellationToken ct = default);
    Task AddAsync(EventoFiscal evento, CancellationToken ct = default);
    Task UpdateAsync(EventoFiscal evento, CancellationToken ct = default);
}

public interface INcmRepository
{
    Task<Ncm?> GetByCodigoAsync(string codigo, CancellationToken ct = default);
    Task<IEnumerable<Ncm>> BuscarAsync(string termo, int limite, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task<string?> GetVersaoTabelaAtualAsync(CancellationToken ct = default);
    Task AddAsync(Ncm ncm, CancellationToken ct = default);
    Task UpsertManyAsync(IEnumerable<Ncm> ncms, string versaoTabela, CancellationToken ct = default);
}

public interface ICnaeRepository
{
    Task<Cnae?> GetByCodigoAsync(string codigo, CancellationToken ct = default);
    Task<IEnumerable<Cnae>> BuscarAsync(string termo, int limite, CancellationToken ct = default);
}

public interface IConfiguracaoEmpresaRepository
{
    Task<ConfiguracaoEmpresa?> GetByEmpresaAsync(Guid empresaId, CancellationToken ct = default);
    Task AddAsync(ConfiguracaoEmpresa configuracao, CancellationToken ct = default);
    Task UpdateAsync(ConfiguracaoEmpresa configuracao, CancellationToken ct = default);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
