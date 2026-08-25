using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Tests.Integration.Fixtures;

namespace NfeSaas.Tests.Integration.Handlers;

// Cobre o FiscalImmutabilityInterceptor com um Postgres real (Testcontainers) — só assim o
// SaveChangesInterceptor de fato roda. Bug real de produção: EmailEnviadoEm era bloqueado
// pelo guard de imutabilidade fiscal mesmo o e-mail já tendo sido entregue via Resend.
public class FiscalImmutabilityInterceptorTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public FiscalImmutabilityInterceptorTests(DatabaseFixture fixture) => _fixture = fixture;

    private async Task<(Guid EmpresaId, Guid NotaId)> SeedNotaAutorizadaAsync()
    {
        await using var db = _fixture.CreateDbContext();

        var sufixo = Guid.NewGuid().ToString("N")[..12];
        var escritorio = Escritorio.Criar("Escritório Teste", "Escritório", $"1{sufixo}",
            $"imut{Guid.NewGuid():N}@teste.com", null, PlanoSaas.Basico);
        db.Escritorios.Add(escritorio);

        var empresa = Empresa.Criar(escritorio.Id, "Empresa Teste", "Empresa",
            $"2{sufixo}", "IE", "Rua", "1", "Centro", "São Paulo", "SP",
            "01310100", "3550308", "11900000000", $"emp{Guid.NewGuid():N}@teste.com",
            RegimeTributario.SimplesNacional, AmbienteSefaz.Homologacao);
        db.Empresas.Add(empresa);

        var nota = NotaFiscal.Criar(empresa.Id, TipoNota.NFe, 1, 1,
            FinalidadeNota.Normal, TipoOperacao.Saida, AmbienteSefaz.Homologacao);
        nota.MarcarEnviada("<x/>");
        var chave = (Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"))[..44];
        nota.Autorizar(chave, "PROT", "<r/>");
        db.NotasFiscais.Add(nota);

        await db.SaveChangesAsync();
        return (empresa.Id, nota.Id);
    }

    [Fact]
    public async Task AtualizarEmailEnviadoEm_EmNotaAutorizada_NaoLancaExcecao()
    {
        var (_, notaId) = await SeedNotaAutorizadaAsync();

        await using var db = _fixture.CreateDbContext();
        var nota = await db.NotasFiscais.FirstAsync(n => n.Id == notaId);

        nota.RegistrarEnvioEmail();
        var act = async () => await db.SaveChangesAsync();

        await act.Should().NotThrowAsync();

        await using var verifyDb = _fixture.CreateDbContext();
        var atualizada = await verifyDb.NotasFiscais.FirstAsync(n => n.Id == notaId);
        atualizada.EmailEnviadoEm.Should().NotBeNull();
        atualizada.Situacao.Should().Be(SituacaoNota.Autorizada);
    }

    [Fact]
    public async Task AlterarCampoFiscalCentral_EmNotaAutorizada_LancaInvalidOperationException()
    {
        var (_, notaId) = await SeedNotaAutorizadaAsync();

        await using var db = _fixture.CreateDbContext();
        var nota = await db.NotasFiscais.FirstAsync(n => n.Id == notaId);

        // Simula uma tentativa de alterar um campo do documento fiscal transmitido (não deve
        // ser permitido de forma alguma, mesmo via reflection/EF direto).
        typeof(NotaFiscal).GetProperty(nameof(NotaFiscal.MotivoRejeicao))!
            .SetValue(nota, "tentativa indevida de alteração");

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*imutáveis*");
    }

    [Fact]
    public async Task Cancelar_EmNotaAutorizada_AindaFunciona()
    {
        var (_, notaId) = await SeedNotaAutorizadaAsync();

        await using var db = _fixture.CreateDbContext();
        var nota = await db.NotasFiscais.FirstAsync(n => n.Id == notaId);

        nota.Cancelar("<cancelamento/>");
        var act = async () => await db.SaveChangesAsync();

        await act.Should().NotThrowAsync();

        await using var verifyDb = _fixture.CreateDbContext();
        var cancelada = await verifyDb.NotasFiscais.FirstAsync(n => n.Id == notaId);
        cancelada.Situacao.Should().Be(SituacaoNota.Cancelada);
    }
}
