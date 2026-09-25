using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NfeSaas.Domain.Entities;

namespace NfeSaas.Infrastructure.Data.Configurations;

// Tabelas do assistente Ori (docs/assistente/).

public class EventoProdutoConfiguration : IEntityTypeConfiguration<EventoProduto>
{
    public void Configure(EntityTypeBuilder<EventoProduto> b)
    {
        b.ToTable("eventos_produto");
        b.HasKey(e => e.Id);
        b.Property(e => e.Tipo).HasMaxLength(60).IsRequired();
        b.Property(e => e.Tela).HasMaxLength(120);
        b.Property(e => e.DadosJson).HasMaxLength(2000);
        b.HasIndex(e => new { e.EscritorioId, e.OcorridoEm });
        b.HasIndex(e => new { e.Tipo, e.OcorridoEm });
    }
}

public class PreferenciaAssistenteConfiguration : IEntityTypeConfiguration<PreferenciaAssistente>
{
    public void Configure(EntityTypeBuilder<PreferenciaAssistente> b)
    {
        b.ToTable("preferencias_assistente");
        b.HasKey(p => p.Id);
        b.HasIndex(p => p.UsuarioId).IsUnique();
    }
}

public class SugestaoDispensadaConfiguration : IEntityTypeConfiguration<SugestaoDispensada>
{
    public void Configure(EntityTypeBuilder<SugestaoDispensada> b)
    {
        b.ToTable("sugestoes_dispensadas");
        b.HasKey(s => s.Id);
        b.Property(s => s.Chave).HasMaxLength(200).IsRequired();
        b.HasIndex(s => new { s.UsuarioId, s.Chave }).IsUnique();
    }
}

public class UsoAssistenteConfiguration : IEntityTypeConfiguration<UsoAssistente>
{
    public void Configure(EntityTypeBuilder<UsoAssistente> b)
    {
        b.ToTable("usos_assistente");
        b.HasKey(u => u.Id);
        b.Property(u => u.CustoEstimadoUsd).HasPrecision(12, 6);
        b.HasIndex(u => new { u.UsuarioId, u.Dia }).IsUnique();
        b.HasIndex(u => new { u.EscritorioId, u.Dia });
    }
}

public class InteracaoAssistenteConfiguration : IEntityTypeConfiguration<InteracaoAssistente>
{
    public void Configure(EntityTypeBuilder<InteracaoAssistente> b)
    {
        b.ToTable("interacoes_assistente");
        b.HasKey(i => i.Id);
        b.Property(i => i.PerguntaSanitizada).HasMaxLength(8000).IsRequired();
        b.Property(i => i.RespostaSanitizada).HasMaxLength(16000).IsRequired();
        b.Property(i => i.ArtigosCitados).HasMaxLength(1000);
        b.Property(i => i.Modelo).HasMaxLength(100).IsRequired();
        b.Property(i => i.CustoEstimadoUsd).HasPrecision(12, 6);
        b.HasIndex(i => new { i.EscritorioId, i.CreatedAt });
        b.HasIndex(i => i.UsuarioId);
    }
}

