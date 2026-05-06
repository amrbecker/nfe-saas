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
        builder.Property(e => e.CertificadoBytes).HasColumnType("bytea");
        builder.Property(e => e.CertificadoSenha).HasMaxLength(500);

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
