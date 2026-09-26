using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NfeSaas.API.Workers.Assistente;
using NfeSaas.Application.DTOs;
using NfeSaas.Application.DTOs.Assistente;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Tests.Integration.Fixtures;

namespace NfeSaas.Tests.Integration.Assistente;

/// <summary>
/// Ori ponta a ponta com Postgres real: migration das tabelas do assistente, autenticação, papéis e isolamento
/// multi-tenant dos endpoints. Sem chave de IA configurada — a Ori responde só com a base (comportamento de produção
/// quando o endpoint do modelo não está disponível).
/// </summary>
public class OriEndpointsTests : IClassFixture<DatabaseFixture>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly DatabaseFixture _fixture;

    public OriEndpointsTests(DatabaseFixture fixture) => _fixture = fixture;

    private sealed record Conta(Escritorio Escritorio, Empresa Empresa, HttpClient Cliente, NotaFiscal NotaRejeitada, NotaFiscal NotaAutorizada);

    private async Task<Conta> CriarContaAsync(string sufixo, DateTime? certificadoValidade = null)
    {
        await using var db = _fixture.CreateDbContext();
        var cnpjEsc = $"7{sufixo}".PadRight(14, '0');
        var escritorio = Escritorio.Criar($"Escritório {sufixo}", "Esc", cnpjEsc, $"ori{sufixo}@teste.com", null, PlanoSaas.Profissional);
        db.Escritorios.Add(escritorio);
        await db.SaveChangesAsync();

        var empresa = Empresa.Criar(escritorio.Id, $"Empresa {sufixo}", $"Aurora {sufixo}", $"8{sufixo}".PadRight(14, '5'), "IE", "Rua", "1",
            "Centro", "São Paulo", "SP", "01310100", "3550308", "11900000000", $"emp{sufixo}@teste.com",
            RegimeTributario.SimplesNacional, AmbienteSefaz.Homologacao);
        if (certificadoValidade is { } v) empresa.AtualizarCertificado(new byte[] { 1, 2, 3 }, "senha", v, empresa.Cnpj);
        db.Empresas.Add(empresa);
        db.Usuarios.Add(Usuario.Criar(escritorio.Id, "Admin", $"ori{sufixo}@teste.com", BCrypt.Net.BCrypt.HashPassword("Senha@123"), "Admin"));

        var rejeitada = NotaFiscal.Criar(empresa.Id, TipoNota.NFe, 1, 10, FinalidadeNota.Normal, TipoOperacao.Saida, AmbienteSefaz.Homologacao);
        rejeitada.SetDestinatario("52998224725", "Maria Fictícia", "maria@exemplo.com", TipoPessoa.PessoaFisica,
            "Rua X", "1", "Centro", "BH", "MG", "30110000", "3106200", null);
        rejeitada.AdicionarItem(ItemNotaFiscal.Criar(rejeitada.Id, 1, "P1", "Caneca", "69120000", "6102", "UN", 1, 10m));
        rejeitada.Rejeitar("[778] Rejeição: Informado NCM inexistente");
        var autorizada = NotaFiscal.Criar(empresa.Id, TipoNota.NFe, 1, 11, FinalidadeNota.Normal, TipoOperacao.Saida, AmbienteSefaz.Homologacao);
        autorizada.SetDestinatario("11222333000181", "Cliente Exemplo", null, TipoPessoa.PessoaJuridica,
            "Rua Y", "2", "Centro", "BH", "MG", "30110000", "3106200", null);
        autorizada.AdicionarItem(ItemNotaFiscal.Criar(autorizada.Id, 1, "P1", "Caneca", "69120000", "6102", "UN", 1, 10m));
        autorizada.MarcarEnviada("<x/>");
        autorizada.Autorizar($"3526{sufixo}".PadRight(44, '1')[..44], "123", "<r/>");
        db.NotasFiscais.AddRange(rejeitada, autorizada);
        await db.SaveChangesAsync();

        var cliente = _fixture.Factory.CreateClient();
        var login = await (await cliente.PostAsJsonAsync("/api/auth/login", new LoginDto($"ori{sufixo}@teste.com", "Senha@123")))
            .Content.ReadFromJsonAsync<LoginResultDto>(Json);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
        var sel = await cliente.PostAsJsonAsync("/api/auth/selecionar-empresa", new SelecionarEmpresaDto(empresa.Id));
        var token = JsonDocument.Parse(await sel.Content.ReadAsStringAsync()).RootElement.GetProperty("accessToken").GetString();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return new Conta(escritorio, empresa, cliente, rejeitada, autorizada);
    }

    [Fact]
    public async Task Endpoints_da_Ori_exigem_autenticacao()
    {
        var anonimo = _fixture.Factory.CreateClient();
        (await anonimo.GetAsync("/api/assistente/status")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await anonimo.GetAsync("/api/assistente/base/mapa")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await anonimo.PostAsJsonAsync("/api/assistente/perguntar", new PerguntaOriDto("oi", new ContextoTelaDto("dashboard"))))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Status_informa_ori_habilitada_e_ia_desligada_sem_chave()
    {
        var a = await CriarContaAsync("1001");
        var status = await a.Cliente.GetFromJsonAsync<StatusAssistenteDto>("/api/assistente/status", Json);
        status!.Habilitado.Should().BeTrue();
        status.IaHabilitada.Should().BeFalse();
    }

    [Fact]
    public async Task Explicar_rejeicao_isola_empresas_e_responde_sem_modelo()
    {
        var a = await CriarContaAsync("1002");
        var b = await CriarContaAsync("1003");

        var alheia = await b.Cliente.PostAsJsonAsync($"/api/assistente/explicar-rejeicao/{a.NotaRejeitada.Id}", new ExplicarRejeicaoDto(null));
        alheia.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var propria = await a.Cliente.PostAsJsonAsync($"/api/assistente/explicar-rejeicao/{a.NotaRejeitada.Id}", new ExplicarRejeicaoDto(null));
        propria.StatusCode.Should().Be(HttpStatusCode.OK);
        var r = await propria.Content.ReadFromJsonAsync<RespostaOriDto>(Json);
        r!.RespondidaSemModelo.Should().BeTrue();
    }

    [Fact]
    public async Task Automacoes_e_lembretes_nao_atravessam_empresas()
    {
        var a = await CriarContaAsync("1004");
        var b = await CriarContaAsync("1005");

        (await b.Cliente.GetAsync($"/api/assistente/automacoes/preparar-nota/{a.NotaAutorizada.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await b.Cliente.GetAsync($"/api/assistente/automacoes/cadastro-sugerido/{a.NotaAutorizada.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await b.Cliente.PostAsJsonAsync("/api/assistente/lembretes",
            new CriarLembreteDto(a.NotaAutorizada.Id, "x", PeriodicidadeLembrete.Mensal, 5))).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var preparada = await a.Cliente.GetFromJsonAsync<EmitirNotaFiscalDto>($"/api/assistente/automacoes/preparar-nota/{a.NotaAutorizada.Id}", Json);
        preparada!.Itens.Should().ContainSingle(i => i.Ncm == "69120000");
        var criado = await a.Cliente.PostAsJsonAsync("/api/assistente/lembretes", new CriarLembreteDto(a.NotaAutorizada.Id, "Mensal", PeriodicidadeLembrete.Mensal, 5));
        criado.StatusCode.Should().Be(HttpStatusCode.OK);
        (await b.Cliente.GetFromJsonAsync<List<LembreteDto>>("/api/assistente/lembretes", Json)).Should().BeEmpty();
    }

    [Fact]
    public async Task Telas_internas_exigem_papel()
    {
        var a = await CriarContaAsync("1006");
        (await a.Cliente.GetAsync("/api/curadoria/publicacoes")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await a.Cliente.GetAsync("/api/interno/chamados")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await a.Cliente.GetAsync("/api/interno/saude")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Chamado_e_telemetria_sao_gravados_sem_dado_pessoal()
    {
        var a = await CriarContaAsync("1007");
        (await a.Cliente.PostAsJsonAsync("/api/assistente/chamados", new CriarChamadoDto(
            "Erro para o CPF 529.982.247-25", "joao@exemplo.com não recebe", null, SeveridadeChamado.Media, a.NotaRejeitada.Id, null)))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await a.Cliente.PostAsJsonAsync("/api/eventos", new LoteEventosDto(new()
        {
            new EventoProdutoDto("tela_aberta", "emitir-nfe", "{\"cpf\":\"52998224725\",\"aba\":\"Produtos\"}", DateTime.UtcNow),
            new EventoProdutoDto("tipo_nao_permitido", null, null, DateTime.UtcNow)
        }))).StatusCode.Should().Be(HttpStatusCode.Accepted);

        await using var db = _fixture.CreateDbContext();
        var chamado = await db.Chamados.SingleAsync(c => c.EscritorioId == a.Escritorio.Id);
        chamado.Titulo.Should().NotContain("529.982.247-25");
        chamado.Descricao.Should().NotContain("joao@exemplo.com");
        chamado.NotaFiscalId.Should().Be(a.NotaRejeitada.Id);
        var eventos = await db.EventosProduto.Where(e => e.EscritorioId == a.Escritorio.Id).ToListAsync();
        eventos.Should().ContainSingle();
        eventos[0].DadosJson.Should().NotContain("cpf").And.Contain("Produtos");
    }

    [Fact]
    public async Task Worker_de_CS_cria_alerta_de_certificado_uma_vez_e_so_para_o_escritorio_dono()
    {
        var a = await CriarContaAsync("1008", certificadoValidade: DateTime.UtcNow.AddDays(5));
        var b = await CriarContaAsync("1009");

        using (var scope = _fixture.Factory.Services.CreateScope())
            await SaudeContaWorker.ExecutarAsync(scope.ServiceProvider, CancellationToken.None);
        using (var scope = _fixture.Factory.Services.CreateScope())
            await SaudeContaWorker.ExecutarAsync(scope.ServiceProvider, CancellationToken.None);

        var alertasA = await a.Cliente.GetFromJsonAsync<List<AlertaCsDto>>("/api/assistente/alertas", Json);
        alertasA.Should().ContainSingle(x => x.Tipo == TipoAlertaCs.CertificadoVencendo);
        var alertasB = await b.Cliente.GetFromJsonAsync<List<AlertaCsDto>>("/api/assistente/alertas", Json);
        alertasB.Should().NotContain(x => x.EmpresaId == a.Empresa.Id);

        var id = alertasA!.Single(x => x.Tipo == TipoAlertaCs.CertificadoVencendo).Id;
        (await b.Cliente.PostAsync($"/api/assistente/alertas/{id}/dispensar", null)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await a.Cliente.PostAsync($"/api/assistente/alertas/{id}/dispensar", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