public class ConversaConfiguration : IEntityTypeConfiguration<Conversa>
{
    public void Configure(EntityTypeBuilder<Conversa> b)
    {
        b.ToTable("conversas_assistente");
        b.HasKey(c => c.Id);
        b.Property(c => c.Titulo).HasMaxLength(120).IsRequired();
        b.Property(c => c.ResumoAnterior).HasMaxLength(4000);
        b.HasMany(c => c.Mensagens).WithOne().HasForeignKey(m => m.ConversaId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(c => c.Mensagens).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasIndex(c => new { c.UsuarioId, c.EmpresaId, c.UltimaMensagemEm });
        b.HasIndex(c => c.UltimaMensagemEm);
    }
}

public class MensagemConversaConfiguration : IEntityTypeConfiguration<MensagemConversa>
{
    public void Configure(EntityTypeBuilder<MensagemConversa> b)
    {
        b.ToTable("mensagens_conversa");
        b.HasKey(m => m.Id);
        b.Property(m => m.ConteudoSanitizado).HasMaxLength(16000).IsRequired();
        b.Property(m => m.FerramentasJson).HasMaxLength(16000);
        b.HasIndex(m => new { m.ConversaId, m.CriadaEm });
    }
}

public class ChamadoConfiguration : IEntityTypeConfiguration<Chamado>
{
    public void Configure(EntityTypeBuilder<Chamado> b)
    {
        b.ToTable("chamados");
        b.HasKey(c => c.Id);
        b.Property(c => c.Titulo).HasMaxLength(200).IsRequired();
        b.Property(c => c.Descricao).HasMaxLength(4000).IsRequired();
        b.Property(c => c.Passos).HasMaxLength(4000);
        b.Property(c => c.ContextoTecnicoJson).HasMaxLength(16000);
        b.Property(c => c.IssueUrl).HasMaxLength(500);
        b.Property(c => c.NotaTriagem).HasMaxLength(2000);
        b.HasIndex(c => new { c.Status, c.CreatedAt });
        b.HasIndex(c => c.EscritorioId);
    }
}

public class SinalProdutoConfiguration : IEntityTypeConfiguration<SinalProduto>
{
    public void Configure(EntityTypeBuilder<SinalProduto> b)
    {
        b.ToTable("sinais_produto");
        b.HasKey(s => s.Id);
        b.Property(s => s.ResumoSanitizado).HasMaxLength(2000).IsRequired();
        b.Property(s => s.Tela).HasMaxLength(120);
        b.Property(s => s.Tema).HasMaxLength(80);
        b.HasIndex(s => new { s.Tipo, s.CreatedAt });
    }
}

public class AlertaCsConfiguration : IEntityTypeConfiguration<AlertaCs>
{
    public void Configure(EntityTypeBuilder<AlertaCs> b)
    {
        b.ToTable("alertas_cs");
        b.HasKey(a => a.Id);
        b.Property(a => a.Chave).HasMaxLength(200).IsRequired();
        b.Property(a => a.Mensagem).HasMaxLength(500).IsRequired();
        b.Property(a => a.LinkAcao).HasMaxLength(300);
        b.HasIndex(a => a.Chave).IsUnique();
        b.HasIndex(a => new { a.EscritorioId, a.EmpresaId, a.DispensadoEm });
    }
}

public class LembreteEmissaoConfiguration : IEntityTypeConfiguration<LembreteEmissao>
{
    public void Configure(EntityTypeBuilder<LembreteEmissao> b)
    {
        b.ToTable("lembretes_emissao");
        b.HasKey(l => l.Id);
        b.Property(l => l.Descricao).HasMaxLength(200).IsRequired();
        b.HasIndex(l => new { l.Ativo, l.ProximaEm });
        b.HasIndex(l => new { l.EmpresaId, l.NotaModeloId });
    }
}

public class FonteMonitoradaConfiguration : IEntityTypeConfiguration<FonteMonitorada>
{
    public void Configure(EntityTypeBuilder<FonteMonitorada> b)
    {
        b.ToTable("fontes_monitoradas");
        b.HasKey(f => f.Id);
        b.Property(f => f.Nome).HasMaxLength(200).IsRequired();
        b.Property(f => f.Url).HasMaxLength(500).IsRequired();
        b.Property(f => f.TermosFiltro).HasMaxLength(1000);
        b.Property(f => f.UltimoHash).HasMaxLength(128);
        b.HasIndex(f => f.Url).IsUnique();
    }
}

public class PublicacaoDetectadaConfiguration : IEntityTypeConfiguration<PublicacaoDetectada>
{
    public void Configure(EntityTypeBuilder<PublicacaoDetectada> b)
    {
        b.ToTable("publicacoes_detectadas");
        b.HasKey(p => p.Id);
        b.Property(p => p.Titulo).HasMaxLength(500).IsRequired();
        b.Property(p => p.Url).HasMaxLength(1000);
        b.Property(p => p.Hash).HasMaxLength(128).IsRequired();
        b.Property(p => p.Resumo).HasMaxLength(4000);
        b.Property(p => p.ArtigosAfetados).HasMaxLength(1000);
        b.Property(p => p.NotaCurador).HasMaxLength(2000);
        b.HasOne(p => p.Fonte).WithMany().HasForeignKey(p => p.FonteId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(p => new { p.FonteId, p.Hash }).IsUnique();
        b.HasIndex(p => new { p.Status, p.DetectadaEm });
    }
}
