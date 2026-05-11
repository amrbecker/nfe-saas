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
