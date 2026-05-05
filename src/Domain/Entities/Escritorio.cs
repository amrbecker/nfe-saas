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

    private readonly List<Empresa> _empresas = new();
    public IReadOnlyCollection<Empresa> Empresas => _empresas.AsReadOnly();

    private readonly List<Usuario> _usuarios = new();
    public IReadOnlyCollection<Usuario> Usuarios => _usuarios.AsReadOnly();

    protected Escritorio() { }

    public static Escritorio Criar(string razaoSocial, string nomeFantasia, string cnpj, string email, string? telefone = null, PlanoSaas plano = PlanoSaas.Basico)
    {
        return new Escritorio
        {
            RazaoSocial = razaoSocial,
            NomeFantasia = nomeFantasia,
            Cnpj = cnpj,
            Email = email,
            Telefone = telefone,
            Plano = plano
        };
    }

    public void AtualizarPlano(PlanoSaas plano)
    {
        Plano = plano;
        SetUpdated();
    }
}
