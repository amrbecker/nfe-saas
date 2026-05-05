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
public class MultiTenantSteps
{
    private readonly ScenarioState _state;
    private readonly HttpClient _client;

    public MultiTenantSteps(ScenarioState state)
    {
        _state = state;
        _client = Hooks.App.CreateClient();
    }

    [Given(@"existe um escritório ""(.*)"" com CNPJ ""(.*)"" e admin ""(.*)"" com senha ""(.*)"" e empresa com CNPJ ""(.*)""")]
    public async Task GivenEscritorioComEmpresa(
        string nomeEscritorio, string cnpjEscritorio,
        string email, string senha, string cnpjEmpresa)
    {
        await using var db = Hooks.App.CreateDbContext();

        if (await db.Escritorios.AnyAsync(e => e.Cnpj == cnpjEscritorio))
        {
            var existing = await db.Escritorios.FirstAsync(e => e.Cnpj == cnpjEscritorio);
            _state.EscritorioIds[nomeEscritorio] = existing.Id;
            var existingEmp = await db.Empresas.FirstOrDefaultAsync(e => e.Cnpj == cnpjEmpresa);
            if (existingEmp != null) _state.EmpresaIds[nomeEscritorio] = existingEmp.Id;
            return;
        }

        var escritorio = Escritorio.Criar(nomeEscritorio, nomeEscritorio, cnpjEscritorio,
            email, null, PlanoSaas.Basico);
        db.Escritorios.Add(escritorio);
        await db.SaveChangesAsync();

        _state.EscritorioIds[nomeEscritorio] = escritorio.Id;

        var empresa = Empresa.Criar(escritorio.Id, $"Empresa de {nomeEscritorio}", nomeEscritorio,
            cnpjEmpresa, "IE", "Rua", "1", "Centro", "São Paulo", "SP",
            "01310100", "3550308", "11900000000", $"emp@{nomeEscritorio.ToLower()}.com",
            RegimeTributario.SimplesNacional, AmbienteSefaz.Homologacao);
        db.Empresas.Add(empresa);

        var admin = Usuario.Criar(escritorio.Id, $"Admin {nomeEscritorio}", email,
            BCrypt.Net.BCrypt.HashPassword(senha), "Admin");
        db.Usuarios.Add(admin);

        await db.SaveChangesAsync();
        _state.EmpresaIds[nomeEscritorio] = empresa.Id;
    }

    [When(@"seleciono a empresa do escritório ""(.*)""")]
    public async Task WhenSelecionarEmpresaDeOutroEscritorio(string nomeEscritorio)
    {
        var empresaId = _state.EmpresaIds[nomeEscritorio];

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _state.CurrentToken);

        _state.LastResponse = await _client.PostAsJsonAsync("/api/auth/selecionar-empresa",
            new SelecionarEmpresaDto(empresaId));

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [When(@"listo as empresas do escritório")]
    public async Task WhenListarEmpresas()
    {
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _state.CurrentToken);

        _state.LastResponse = await _client.GetAsync("/api/escritorio/empresas");

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [When(@"listo os usuários do escritório")]
    public async Task WhenListarUsuarios()
    {
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _state.CurrentToken);

        _state.LastResponse = await _client.GetAsync("/api/escritorio/usuarios");

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Then(@"recebo apenas empresas do próprio escritório")]
    public async Task ThenApenasEmpresasProprias()
    {
        _state.LastResponse!.StatusCode.Should().Be(HttpStatusCode.OK);

        var empresas = await _state.LastResponse.Content
            .ReadFromJsonAsync<List<EmpresaResumoDto>>();

        empresas.Should().NotBeNull();

        foreach (var emp in empresas!)
        {
            await using var db = Hooks.App.CreateDbContext();
            var dbEmp = await db.Empresas.FindAsync(emp.Id);
            dbEmp!.EscritorioId.Should().Be(_state.CurrentEscritorioId!.Value);
        }
    }

    [Then(@"recebo apenas usuários do próprio escritório")]
    public async Task ThenApenasUsuariosProprios()
    {
        _state.LastResponse!.StatusCode.Should().Be(HttpStatusCode.OK);

        var usuarios = await _state.LastResponse.Content
            .ReadFromJsonAsync<List<UsuarioResumoDto>>();

        usuarios.Should().NotBeNull();

        foreach (var usr in usuarios!)
        {
            await using var db = Hooks.App.CreateDbContext();
            var dbUsr = await db.Usuarios.FindAsync(usr.Id);
            dbUsr!.EscritorioId.Should().Be(_state.CurrentEscritorioId!.Value);
        }
    }
}
