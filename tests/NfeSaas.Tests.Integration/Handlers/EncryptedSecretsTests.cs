using Microsoft.EntityFrameworkCore;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Tests.Integration.Fixtures;

namespace NfeSaas.Tests.Integration.Handlers;

public class EncryptedSecretsTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public EncryptedSecretsTests(DatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CertificadoSenhaECscToken_SaoCifradosNoBancoEDecifradosNaLeitura()
    {
        const string senhaClara = "MinhaSenha@123!";
        const string cscClaro = "abcdef0123456789ABCDEF0123456789";
        Guid empresaId;

        await using (var db = _fixture.CreateDbContext())
        {
            var escritorio = Escritorio.Criar(
                "Esc Cifragem", "Esc", "12345678000199",
                "cifragem@teste.com", null, PlanoSaas.Basico);
            db.Escritorios.Add(escritorio);
            await db.SaveChangesAsync();

            var empresa = Empresa.Criar(escritorio.Id,
                "Emp Cifragem", "Emp", "98765432000188", "IE",
                "Rua", "1", "Centro", "São Paulo", "SP",
                "01310100", "3550308", "11000000000",
                "emp@cifragem.com",
                RegimeTributario.SimplesNacional, AmbienteSefaz.Homologacao);
            empresa.AtualizarCertificado(new byte[] { 1, 2, 3 }, senhaClara,
                DateTime.UtcNow.AddYears(1), "98765432000188");
            empresa.AtualizarCsc("1", cscClaro);
            db.Empresas.Add(empresa);
            await db.SaveChangesAsync();

            empresaId = empresa.Id;
        }

        await using (var db = _fixture.CreateDbContext())
        {
            var empresa = await db.Empresas.FirstAsync(e => e.Id == empresaId);
            empresa.CertificadoSenha.Should().Be(senhaClara);
            empresa.CscToken.Should().Be(cscClaro);
        }

        await using (var db = _fixture.CreateDbContext())
        {
            var (senhaRaw, cscRaw) = await LerColunasCruasAsync(db, empresaId);

            senhaRaw.Should().StartWith("enc:v1:");
            senhaRaw.Should().NotContain(senhaClara);
            cscRaw.Should().StartWith("enc:v1:");
            cscRaw.Should().NotContain(cscClaro);
        }
    }

    [Fact]
    public async Task ValorLegadoSemPrefixo_RetornadoComoTextoClaro_ParaCompatibilidade()
    {
        const string senhaLegada = "senha-pre-cifragem";
        Guid empresaId;

        await using (var db = _fixture.CreateDbContext())
        {
            var escritorio = Escritorio.Criar(
                "Esc Legado", "Esc", "12345678000277",
                "legado@teste.com", null, PlanoSaas.Basico);
            db.Escritorios.Add(escritorio);
            await db.SaveChangesAsync();

            var empresa = Empresa.Criar(escritorio.Id,
                "Emp Legado", "Emp", "98765432000266", "IE",
                "Rua", "1", "Centro", "São Paulo", "SP",
                "01310100", "3550308", "11000000000",
                "emp@legado.com",
                RegimeTributario.SimplesNacional, AmbienteSefaz.Homologacao);
            db.Empresas.Add(empresa);
            await db.SaveChangesAsync();

            empresaId = empresa.Id;

            // Simula dado legado em texto claro escrevendo direto na coluna, bypassando o converter.
            await db.Database.ExecuteSqlInterpolatedAsync(
                $@"UPDATE empresas SET ""CertificadoSenha"" = {senhaLegada} WHERE ""Id"" = {empresaId}");
        }

        await using (var db = _fixture.CreateDbContext())
        {
            var empresa = await db.Empresas.FirstAsync(e => e.Id == empresaId);
            empresa.CertificadoSenha.Should().Be(senhaLegada);
        }
    }

    private static async Task<(string CertificadoSenha, string CscToken)> LerColunasCruasAsync(
        Infrastructure.Data.NfeDbContext db, Guid empresaId)
    {
        var conn = db.Database.GetDbConnection();
        await conn.OpenAsync();
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT ""CertificadoSenha"", ""CscToken"" FROM empresas WHERE ""Id"" = @id";
            var p = cmd.CreateParameter();
            p.ParameterName = "@id";
            p.Value = empresaId;
            cmd.Parameters.Add(p);

            await using var reader = await cmd.ExecuteReaderAsync();
            var ok = await reader.ReadAsync();
            ok.Should().BeTrue("a empresa salva deve existir no banco");
            return (reader.GetString(0), reader.GetString(1));
        }
        finally
        {
            await conn.CloseAsync();
        }
    }
}
