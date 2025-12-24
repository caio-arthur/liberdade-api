using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class TransacaoConfiguration : IEntityTypeConfiguration<Transacao>
    {
        public void Configure(EntityTypeBuilder<Transacao> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.ValorTotal).HasPrecision(18, 2);
            builder.Property(t => t.Quantidade).HasPrecision(18, 8);

            builder.Property(t => t.TipoTransacao)
                   .HasConversion<string>();

            builder.HasOne(t => t.Ativo)
                   .WithMany()
                   .HasForeignKey(t => t.AtivoId)
                   .IsRequired(false);
        }
    }
}