using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class MetaAlocacaoConfiguration : IEntityTypeConfiguration<MetaAlocacao>
    {
        public void Configure(EntityTypeBuilder<MetaAlocacao> builder)
        {
            builder.HasKey(m => m.Id);

            builder.Property(m => m.Categoria)
                   .HasConversion<string>();
        }
    }
}