using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NfeSaas.Application.DTOs;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Infrastructure.Data;
using NfeSaas.Tests.Integration.Fixtures;

namespace NfeSaas.Tests.Integration.Handlers;

public class MultiTenantIsolationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public MultiTenantIsolationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Client;
    }

    private async Task<(Escritorio escritorio, Empresa empresa, string tokenLogin)> SeedEscritorioComEmpresaELogin(
        string cnpjEscritorio, string email, string cnpjEmpresa)
    {
        await using var db = _fixture.CreateDbContext();

        var escritorio = Escritorio.Criar(
            $"Escritório {cnpjEscritorio}", "Escritório", cnpjEscritorio,
            email, null, PlanoSaas.Basico);
        db.Escritorios.Add(escritorio);
        await db.SaveChangesAsync();

        var empresa = Empresa.Criar(escritorio.Id, $"Empresa {cnpjEmpresa}", "Empresa",
            cnpjEmpresa, "IE", "Rua", "1", "Centro", "São Paulo", "SP",
            "01310100", "3550308", "11900000000", $"emp{cnpjEmpresa}@teste.com",
            RegimeTributario.SimplesNacional, AmbienteSefaz.Homologacao);
        db.Empresas.Add(empresa);

        var admin = Usuario.Criar(escritorio.Id, "Admin", email,
            BCrypt.Net.BCrypt.HashPassword("Senha@123"), "Admin");
        db.Usuarios.Add(admin);
        await db.SaveChangesAsync();

        var loginResp = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginDto(email, "Senha@123"));
        var login = await loginResp.Content.ReadFromJsonAsync<LoginResultDto>();

        return (escritorio, empresa, login!.AccessToken);
    }

    [Fact]
    public async Task SelecionarEmpresa_EmpresaDeOutroEscritorio_NaoGeraToken()
    {
        var (_, _, tokenA) = await SeedEscritorioComEmpresaELogin(
            "77888999000100", "isolamento1a@teste.com", "88999000000111");
        var (_, empresaB, _) = await SeedEscritorioComEmpresaELogin(
            "88999000000122", "isolamento1b@teste.com", "99000111000133");

        // Usuário A tenta selecionar empresa do Escritório B
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);

        var response = await _client.PostAsJsonAsync("/api/auth/selecionar-empresa",
            new SelecionarEmpresaDto(empresaB.Id));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task GetEmpresas_RetornaApenasEmpresasDoProprioEscritorio()
    {
        var (escritorioA, empresaA, tokenA) = await SeedEscritorioComEmpresaELogin(
            "10111222000144", "isolamento2a@teste.com", "11222333000155");
        // Seed escritório B com empresa B (diferente)
        await SeedEscritorioComEmpresaELogin(
            "20222333000166", "isolamento2b@teste.com", "22333444000177");

        // Usuário A lista suas empresas
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);

        var response = await _client.GetAsync("/api/escritorio/empresas");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var empresas = await response.Content.ReadFromJsonAsync<List<EmpresaResumoDto>>();
        empresas.Should().NotBeNull();
        empresas!.Should().AllSatisfy(e =>
            e.Id.Should().NotBe(Guid.Empty));

        // Todas as empresas retornadas pertencem ao Escritório A
        foreach (var emp in empresas)
        {
            await using var db = _fixture.CreateDbContext();
            var dbEmp = await db.Empresas.FindAsync(emp.Id);
            dbEmp!.EscritorioId.Should().Be(escritorioA.Id);
        }

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task GetUsuarios_RetornaApenasUsuariosDoProprioEscritorio()
    {
        var (escritorioA, _, tokenA) = await SeedEscritorioComEmpresaELogin(
            "30333444000188", "isolamento3a@teste.com", "31333444000199");
        await SeedEscritorioComEmpresaELogin(
            "40444555000100", "isolamento3b@teste.com", "41444555000111");

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);

        var response = await _client.GetAsync("/api/escritorio/usuarios");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var usuarios = await response.Content.ReadFromJsonAsync<List<UsuarioResumoDto>>();
        usuarios.Should().NotBeNull();

        foreach (var usr in usuarios!)
        {
            await using var db = _fixture.CreateDbContext();
            var dbUsr = await db.Usuarios.FindAsync(usr.Id);
            dbUsr!.EscritorioId.Should().Be(escritorioA.Id);
        }

        _client.DefaultRequestHeaders.Authorization = null;
    }
}
