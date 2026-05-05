using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NfeSaas.Application.DTOs;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Infrastructure.Data;
using NfeSaas.Tests.Integration.Fixtures;

namespace NfeSaas.Tests.Integration.Handlers;

public class LoginHandlerTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public LoginHandlerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Client;
    }

    private async Task<(Escritorio escritorio, Usuario admin)> SeedEscritorioComAdmin(
        string cnpj, string email, string senha)
    {
        await using var db = _fixture.CreateDbContext();

        var escritorio = Escritorio.Criar(
            $"Escritório {cnpj}", "Escritório Teste", cnpj,
            email, null, PlanoSaas.Basico);
        db.Escritorios.Add(escritorio);
        await db.SaveChangesAsync();

        var admin = Usuario.Criar(escritorio.Id, "Admin Teste", email,
            BCrypt.Net.BCrypt.HashPassword(senha), "Admin");
        db.Usuarios.Add(admin);
        await db.SaveChangesAsync();

        return (escritorio, admin);
    }

    [Fact]
    public async Task Login_ComCredenciaisCorretas_RetornaTokenEEmpresas()
    {
        await SeedEscritorioComAdmin("11222333000144", "admin1@teste.com", "Senha@123");

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginDto("admin1@teste.com", "Senha@123"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LoginResultDto>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.Email.Should().Be("admin1@teste.com");
        result.Role.Should().Be("Admin");
        result.Empresas.Should().NotBeNull();
    }

    [Fact]
    public async Task Login_ComSenhaErrada_Retorna401()
    {
        await SeedEscritorioComAdmin("22333444000155", "admin2@teste.com", "Senha@123");

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginDto("admin2@teste.com", "SenhaErrada"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ComEmailInexistente_Retorna401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginDto("nao.existe@teste.com", "Senha@123"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_RetornaListaDeEmpresas_DoEscritorioDoUsuario()
    {
        await using var db = _fixture.CreateDbContext();

        var escritorio = Escritorio.Criar("Escritório Com Empresa", "Empresa Teste",
            "33444555000166", "admin3@teste.com", null, PlanoSaas.Profissional);
        db.Escritorios.Add(escritorio);
        await db.SaveChangesAsync();

        var empresa = Empresa.Criar(escritorio.Id, "Empresa Ltda", "Empresa",
            "44555666000177", "IE001", "Rua A", "1", "Centro",
            "São Paulo", "SP", "01310100", "3550308",
            "11999999999", "empresa@empresa.com",
            RegimeTributario.SimplesNacional, AmbienteSefaz.Homologacao);
        db.Empresas.Add(empresa);

        var admin = Usuario.Criar(escritorio.Id, "Admin", "admin3@teste.com",
            BCrypt.Net.BCrypt.HashPassword("Senha@123"), "Admin");
        db.Usuarios.Add(admin);
        await db.SaveChangesAsync();

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginDto("admin3@teste.com", "Senha@123"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LoginResultDto>();
        result!.Empresas.Should().HaveCount(1);
        result.Empresas[0].Cnpj.Should().Be("44555666000177");
    }

    [Fact]
    public async Task SelecionarEmpresa_EmpresaDoProprioEscritorio_RetornaNovoToken()
    {
        await using var db = _fixture.CreateDbContext();

        var escritorio = Escritorio.Criar("Escritório Seleção", "Seleção",
            "55666777000188", "admin4@teste.com", null, PlanoSaas.Basico);
        db.Escritorios.Add(escritorio);
        await db.SaveChangesAsync();

        var empresa = Empresa.Criar(escritorio.Id, "Empresa Seleção Ltda", "Seleção",
            "66777888000199", "IE002", "Rua B", "2", "Centro",
            "São Paulo", "SP", "01310100", "3550308",
            "11888888888", "selecao@empresa.com",
            RegimeTributario.SimplesNacional, AmbienteSefaz.Homologacao);
        db.Empresas.Add(empresa);

        var admin = Usuario.Criar(escritorio.Id, "Admin", "admin4@teste.com",
            BCrypt.Net.BCrypt.HashPassword("Senha@123"), "Admin");
        db.Usuarios.Add(admin);
        await db.SaveChangesAsync();

        // Login
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginDto("admin4@teste.com", "Senha@123"));
        var login = await loginResp.Content.ReadFromJsonAsync<LoginResultDto>();

        // Selecionar empresa
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", login!.AccessToken);

        var selecionarResp = await _client.PostAsJsonAsync("/api/auth/selecionar-empresa",
            new SelecionarEmpresaDto(empresa.Id));

        selecionarResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var novoToken = await selecionarResp.Content.ReadAsStringAsync();
        novoToken.Should().NotBeNullOrEmpty();

        _client.DefaultRequestHeaders.Authorization = null;
    }
}
