using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class PosicaoCarteiraConfiguration : IEntityTypeConfiguration<PosicaoCarteira>
    {
        public void Configure(EntityTypeBuilder<PosicaoCarteira> builder)
        {
            builder.HasKey(p => p.AtivoId);

            builder.Property(p => p.Categoria)
                   .HasConversion<string>();

            builder.Property(p => p.ValorTotalAtual).HasPrecision(18, 2);
        }
    }
}