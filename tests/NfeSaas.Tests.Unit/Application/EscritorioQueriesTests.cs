using FluentAssertions;
using Moq;
using NfeSaas.Application.Queries;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Tests.Unit.Application;

public class GetEmpresasQueryHandlerTests
{
    private readonly Mock<IEmpresaRepository> _repo = new();
    private readonly Mock<IConfiguracaoEmpresaRepository> _configRepo = new();

    private GetEmpresasQueryHandler Handler() => new(_repo.Object, _configRepo.Object);

    private static Empresa CriarEmpresa(Guid escritorioId, string cnpj) =>
        Empresa.Criar(escritorioId, "Razao Social Ltda", "Fantasia", cnpj, "111111111111",
            "Rua A", "1", "Centro", "Cidade", "SP", "01310100", "3550308",
            "11999999999", "empresa@x.com",
            RegimeTributario.RegimeNormal, AmbienteSefaz.Homologacao);

    [Fact]
    public async Task Handle_EmpresaComIdNoHashSet_RetornaConfiguracaoConcluidaTrue()
    {
        var escritorioId = Guid.NewGuid();
        var empresa = CriarEmpresa(escritorioId, "11111111000191");

        _repo.Setup(r => r.GetByEscritorioAsync(escritorioId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new[] { empresa });
        _configRepo.Setup(c => c.GetEmpresaIdsConfiguradosAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new HashSet<Guid> { empresa.Id });

        var result = await Handler().Handle(new GetEmpresasQuery(escritorioId), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Id.Should().Be(empresa.Id);
        result[0].ConfiguracaoConcluida.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_EmpresaComIdForaDoHashSet_RetornaConfiguracaoConcluidaFalse()
    {
        var escritorioId = Guid.NewGuid();
        var empresa = CriarEmpresa(escritorioId, "22222222000199");

        _repo.Setup(r => r.GetByEscritorioAsync(escritorioId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new[] { empresa });
        _configRepo.Setup(c => c.GetEmpresaIdsConfiguradosAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new HashSet<Guid>());

        var result = await Handler().Handle(new GetEmpresasQuery(escritorioId), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Id.Should().Be(empresa.Id);
        result[0].ConfiguracaoConcluida.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_DeveChamarGetEmpresaIdsConfiguradosAsyncComTodosOsIdsDasEmpresas()
    {
        var escritorioId = Guid.NewGuid();
        var empresa1 = CriarEmpresa(escritorioId, "11111111000191");
        var empresa2 = CriarEmpresa(escritorioId, "22222222000199");

        _repo.Setup(r => r.GetByEscritorioAsync(escritorioId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new[] { empresa1, empresa2 });

        IEnumerable<Guid>? idsRecebidos = null;
        _configRepo.Setup(c => c.GetEmpresaIdsConfiguradosAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                   .Callback<IEnumerable<Guid>, CancellationToken>((ids, _) => idsRecebidos = ids)
                   .ReturnsAsync(new HashSet<Guid> { empresa1.Id });

        var result = await Handler().Handle(new GetEmpresasQuery(escritorioId), CancellationToken.None);

        idsRecebidos.Should().NotBeNull();
        idsRecebidos!.Should().BeEquivalentTo(new[] { empresa1.Id, empresa2.Id });

        result.Should().HaveCount(2);
        result.Single(e => e.Id == empresa1.Id).ConfiguracaoConcluida.Should().BeTrue();
        result.Single(e => e.Id == empresa2.Id).ConfiguracaoConcluida.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_MapeiaRazaoSocialNomeFantasiaECnpjCorretamente()
    {
        var escritorioId = Guid.NewGuid();
        var empresa = CriarEmpresa(escritorioId, "33333333000177");

        _repo.Setup(r => r.GetByEscritorioAsync(escritorioId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new[] { empresa });
        _configRepo.Setup(c => c.GetEmpresaIdsConfiguradosAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new HashSet<Guid>());

        var result = await Handler().Handle(new GetEmpresasQuery(escritorioId), CancellationToken.None);

        result[0].RazaoSocial.Should().Be(empresa.RazaoSocial);
        result[0].NomeFantasia.Should().Be(empresa.NomeFantasia);
        result[0].Cnpj.Should().Be(empresa.Cnpj);
    }
}
