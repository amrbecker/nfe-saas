using FluentAssertions;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Tests.Unit.Domain;

public class ClienteTests
{
    private static Cliente CriarCliente(
        TipoPessoa tipo = TipoPessoa.PessoaJuridica,
        string? cpfCnpj = "12345678000195",
        string uf = "SP",
        IndicadorIeDestinatario indIe = IndicadorIeDestinatario.Contribuinte,
        string? ie = "111111111111") =>
        Cliente.Criar(
            Guid.NewGuid(), tipo, cpfCnpj,
            "Cliente Teste LTDA", "Fantasia", "cli@teste.com", "11999999999",
            "Rua das Flores", "100", "Bloco B", "Centro", "São Paulo",
            uf, "01310100", "3550308",
            ie, indIe);

    [Fact]
    public void Criar_DevePersistirTodosOsCampos()
    {
        var empresaId = Guid.NewGuid();
        var c = Cliente.Criar(
            empresaId, TipoPessoa.PessoaFisica, "12345678901",
            "João Silva", null, "joao@test.com", "11888888888",
            "Av Paulista", "1500", null, "Bela Vista", "São Paulo",
            "SP", "01310100", "3550308",
            null, IndicadorIeDestinatario.NaoContribuinte);

        c.EmpresaId.Should().Be(empresaId);
        c.TipoPessoa.Should().Be(TipoPessoa.PessoaFisica);
        c.CpfCnpj.Should().Be("12345678901");
        c.RazaoSocial.Should().Be("João Silva");
        c.NomeFantasia.Should().BeNull();
        c.Email.Should().Be("joao@test.com");
        c.Telefone.Should().Be("11888888888");
        c.Logradouro.Should().Be("Av Paulista");
        c.Numero.Should().Be("1500");
        c.Complemento.Should().BeNull();
        c.Bairro.Should().Be("Bela Vista");
        c.Cidade.Should().Be("São Paulo");
        c.Uf.Should().Be("SP");
        c.Cep.Should().Be("01310100");
        c.CodigoMunicipio.Should().Be("3550308");
        c.InscricaoEstadual.Should().BeNull();
        c.IndicadorIe.Should().Be(IndicadorIeDestinatario.NaoContribuinte);
        c.Ativo.Should().BeTrue();
    }

    [Fact]
    public void Criar_NovoCliente_DeveEstarAtivo()
    {
        var c = CriarCliente();
        c.Ativo.Should().BeTrue();
    }

    [Fact]
    public void Desativar_DeveMudarStatus()
    {
        var c = CriarCliente();
        c.Desativar();
        c.Ativo.Should().BeFalse();
    }

    [Fact]
    public void Ativar_AposDesativar_DeveRestaurarStatus()
    {
        var c = CriarCliente();
        c.Desativar();
        c.Ativar();
        c.Ativo.Should().BeTrue();
    }

    [Fact]
    public void Atualizar_DeveModificarTodosOsCampos()
    {
        var c = CriarCliente();
        var antes = c.UpdatedAt;

        c.Atualizar(
            TipoPessoa.PessoaFisica, "99988877766",
            "Nome Novo", "Fantasia Nova", "novo@email.com", "21999999999",
            "Rua Nova", "200", "Apto 1", "Novo Bairro", "Rio de Janeiro",
            "RJ", "20000000", "3304557",
            null, IndicadorIeDestinatario.IsentoIcms);

        c.TipoPessoa.Should().Be(TipoPessoa.PessoaFisica);
        c.CpfCnpj.Should().Be("99988877766");
        c.RazaoSocial.Should().Be("Nome Novo");
        c.NomeFantasia.Should().Be("Fantasia Nova");
        c.Email.Should().Be("novo@email.com");
        c.Telefone.Should().Be("21999999999");
        c.Logradouro.Should().Be("Rua Nova");
        c.Numero.Should().Be("200");
        c.Complemento.Should().Be("Apto 1");
        c.Bairro.Should().Be("Novo Bairro");
        c.Cidade.Should().Be("Rio de Janeiro");
        c.Uf.Should().Be("RJ");
        c.Cep.Should().Be("20000000");
        c.CodigoMunicipio.Should().Be("3304557");
        c.InscricaoEstadual.Should().BeNull();
        c.IndicadorIe.Should().Be(IndicadorIeDestinatario.IsentoIcms);
        c.UpdatedAt.Should().NotBe(antes);
    }

    [Theory]
    [InlineData(IndicadorIeDestinatario.Contribuinte, 1)]
    [InlineData(IndicadorIeDestinatario.IsentoIcms, 2)]
    [InlineData(IndicadorIeDestinatario.NaoContribuinte, 9)]
    public void IndicadorIeDestinatario_TemValoresSefazCorretos(IndicadorIeDestinatario ind, int valorEsperado)
    {
        ((int)ind).Should().Be(valorEsperado);
    }

    [Fact]
    public void Criar_Estrangeiro_DeveAceitarCpfCnpjNulo()
    {
        var c = Cliente.Criar(
            Guid.NewGuid(), TipoPessoa.Estrangeiro, null,
            "Foreign Corp Inc", null, null, null,
            "Some Street", "S/N", null, "Downtown", "New York",
            "EX", "00000000", "9999999",
            null, IndicadorIeDestinatario.NaoContribuinte);

        c.TipoPessoa.Should().Be(TipoPessoa.Estrangeiro);
        c.CpfCnpj.Should().BeNull();
    }

    [Fact]
    public void Delete_NovoCliente_DeveSoftDelete()
    {
        var c = CriarCliente();
        c.Delete();
        c.IsDeleted.Should().BeTrue();
    }
}
