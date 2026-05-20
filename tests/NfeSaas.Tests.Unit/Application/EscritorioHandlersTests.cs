using FluentAssertions;
using Moq;
using NfeSaas.Application.Commands.EscritorioCommands;
using NfeSaas.Application.DTOs;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Tests.Unit.Application;

public class EscritorioHandlersTests
{
    private readonly Mock<IEscritorioRepository> _escritorioRepo = new();
    private readonly Mock<IEmpresaRepository> _empresaRepo = new();
    private readonly Mock<IUsuarioRepository> _usuarioRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    // ==========================================================
    // CreateEscritorioCommand
    // ==========================================================
    private CreateEscritorioCommandHandler CreateEscritorioH() =>
        new(_escritorioRepo.Object, _usuarioRepo.Object, _uow.Object);

    [Fact]
    public async Task CreateEscritorio_CnpjInvalido_RetornaNull()
    {
        var dto = new CreateEscritorioDto("Razão", "Fantasia", "11111111111111",
            "admin@x.com", null, 1, "Admin", "admin@x.com", "senha");
        var result = await CreateEscritorioH().Handle(new CreateEscritorioCommand(dto), CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateEscritorio_CnpjJaExistente_RetornaNull()
    {
        var dto = new CreateEscritorioDto("Razão", "Fantasia", "11222333000181",
            "admin@x.com", null, 1, "Admin", "admin@x.com", "senha");
        _escritorioRepo.Setup(r => r.GetByCnpjAsync("11222333000181", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Escritorio.Criar("Existente", "X", "11222333000181", "x@x.com", null, PlanoSaas.Basico));

        var result = await CreateEscritorioH().Handle(new CreateEscritorioCommand(dto), CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateEscritorio_DadosValidos_CriaEscritorioEAdmin()
    {
        var dto = new CreateEscritorioDto("Razão", "Fantasia", "11222333000181",
            "admin@x.com", "11999999999", 1, "Admin", "admin@x.com", "senha");
        _escritorioRepo.Setup(r => r.GetByCnpjAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Escritorio?)null);

        var result = await CreateEscritorioH().Handle(new CreateEscritorioCommand(dto), CancellationToken.None);

        result.Should().NotBeNull();
        result!.RazaoSocial.Should().Be("Razão");
        _escritorioRepo.Verify(r => r.AddAsync(It.IsAny<Escritorio>(), It.IsAny<CancellationToken>()), Times.Once);
        _usuarioRepo.Verify(r => r.AddAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ==========================================================
    // CadastrarEscritorioComoEmpresaCommand
    // ==========================================================
    private CadastrarEscritorioComoEmpresaCommandHandler CadastrarComoEmpresaH() =>
        new(_escritorioRepo.Object, _empresaRepo.Object, _uow.Object);

    private static CadastrarEscritorioComoEmpresaCommand CmdCadastrarComoEmpresa(
        Guid escritorioId,
        string ie = "111111111111",
        string uf = "SP",
        string cep = "01310100",
        string codigoMunicipio = "3550308",
        int regime = 3,
        int ambiente = 2,
        string? cnae = null) =>
        new(escritorioId, ie,
            "Rua Teste", "100", "Centro", "São Paulo", uf,
            cep, codigoMunicipio, regime, ambiente, cnae);

    private static Escritorio EscritorioParaCadastro(string cnpj = "11222333000181") =>
        Escritorio.Criar("Escritório XPTO LTDA", "XPTO", cnpj,
            "contato@xpto.com", "11999999999", PlanoSaas.Profissional);

    [Fact]
    public async Task CadastrarEscritorioComoEmpresa_EscritorioNaoExiste_RetornaNull()
    {
        _escritorioRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Escritorio?)null);

        var result = await CadastrarComoEmpresaH().Handle(
            CmdCadastrarComoEmpresa(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
        _empresaRepo.Verify(r => r.AddAsync(It.IsAny<Empresa>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CadastrarEscritorioComoEmpresa_UfInvalida_RetornaNull()
    {
        var escritorio = EscritorioParaCadastro();
        _escritorioRepo.Setup(r => r.GetByIdAsync(escritorio.Id, It.IsAny<CancellationToken>())).ReturnsAsync(escritorio);

        var result = await CadastrarComoEmpresaH().Handle(
            CmdCadastrarComoEmpresa(escritorio.Id, uf: "ZZ"), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CadastrarEscritorioComoEmpresa_IeInvalidaParaUf_RetornaNull()
    {
        var escritorio = EscritorioParaCadastro();
        _escritorioRepo.Setup(r => r.GetByIdAsync(escritorio.Id, It.IsAny<CancellationToken>())).ReturnsAsync(escritorio);

        // SP exige IE de 12 dígitos — 8 dígitos é inválido
        var result = await CadastrarComoEmpresaH().Handle(
            CmdCadastrarComoEmpresa(escritorio.Id, ie: "12345678"), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CadastrarEscritorioComoEmpresa_CepComDigitosInsuficientes_RetornaNull()
    {
        var escritorio = EscritorioParaCadastro();
        _escritorioRepo.Setup(r => r.GetByIdAsync(escritorio.Id, It.IsAny<CancellationToken>())).ReturnsAsync(escritorio);

        var result = await CadastrarComoEmpresaH().Handle(
            CmdCadastrarComoEmpresa(escritorio.Id, cep: "123"), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CadastrarEscritorioComoEmpresa_CnaeInvalido_RetornaNull()
    {
        var escritorio = EscritorioParaCadastro();
        _escritorioRepo.Setup(r => r.GetByIdAsync(escritorio.Id, It.IsAny<CancellationToken>())).ReturnsAsync(escritorio);

        var result = await CadastrarComoEmpresaH().Handle(
            CmdCadastrarComoEmpresa(escritorio.Id, cnae: "12345"), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CadastrarEscritorioComoEmpresa_RegimeTributarioInvalido_RetornaNull()
    {
        var escritorio = EscritorioParaCadastro();
        _escritorioRepo.Setup(r => r.GetByIdAsync(escritorio.Id, It.IsAny<CancellationToken>())).ReturnsAsync(escritorio);

        var result = await CadastrarComoEmpresaH().Handle(
            CmdCadastrarComoEmpresa(escritorio.Id, regime: 99), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CadastrarEscritorioComoEmpresa_AmbienteSefazInvalido_RetornaNull()
    {
        var escritorio = EscritorioParaCadastro();
        _escritorioRepo.Setup(r => r.GetByIdAsync(escritorio.Id, It.IsAny<CancellationToken>())).ReturnsAsync(escritorio);

        var result = await CadastrarComoEmpresaH().Handle(
            CmdCadastrarComoEmpresa(escritorio.Id, ambiente: 7), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CadastrarEscritorioComoEmpresa_CnpjJaExisteNoMesmoEscritorio_RetornaExistente_SemRecriar()
    {
        var escritorio = EscritorioParaCadastro();
        var existente = Empresa.Criar(escritorio.Id, "Pré-existente LTDA", "Pré",
            "11222333000181", "111111111111", "Rua X", "1", "Bairro", "Cidade", "SP",
            "01310100", "3550308", "11999999999", "x@x.com",
            RegimeTributario.RegimeNormal, AmbienteSefaz.Homologacao);

        _escritorioRepo.Setup(r => r.GetByIdAsync(escritorio.Id, It.IsAny<CancellationToken>())).ReturnsAsync(escritorio);
        _empresaRepo.Setup(r => r.GetByCnpjAsync("11222333000181", It.IsAny<CancellationToken>())).ReturnsAsync(existente);

        var result = await CadastrarComoEmpresaH().Handle(
            CmdCadastrarComoEmpresa(escritorio.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(existente.Id);
        result.RazaoSocial.Should().Be("Pré-existente LTDA");
        _empresaRepo.Verify(r => r.AddAsync(It.IsAny<Empresa>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CadastrarEscritorioComoEmpresa_CnpjExisteEmOutroEscritorio_RetornaNull()
    {
        var escritorio = EscritorioParaCadastro();
        var empresaAlheia = Empresa.Criar(Guid.NewGuid(), "Outra LTDA", "Outra",
            "11222333000181", "111111111111", "Rua X", "1", "Bairro", "Cidade", "SP",
            "01310100", "3550308", "11999999999", "x@x.com",
            RegimeTributario.RegimeNormal, AmbienteSefaz.Homologacao);

        _escritorioRepo.Setup(r => r.GetByIdAsync(escritorio.Id, It.IsAny<CancellationToken>())).ReturnsAsync(escritorio);
        _empresaRepo.Setup(r => r.GetByCnpjAsync("11222333000181", It.IsAny<CancellationToken>())).ReturnsAsync(empresaAlheia);

        var result = await CadastrarComoEmpresaH().Handle(
            CmdCadastrarComoEmpresa(escritorio.Id), CancellationToken.None);

        result.Should().BeNull();
        _empresaRepo.Verify(r => r.AddAsync(It.IsAny<Empresa>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CadastrarEscritorioComoEmpresa_DadosValidos_CriaEmpresaCopiandoDadosDoEscritorio()
    {
        var escritorio = EscritorioParaCadastro("11222333000181");
        _escritorioRepo.Setup(r => r.GetByIdAsync(escritorio.Id, It.IsAny<CancellationToken>())).ReturnsAsync(escritorio);
        _empresaRepo.Setup(r => r.GetByCnpjAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Empresa?)null);

        Empresa? capturada = null;
        _empresaRepo.Setup(r => r.AddAsync(It.IsAny<Empresa>(), It.IsAny<CancellationToken>()))
            .Callback<Empresa, CancellationToken>((e, _) => capturada = e)
            .Returns(Task.CompletedTask);

        var result = await CadastrarComoEmpresaH().Handle(
            CmdCadastrarComoEmpresa(escritorio.Id, cnae: "6920601"), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Cnpj.Should().Be("11222333000181");
        result.RazaoSocial.Should().Be(escritorio.RazaoSocial);
        result.NomeFantasia.Should().Be(escritorio.NomeFantasia);

        capturada.Should().NotBeNull();
        capturada!.EscritorioId.Should().Be(escritorio.Id);
        capturada.Email.Should().Be(escritorio.Email);
        capturada.Telefone.Should().Be(escritorio.Telefone);
        capturada.Cnae.Should().Be("6920601");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ==========================================================
    // AtivarPlanoPagoCommand
    // ==========================================================
    private AtivarPlanoPagoCommandHandler AtivarPlanoH() =>
        new(_escritorioRepo.Object, _uow.Object);

    [Fact]
    public async Task AtivarPlanoPago_EscritorioNaoExiste_RetornaFalse()
    {
        _escritorioRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Escritorio?)null);

        var ok = await AtivarPlanoH().Handle(
            new AtivarPlanoPagoCommand(Guid.NewGuid(), DateTime.UtcNow.AddDays(30), 99m),
            CancellationToken.None);

        ok.Should().BeFalse();
    }

    [Fact]
    public async Task AtivarPlanoPago_DataAteNoPassado_RetornaFalse()
    {
        var escritorio = Escritorio.Criar("E", "E", "11222333000181", "e@e.com", null, PlanoSaas.Basico);
        _escritorioRepo.Setup(r => r.GetByIdAsync(escritorio.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(escritorio);

        var ok = await AtivarPlanoH().Handle(
            new AtivarPlanoPagoCommand(escritorio.Id, DateTime.UtcNow.AddDays(-1), 99m),
            CancellationToken.None);

        ok.Should().BeFalse();
        escritorio.PlanoAtivoAteEm.Should().BeNull();
    }

    [Fact]
    public async Task AtivarPlanoPago_DadosValidos_AtivaPlanoEPersiste()
    {
        var escritorio = Escritorio.Criar("E", "E", "11222333000181", "e@e.com", null, PlanoSaas.Profissional);
        _escritorioRepo.Setup(r => r.GetByIdAsync(escritorio.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(escritorio);
        var ativoAte = DateTime.UtcNow.AddDays(365);

        var ok = await AtivarPlanoH().Handle(
            new AtivarPlanoPagoCommand(escritorio.Id, ativoAte, 299m),
            CancellationToken.None);

        ok.Should().BeTrue();
        escritorio.PlanoAtivoAteEm.Should().Be(ativoAte);
        escritorio.CalcularStatusAssinatura().Should().Be(StatusAssinaturaEscritorio.Pago);
        _escritorioRepo.Verify(r => r.UpdateAsync(escritorio, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ==========================================================
    // CreateEmpresaCommand
    // ==========================================================
    private CreateEmpresaCommandHandler CreateEmpresaH() =>
        new(_empresaRepo.Object, _escritorioRepo.Object, _uow.Object);

    private static CreateEmpresaDto EmpresaDto(string cnpj = "11222333000181", string ie = "111111111111",
        string uf = "SP", string? cnae = null) =>
        new("Razão LTDA", "Fantasia", cnpj, ie,
            "Rua A", "1", "Bairro", "Cidade", uf, "01310100", "3550308",
            "11999999999", "x@x.com", 3, 2, cnae);

    [Fact]
    public async Task CreateEmpresa_EscritorioNaoExiste_RetornaNull()
    {
        _escritorioRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Escritorio?)null);

        var result = await CreateEmpresaH().Handle(
            new CreateEmpresaCommand(Guid.NewGuid(), EmpresaDto()), CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateEmpresa_CnpjInvalido_RetornaNull()
    {
        var escritorio = Escritorio.Criar("E", "E", "11222333000181", "e@e.com", null, PlanoSaas.Basico);
        _escritorioRepo.Setup(r => r.GetByIdAsync(escritorio.Id, It.IsAny<CancellationToken>())).ReturnsAsync(escritorio);

        var result = await CreateEmpresaH().Handle(
            new CreateEmpresaCommand(escritorio.Id, EmpresaDto(cnpj: "11111111111111")),
            CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateEmpresa_UfInvalida_RetornaNull()
    {
        var escritorio = Escritorio.Criar("E", "E", "11222333000181", "e@e.com", null, PlanoSaas.Basico);
        _escritorioRepo.Setup(r => r.GetByIdAsync(escritorio.Id, It.IsAny<CancellationToken>())).ReturnsAsync(escritorio);

        var result = await CreateEmpresaH().Handle(
            new CreateEmpresaCommand(escritorio.Id, EmpresaDto(uf: "ZZ")),
            CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateEmpresa_IeInvalidaParaUf_RetornaNull()
    {
        var escritorio = Escritorio.Criar("E", "E", "11222333000181", "e@e.com", null, PlanoSaas.Basico);
        _escritorioRepo.Setup(r => r.GetByIdAsync(escritorio.Id, It.IsAny<CancellationToken>())).ReturnsAsync(escritorio);

        // SP exige IE de 12 dígitos. Passar 8 = inválido
        var result = await CreateEmpresaH().Handle(
            new CreateEmpresaCommand(escritorio.Id, EmpresaDto(ie: "12345678")),
            CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateEmpresa_CnaeInvalido_RetornaNull()
    {
        var escritorio = Escritorio.Criar("E", "E", "11222333000181", "e@e.com", null, PlanoSaas.Basico);
        _escritorioRepo.Setup(r => r.GetByIdAsync(escritorio.Id, It.IsAny<CancellationToken>())).ReturnsAsync(escritorio);

        var result = await CreateEmpresaH().Handle(
            new CreateEmpresaCommand(escritorio.Id, EmpresaDto(cnae: "12345")),
            CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateEmpresa_DadosValidos_CriaERetornaResumo()
    {
        var escritorio = Escritorio.Criar("E", "E", "11222333000181", "e@e.com", null, PlanoSaas.Basico);
        _escritorioRepo.Setup(r => r.GetByIdAsync(escritorio.Id, It.IsAny<CancellationToken>())).ReturnsAsync(escritorio);

        var result = await CreateEmpresaH().Handle(
            new CreateEmpresaCommand(escritorio.Id, EmpresaDto()),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.RazaoSocial.Should().Be("Razão LTDA");
        _empresaRepo.Verify(r => r.AddAsync(It.IsAny<Empresa>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ==========================================================
    // CreateUsuarioCommand
    // ==========================================================
    private CreateUsuarioCommandHandler CreateUsuarioH() =>
        new(_usuarioRepo.Object, _escritorioRepo.Object, _uow.Object);

    [Fact]
    public async Task CreateUsuario_EscritorioNaoExiste_RetornaNull()
    {
        _escritorioRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Escritorio?)null);

        var result = await CreateUsuarioH().Handle(
            new CreateUsuarioCommand(Guid.NewGuid(), new CreateUsuarioDto("Nome", "x@x.com", "senha", "User")),
            CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateUsuario_EmailJaExistente_RetornaNull()
    {
        var escritorio = Escritorio.Criar("E", "E", "11222333000181", "e@e.com", null, PlanoSaas.Basico);
        _escritorioRepo.Setup(r => r.GetByIdAsync(escritorio.Id, It.IsAny<CancellationToken>())).ReturnsAsync(escritorio);
        _usuarioRepo.Setup(r => r.GetByEmailAsync("dup@x.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Usuario.Criar(escritorio.Id, "Existente", "dup@x.com", "hash"));

        var result = await CreateUsuarioH().Handle(
            new CreateUsuarioCommand(escritorio.Id, new CreateUsuarioDto("Novo", "dup@x.com", "senha", "User")),
            CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateUsuario_DadosValidos_CriaEHashSenha()
    {
        var escritorio = Escritorio.Criar("E", "E", "11222333000181", "e@e.com", null, PlanoSaas.Basico);
        _escritorioRepo.Setup(r => r.GetByIdAsync(escritorio.Id, It.IsAny<CancellationToken>())).ReturnsAsync(escritorio);
        _usuarioRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        var result = await CreateUsuarioH().Handle(
            new CreateUsuarioCommand(escritorio.Id, new CreateUsuarioDto("Novo", "novo@x.com", "senha123", "User")),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Nome.Should().Be("Novo");
        _usuarioRepo.Verify(r => r.AddAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ==========================================================
    // UpdateUsuarioCommand
    // ==========================================================
    private UpdateUsuarioCommandHandler UpdateUsuarioH() =>
        new(_usuarioRepo.Object, _uow.Object);

    [Fact]
    public async Task UpdateUsuario_NaoExiste_RetornaNull()
    {
        _usuarioRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Usuario?)null);

        var result = await UpdateUsuarioH().Handle(
            new UpdateUsuarioCommand(Guid.NewGuid(), Guid.NewGuid(),
                new UpdateUsuarioDto("Novo", "User")),
            CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateUsuario_DeOutroEscritorio_RetornaNull()
    {
        var escritorioId = Guid.NewGuid();
        var u = Usuario.Criar(escritorioId, "Original", "x@x.com", "hash");
        _usuarioRepo.Setup(r => r.GetByIdAsync(u.Id, It.IsAny<CancellationToken>())).ReturnsAsync(u);

        var result = await UpdateUsuarioH().Handle(
            new UpdateUsuarioCommand(Guid.NewGuid() /* outro */, u.Id, new UpdateUsuarioDto("X", "User")),
            CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateUsuario_DadosValidos_AtualizaUsuario()
    {
        var escritorioId = Guid.NewGuid();
        var u = Usuario.Criar(escritorioId, "Original", "x@x.com", "hash");
        _usuarioRepo.Setup(r => r.GetByIdAsync(u.Id, It.IsAny<CancellationToken>())).ReturnsAsync(u);

        var result = await UpdateUsuarioH().Handle(
            new UpdateUsuarioCommand(escritorioId, u.Id, new UpdateUsuarioDto("Novo Nome", "Admin")),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Nome.Should().Be("Novo Nome");
        result.Role.Should().Be("Admin");
    }

    // ==========================================================
    // ToggleAtivoUsuarioCommand
    // ==========================================================
    private ToggleAtivoUsuarioCommandHandler ToggleH() => new(_usuarioRepo.Object, _uow.Object);

    [Fact]
    public async Task ToggleAtivoUsuario_AtivoParaInativo_DesativaUsuario()
    {
        var escritorioId = Guid.NewGuid();
        var u = Usuario.Criar(escritorioId, "X", "x@x.com", "hash");  // Ativo por padrão
        _usuarioRepo.Setup(r => r.GetByIdAsync(u.Id, It.IsAny<CancellationToken>())).ReturnsAsync(u);

        var result = await ToggleH().Handle(
            new ToggleAtivoUsuarioCommand(escritorioId, u.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Ativo.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleAtivoUsuario_InativoParaAtivo_AtivaUsuario()
    {
        var escritorioId = Guid.NewGuid();
        var u = Usuario.Criar(escritorioId, "X", "x@x.com", "hash");
        u.Desativar();
        _usuarioRepo.Setup(r => r.GetByIdAsync(u.Id, It.IsAny<CancellationToken>())).ReturnsAsync(u);

        var result = await ToggleH().Handle(
            new ToggleAtivoUsuarioCommand(escritorioId, u.Id), CancellationToken.None);

        result!.Ativo.Should().BeTrue();
    }

    // ==========================================================
    // DeleteUsuarioCommand
    // ==========================================================
    private DeleteUsuarioCommandHandler DeleteH() => new(_usuarioRepo.Object, _uow.Object);

    [Fact]
    public async Task DeleteUsuario_NaoExiste_RetornaFalse()
    {
        _usuarioRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Usuario?)null);

        var ok = await DeleteH().Handle(
            new DeleteUsuarioCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);
        ok.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteUsuario_Existente_MarcaIsDeleted()
    {
        var escritorioId = Guid.NewGuid();
        var u = Usuario.Criar(escritorioId, "X", "x@x.com", "hash");
        _usuarioRepo.Setup(r => r.GetByIdAsync(u.Id, It.IsAny<CancellationToken>())).ReturnsAsync(u);

        var ok = await DeleteH().Handle(
            new DeleteUsuarioCommand(escritorioId, u.Id), CancellationToken.None);
        ok.Should().BeTrue();
        u.IsDeleted.Should().BeTrue();
    }
}
