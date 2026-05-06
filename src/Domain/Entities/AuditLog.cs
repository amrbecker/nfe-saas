namespace NfeSaas.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid EmpresaId { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public string Acao { get; private set; } = null!;
    public string? ChaveNFe { get; private set; }
    public string? Detalhes { get; private set; }
    public string? IpOrigem { get; private set; }
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;

    protected AuditLog() { }

    public static AuditLog Criar(Guid empresaId, string acao, Guid? usuarioId = null,
        string? chaveNfe = null, string? detalhes = null, string? ipOrigem = null)
    {
        return new AuditLog
        {
            EmpresaId = empresaId,
            UsuarioId = usuarioId,
            Acao = acao,
            ChaveNFe = chaveNfe,
            Detalhes = detalhes,
            IpOrigem = ipOrigem
        };
    }
}
