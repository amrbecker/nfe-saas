using NfeSaas.Domain.Common;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Domain.Entities;

public class ConfiguracaoEmpresa : BaseEntity
{
    public Guid EmpresaId { get; private set; }
    public Empresa Empresa { get; private set; } = null!;

    public PerfilCliente PerfilCliente { get; private set; }
    public TipoProduto TipoProduto { get; private set; }
    public VolumeNotas VolumeNotas { get; private set; }
    public NivelAutomacao NivelAutomacao { get; private set; }
    public bool EmiteParaConsumidorFinal { get; private set; }
    public bool OperaIcmsSt { get; private set; }
    public NivelRelatorio NivelRelatorio { get; private set; }

    public DateTime? ConcluidoEm { get; private set; }

    protected ConfiguracaoEmpresa() { }

    public static ConfiguracaoEmpresa Criar(
        Guid empresaId,
        PerfilCliente perfil,
        TipoProduto tipoProduto,
        VolumeNotas volume,
        NivelAutomacao automacao,
        bool consumidorFinal,
        bool operaSt,
        NivelRelatorio relatorio)
    {
        return new ConfiguracaoEmpresa
        {
            EmpresaId = empresaId,
            PerfilCliente = perfil,
            TipoProduto = tipoProduto,
            VolumeNotas = volume,
            NivelAutomacao = automacao,
            EmiteParaConsumidorFinal = consumidorFinal,
            OperaIcmsSt = operaSt,
            NivelRelatorio = relatorio,
            ConcluidoEm = DateTime.UtcNow
        };
    }

    public void Atualizar(
        PerfilCliente perfil,
        TipoProduto tipoProduto,
        VolumeNotas volume,
        NivelAutomacao automacao,
        bool consumidorFinal,
        bool operaSt,
        NivelRelatorio relatorio)
    {
        PerfilCliente = perfil;
        TipoProduto = tipoProduto;
        VolumeNotas = volume;
        NivelAutomacao = automacao;
        EmiteParaConsumidorFinal = consumidorFinal;
        OperaIcmsSt = operaSt;
        NivelRelatorio = relatorio;
        ConcluidoEm = DateTime.UtcNow;
        SetUpdated();
    }
}
