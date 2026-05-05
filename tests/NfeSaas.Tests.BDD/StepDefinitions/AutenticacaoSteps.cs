using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NfeSaas.Application.DTOs;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Tests.BDD.Support;
using Reqnroll;

namespace NfeSaas.Tests.BDD.StepDefinitions;

[Binding]
public class AutenticacaoSteps
{
    private readonly ScenarioState _state;
    private readonly HttpClient _client;

    public AutenticacaoSteps(ScenarioState state)
    {
        _state = state;
        _client = Hooks.App.CreateClient();
    }

    [Given(@"existe um escritório com CNPJ ""(.*)"" e admin ""(.*)"" com senha ""(.*)""")]
    public async Task GivenEscritorioComAdmin(string cnpj, string email, string senha)
    {
        await using var db = Hooks.App.CreateDbContext();

        if (await db.Escritorios.AnyAsync(e => e.Cnpj == cnpj)) return;

        var escritorio = Escritorio.Criar($"Escritório {cnpj}", "BDD Teste", cnpj,
            email, null, PlanoSaas.Basico);
        db.Escritorios.Add(escritorio);
        await db.SaveChangesAsync();

        _state.EscritorioIds[cnpj] = escritorio.Id;
        _state.CurrentEscritorioId = escritorio.Id;

        var admin = Usuario.Criar(escritorio.Id, "Admin BDD", email,
            BCrypt.Net.BCrypt.HashPassword(senha), "Admin");
        db.Usuarios.Add(admin);
        await db.SaveChangesAsync();
    }

    [Given(@"o escritório possui uma empresa com CNPJ ""(.*)""")]
    public async Task GivenEscritorioTemEmpresa(string cnpjEmpresa)
    {
        await using var db = Hooks.App.CreateDbContext();

        if (await db.Empresas.AnyAsync(e => e.Cnpj == cnpjEmpresa)) return;

        var escritorioId = _state.CurrentEscritorioId
            ?? _state.EscritorioIds.Values.Last();

        var empresa = Empresa.Criar(escritorioId, $"Empresa {cnpjEmpresa}", "BDD Emp",
            cnpjEmpresa, "IE", "Rua BDD", "1", "Centro", "São Paulo", "SP",
            "01310100", "3550308", "11900000000", $"emp@{cnpjEmpresa}.com",
            RegimeTributario.SimplesNacional, AmbienteSefaz.Homologacao);
        db.Empresas.Add(empresa);
        await db.SaveChangesAsync();

        _state.EmpresaIds[cnpjEmpresa] = empresa.Id;
    }

    [Given(@"estou autenticado como ""(.*)"" com senha ""(.*)""")]
    public async Task GivenAutenticado(string email, string senha)
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginDto(email, senha));
        var result = await resp.Content.ReadFromJsonAsync<LoginResultDto>();
        _state.CurrentToken = result?.AccessToken;
        _state.CurrentEscritorioId = result?.EscritorioId;
    }

    [When(@"faço login com email ""(.*)"" e senha ""(.*)""")]
    public async Task WhenLogin(string email, string senha)
    {
        _state.LastResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginDto(email, senha));
    }

    [When(@"seleciono a empresa com CNPJ ""(.*)""")]
    public async Task WhenSelecionarEmpresa(string cnpjEmpresa)
    {
        var empresaId = _state.EmpresaIds[cnpjEmpresa];
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _state.CurrentToken);

        _state.LastResponse = await _client.PostAsJsonAsync("/api/auth/selecionar-empresa",
            new SelecionarEmpresaDto(empresaId));

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Then(@"a resposta deve ter status (\d+)")]
    public void ThenStatus(int statusCode)
    {
        _state.LastResponse!.StatusCode.Should().Be((HttpStatusCode)statusCode);
    }

    [Then(@"recebo um token de acesso válido")]
    public async Task ThenTokenValido()
    {
        var result = await _state.LastResponse!.Content.ReadFromJsonAsync<LoginResultDto>();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Then(@"recebo a lista de empresas do escritório")]
    public async Task ThenListaEmpresas()
    {
        var result = await _state.LastResponse!.Content.ReadFromJsonAsync<LoginResultDto>();
        result!.Empresas.Should().NotBeNull();
    }

    [Then(@"recebo um novo token com empresa selecionada")]
    public async Task ThenNovoTokenComEmpresa()
    {
        var token = await _state.LastResponse!.Content.ReadAsStringAsync();
        token.Should().NotBeNullOrWhiteSpace();
    }
}
