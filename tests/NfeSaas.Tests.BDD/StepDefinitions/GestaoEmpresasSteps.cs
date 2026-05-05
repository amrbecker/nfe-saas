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
public class GestaoEmpresasSteps
{
    private readonly ScenarioState _state;
    private readonly HttpClient _client;
    private string? _cnpjEmpresaCriada;
    private string? _emailUsuarioCriado;

    public GestaoEmpresasSteps(ScenarioState state)
    {
        _state = state;
        _client = Hooks.App.CreateClient();
    }

    [Given(@"existe um usuário ""(.*)"" com senha ""(.*)"" e role ""(.*)"" no mesmo escritório com CNPJ ""(.*)""")]
    public async Task GivenUsuarioNoEscritorio(string email, string senha, string role, string cnpjEscritorio)
    {
        await using var db = Hooks.App.CreateDbContext();

        if (await db.Usuarios.AnyAsync(u => u.Email == email)) return;

        var escritorio = await db.Escritorios.FirstAsync(e => e.Cnpj == cnpjEscritorio);

        var usuario = Usuario.Criar(escritorio.Id, "Usuário BDD", email,
            BCrypt.Net.BCrypt.HashPassword(senha), role);
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
    }

    [When(@"crio uma empresa com os dados:")]
    public async Task WhenCriarEmpresaComDados(Table table)
    {
        var dados = table.Rows.ToDictionary(r => r[0], r => r[1]);

        var dto = new CreateEmpresaDto(
            RazaoSocial: dados["RazaoSocial"],
            NomeFantasia: dados["NomeFantasia"],
            Cnpj: dados["Cnpj"],
            InscricaoEstadual: dados.GetValueOrDefault("InscricaoEstadual", "IE001"),
            Logradouro: "Rua BDD",
            Numero: "100",
            Bairro: "Centro",
            Cidade: "São Paulo",
            Uf: "SP",
            Cep: "01310100",
            CodigoMunicipio: "3550308",
            Telefone: "11900000000",
            Email: "empresa@bdd.com",
            RegimeTributario: (int)RegimeTributario.SimplesNacional,
            AmbienteSefaz: (int)AmbienteSefaz.Homologacao
        );

        _cnpjEmpresaCriada = dados["Cnpj"];

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _state.CurrentToken);

        _state.LastResponse = await _client.PostAsJsonAsync("/api/escritorio/empresas", dto);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [When(@"tento criar uma empresa com CNPJ ""(.*)""")]
    public async Task WhenTentarCriarEmpresa(string cnpj)
    {
        var dto = new CreateEmpresaDto(
            RazaoSocial: "Empresa Proibida",
            NomeFantasia: "Proibida",
            Cnpj: cnpj,
            InscricaoEstadual: "IE000",
            Logradouro: "Rua", Numero: "1", Bairro: "Centro",
            Cidade: "SP", Uf: "SP", Cep: "01000000", CodigoMunicipio: "3550308",
            Telefone: "11900000000", Email: "proibida@bdd.com",
            RegimeTributario: (int)RegimeTributario.SimplesNacional,
            AmbienteSefaz: (int)AmbienteSefaz.Homologacao
        );

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _state.CurrentToken);

        _state.LastResponse = await _client.PostAsJsonAsync("/api/escritorio/empresas", dto);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [When(@"crio um usuário com nome ""(.*)"" e email ""(.*)"" e senha ""(.*)""")]
    public async Task WhenCriarUsuario(string nome, string email, string senha)
    {
        _emailUsuarioCriado = email;

        var dto = new CreateUsuarioDto(nome, email, senha, "User");

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _state.CurrentToken);

        _state.LastResponse = await _client.PostAsJsonAsync("/api/escritorio/usuarios", dto);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Then(@"a empresa aparece na listagem do escritório")]
    public async Task ThenEmpresaNaListagem()
    {
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _state.CurrentToken);

        var response = await _client.GetAsync("/api/escritorio/empresas");
        var empresas = await response.Content.ReadFromJsonAsync<List<EmpresaResumoDto>>();

        _client.DefaultRequestHeaders.Authorization = null;

        empresas.Should().NotBeNull();
        empresas!.Should().Contain(e => e.Cnpj == _cnpjEmpresaCriada);
    }

    [Then(@"o usuário ""(.*)"" aparece na listagem do escritório")]
    public async Task ThenUsuarioNaListagem(string email)
    {
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _state.CurrentToken);

        var response = await _client.GetAsync("/api/escritorio/usuarios");
        var usuarios = await response.Content.ReadFromJsonAsync<List<UsuarioResumoDto>>();

        _client.DefaultRequestHeaders.Authorization = null;

        usuarios.Should().NotBeNull();
        usuarios!.Should().Contain(u => u.Email == email);
    }
}
