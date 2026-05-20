using NfeSaas.Domain.Common;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Domain.Entities;

public class Escritorio : BaseEntity
{
    public string RazaoSocial { get; private set; } = null!;
    public string NomeFantasia { get; private set; } = null!;
    public string Cnpj { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string? Telefone { get; private set; }
    public PlanoSaas Plano { get; private set; } = PlanoSaas.Basico;
    public bool Ativo { get; private set; } = true;

    // Trial e cobrança — todo escritório novo entra em trial de 30 dias do plano escolhido;
    // depois disso, o login é bloqueado até PlanoAtivoAteEm > UtcNow.
    public DateTime TrialInicioEm { get; private set; }
    public DateTime TrialFimEm { get; private set; }
    public DateTime? PlanoAtivoAteEm { get; private set; }
    public DateTime? UltimoPagamentoEm { get; private set; }

    private readonly List<Empresa> _empresas = new();
    public IReadOnlyCollection<Empresa> Empresas => _empresas.AsReadOnly();

    private readonly List<Usuario> _usuarios = new();
    public IReadOnlyCollection<Usuario> Usuarios => _usuarios.AsReadOnly();

    protected Escritorio() { }

    public const int DiasTrialPadrao = 30;

    public static Escritorio Criar(string razaoSocial, string nomeFantasia, string cnpj, string email, string? telefone, PlanoSaas plano)
    {
        var agora = DateTime.UtcNow;
        return new Escritorio
        {
            RazaoSocial = razaoSocial,
            NomeFantasia = nomeFantasia,
            Cnpj = cnpj,
            Email = email,
            Telefone = telefone,
            Plano = plano,
            TrialInicioEm = agora,
            TrialFimEm = agora.AddDays(DiasTrialPadrao)
        };
    }

    public void AtualizarPlano(PlanoSaas plano)
    {
        Plano = plano;
        SetUpdated();
    }

    // Confirma pagamento e estende o acesso até a data informada (normalmente +30 ou +365 dias).
    public void AtivarPlanoPago(DateTime ativoAte, DateTime? momentoPagamento = null)
    {
        if (ativoAte <= DateTime.UtcNow)
            throw new InvalidOperationException("Data de validade do plano deve estar no futuro.");
        PlanoAtivoAteEm = ativoAte;
        UltimoPagamentoEm = momentoPagamento ?? DateTime.UtcNow;
        SetUpdated();
    }

    public void Suspender()
    {
        Ativo = false;
        SetUpdated();
    }

    public void Reativar()
    {
        Ativo = true;
        SetUpdated();
    }

    public StatusAssinaturaEscritorio CalcularStatusAssinatura(DateTime? referenciaUtc = null)
    {
        var agora = referenciaUtc ?? DateTime.UtcNow;
        if (!Ativo) return StatusAssinaturaEscritorio.Suspenso;
        if (PlanoAtivoAteEm.HasValue && PlanoAtivoAteEm.Value > agora)
            return StatusAssinaturaEscritorio.Pago;
        if (TrialFimEm > agora) return StatusAssinaturaEscritorio.TrialAtivo;
        return StatusAssinaturaEscritorio.TrialExpirado;
    }

    public bool PodeAcessar(DateTime? referenciaUtc = null)
    {
        var status = CalcularStatusAssinatura(referenciaUtc);
        return status == StatusAssinaturaEscritorio.TrialAtivo
            || status == StatusAssinaturaEscritorio.Pago;
    }

    public int DiasRestantesTrial(DateTime? referenciaUtc = null)
    {
        var agora = referenciaUtc ?? DateTime.UtcNow;
        if (TrialFimEm <= agora) return 0;
        return (int)Math.Ceiling((TrialFimEm - agora).TotalDays);
    }
}
