using Core.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Infrastructure.Persistence
{

    public class LiberdadeDbContext : DbContext
    {
        public LiberdadeDbContext(DbContextOptions<LiberdadeDbContext> options) : base(options)
        {
        }

        public DbSet<Ativo> Ativos { get; set; }
        public DbSet<MetaAlocacao> MetaAlocacoes { get; set; }
        public DbSet<PosicaoCarteira> PosicaoCarteiras { get; set; }
        public DbSet<Transacao> Transacoes { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
