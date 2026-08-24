using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NfeSaas.Domain.Entities;

namespace NfeSaas.Infrastructure.Data.Configurations;

public class EscritorioConfiguration : IEntityTypeConfiguration<Escritorio>
{
    public void Configure(EntityTypeBuilder<Escritorio> builder)
    {
        builder.ToTable("escritorios");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.RazaoSocial).IsRequired().HasMaxLength(150);
        builder.Property(e => e.NomeFantasia).HasMaxLength(150);
        builder.Property(e => e.Cnpj).IsRequired().HasMaxLength(14);
        builder.HasIndex(e => e.Cnpj).IsUnique();
        builder.Property(e => e.Email).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Telefone).HasMaxLength(20);
        builder.Property(e => e.TrialInicioEm).IsRequired();
        builder.Property(e => e.TrialFimEm).IsRequired();
    }
}

public class EmpresaConfiguration : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> builder)
    {
        builder.ToTable("empresas");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.RazaoSocial).IsRequired().HasMaxLength(150);
        builder.Property(e => e.NomeFantasia).HasMaxLength(150);
        builder.Property(e => e.Cnpj).IsRequired().HasMaxLength(14);
        builder.HasIndex(e => e.Cnpj).IsUnique();
        builder.Property(e => e.InscricaoEstadual).HasMaxLength(20);
        builder.Property(e => e.Email).HasMaxLength(100);
        builder.Property(e => e.Telefone).HasMaxLength(20);
        builder.Property(e => e.Cep).HasMaxLength(8);
        builder.Property(e => e.Cnae).HasMaxLength(7);
        builder.Property(e => e.CertificadoBytes).HasColumnType("bytea");
        // CertificadoSenha e CscToken são cifrados em repouso via EncryptedStringConverter
        // (configurado em NfeDbContext.OnModelCreating). HasMaxLength fica em 1000 para acomodar
        // o overhead do Data Protection sobre senhas longas.
        builder.Property(e => e.CertificadoSenha).HasMaxLength(1000);
        builder.Property(e => e.CscId).HasMaxLength(6);
        builder.Property(e => e.CscToken).HasMaxLength(1000);

        builder.HasOne(e => e.Escritorio)
            .WithMany(es => es.Empresas)
            .HasForeignKey(e => e.EscritorioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Nome).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(100);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.SenhaHash).IsRequired().HasMaxLength(500);
        builder.Property(u => u.Role).HasMaxLength(50);
        builder.Property(u => u.RefreshToken).HasMaxLength(500);

        builder.HasOne(u => u.Escritorio)
            .WithMany(e => e.Usuarios)
            .HasForeignKey(u => u.EscritorioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class NotaFiscalConfiguration : IEntityTypeConfiguration<NotaFiscal>
{
    public void Configure(EntityTypeBuilder<NotaFiscal> builder)
    {
        builder.ToTable("notas_fiscais");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.ChaveAcesso).HasMaxLength(44);
        builder.HasIndex(n => n.ChaveAcesso);
        // Anti-duplicação: mesma empresa não pode emitir 2x a mesma série/número/tipo/ambiente
        builder.HasIndex(n => new { n.EmpresaId, n.Tipo, n.Serie, n.Numero, n.Ambiente })
               .IsUnique()
               .HasDatabaseName("ix_notas_fiscais_dedup");
        builder.Property(n => n.Protocolo).HasMaxLength(50);
        builder.Property(n => n.DestinatarioCpfCnpj).HasMaxLength(14);
        builder.Property(n => n.DestinatarioRazaoSocial).HasMaxLength(150);
        builder.Property(n => n.DestinatarioEmail).HasMaxLength(100);
        builder.Property(n => n.XmlEnvio).HasColumnType("text");
        builder.Property(n => n.XmlRetorno).HasColumnType("text");
        builder.Property(n => n.XmlCancelamento).HasColumnType("text");
        builder.Property(n => n.MotivoRejeicao).HasMaxLength(500);
        builder.Property(n => n.InformacoesAdicionais).HasMaxLength(2000);
        builder.Property(n => n.TotalNota).HasPrecision(15, 2);
        builder.Property(n => n.TotalProdutos).HasPrecision(15, 2);
        builder.Property(n => n.TotalIcms).HasPrecision(15, 2);

        builder.HasOne(n => n.Empresa)
            .WithMany(e => e.Notas)
            .HasForeignKey(n => n.EmpresaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(n => n.Itens)
            .WithOne(i => i.NotaFiscal)
            .HasForeignKey(i => i.NotaFiscalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ItemNotaFiscalConfiguration : IEntityTypeConfiguration<ItemNotaFiscal>
{
    public void Configure(EntityTypeBuilder<ItemNotaFiscal> builder)
    {
        builder.ToTable("itens_nota_fiscal");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.CodigoProduto).IsRequired().HasMaxLength(60);
        builder.Property(i => i.Descricao).IsRequired().HasMaxLength(120);
        builder.Property(i => i.Ncm).HasMaxLength(8);
        builder.Property(i => i.Cfop).HasMaxLength(4);
        builder.Property(i => i.UnidadeComercial).HasMaxLength(6);
        builder.Property(i => i.ValorUnitario).HasPrecision(15, 4);
        builder.Property(i => i.ValorTotal).HasPrecision(15, 2);
        builder.Property(i => i.ValorDesconto).HasPrecision(15, 2);
        builder.Property(i => i.BaseCalculoIcms).HasPrecision(15, 2);
        builder.Property(i => i.ValorIcms).HasPrecision(15, 2);
        builder.Property(i => i.ValorPis).HasPrecision(15, 2);
        builder.Property(i => i.ValorCofins).HasPrecision(15, 2);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Acao).IsRequired().HasMaxLength(100);
        builder.Property(a => a.ChaveNFe).HasMaxLength(44);
        builder.Property(a => a.Detalhes).HasMaxLength(1000);
        builder.Property(a => a.IpOrigem).HasMaxLength(45);
        builder.HasIndex(a => a.EmpresaId);
        builder.HasIndex(a => a.Timestamp);
    }
}

public class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.ToTable("produtos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Codigo).IsRequired().HasMaxLength(60);
        builder.Property(p => p.Descricao).IsRequired().HasMaxLength(120);
        builder.Property(p => p.Ncm).IsRequired().HasMaxLength(8);
        builder.Property(p => p.Cest).HasMaxLength(7);
        builder.Property(p => p.CfopPadrao).IsRequired().HasMaxLength(4);
        builder.Property(p => p.UnidadeComercial).IsRequired().HasMaxLength(6);
        builder.Property(p => p.CodigoEan).HasMaxLength(14);
        builder.Property(p => p.CodigoAnp).HasMaxLength(9);
        builder.Property(p => p.ValorUnitarioPadrao).HasPrecision(15, 4);

        builder.HasIndex(p => new { p.EmpresaId, p.Codigo }).IsUnique()
               .HasDatabaseName("ix_produtos_empresa_codigo");

        builder.HasOne(p => p.Empresa)
            .WithMany()
            .HasForeignKey(p => p.EmpresaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("clientes");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.CpfCnpj).HasMaxLength(14);
        builder.Property(c => c.RazaoSocial).IsRequired().HasMaxLength(150);
        builder.Property(c => c.NomeFantasia).HasMaxLength(150);
        builder.Property(c => c.Email).HasMaxLength(100);
        builder.Property(c => c.Telefone).HasMaxLength(20);
        builder.Property(c => c.Logradouro).IsRequired().HasMaxLength(150);
        builder.Property(c => c.Numero).IsRequired().HasMaxLength(20);
        builder.Property(c => c.Complemento).HasMaxLength(60);
        builder.Property(c => c.Bairro).IsRequired().HasMaxLength(80);
        builder.Property(c => c.Cidade).IsRequired().HasMaxLength(80);
        builder.Property(c => c.Uf).IsRequired().HasMaxLength(2);
        builder.Property(c => c.Cep).IsRequired().HasMaxLength(8);
        builder.Property(c => c.CodigoMunicipio).IsRequired().HasMaxLength(7);
        builder.Property(c => c.InscricaoEstadual).HasMaxLength(20);

        builder.HasIndex(c => new { c.EmpresaId, c.CpfCnpj })
               .HasDatabaseName("ix_clientes_empresa_cpfcnpj");

        builder.HasOne(c => c.Empresa)
            .WithMany()
            .HasForeignKey(c => c.EmpresaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class EventoFiscalConfiguration : IEntityTypeConfiguration<EventoFiscal>
{
    public void Configure(EntityTypeBuilder<EventoFiscal> builder)
    {
        builder.ToTable("eventos_fiscais");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.ChaveAcesso).HasMaxLength(44);
        builder.Property(e => e.Justificativa).IsRequired().HasMaxLength(2000);
        builder.Property(e => e.Protocolo).HasMaxLength(50);
        builder.Property(e => e.MotivoRejeicao).HasMaxLength(500);
        builder.Property(e => e.XmlEvento).HasColumnType("text");
        builder.Property(e => e.XmlRetorno).HasColumnType("text");

        builder.HasIndex(e => new { e.EmpresaId, e.ChaveAcesso });
        builder.HasIndex(e => new { e.EmpresaId, e.Tipo, e.DataEvento });

        builder.HasOne(e => e.Empresa)
            .WithMany()
            .HasForeignKey(e => e.EmpresaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class NcmConfiguration : IEntityTypeConfiguration<Ncm>
{
    public void Configure(EntityTypeBuilder<Ncm> builder)
    {
        builder.ToTable("ncms");
        builder.HasKey(n => n.Codigo);
        builder.Property(n => n.Codigo).HasMaxLength(8).IsRequired();
        builder.Property(n => n.Descricao).IsRequired().HasMaxLength(500);
        builder.Property(n => n.CategoriaCapitulo).HasMaxLength(2);
        builder.Property(n => n.Posicao).HasMaxLength(4);
        builder.Property(n => n.AliquotaIpiPadrao).HasPrecision(6, 2);
        builder.Property(n => n.VersaoTabela).HasMaxLength(20);

        // Índices para autocomplete: pré-fixo no código + busca textual na descrição.
        builder.HasIndex(n => n.Codigo).HasDatabaseName("ix_ncms_codigo_prefix");
        builder.HasIndex(n => n.Posicao);
        builder.HasIndex(n => n.Ativo);
    }
}

public class CnaeConfiguration : IEntityTypeConfiguration<Cnae>
{
    public void Configure(EntityTypeBuilder<Cnae> builder)
    {
        builder.ToTable("cnaes");
        builder.HasKey(c => c.Codigo);
        builder.Property(c => c.Codigo).HasMaxLength(7).IsRequired();
        builder.Property(c => c.Descricao).IsRequired().HasMaxLength(500);
        builder.Property(c => c.Secao).HasMaxLength(1);
        builder.Property(c => c.Divisao).HasMaxLength(2);

        builder.HasIndex(c => c.Ativo);
    }
}

public class ConfiguracaoEmpresaConfiguration : IEntityTypeConfiguration<ConfiguracaoEmpresa>
{
    public void Configure(EntityTypeBuilder<ConfiguracaoEmpresa> builder)
    {
        builder.ToTable("configuracoes_empresa");
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => c.EmpresaId).IsUnique();

        builder.HasOne(c => c.Empresa)
            .WithOne()
            .HasForeignKey<ConfiguracaoEmpresa>(c => c.EmpresaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
