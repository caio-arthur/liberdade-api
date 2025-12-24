using Application.Common.Interfaces;
using Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Infrastructure.Persistence
{

    public class LiberdadeDbContext : IdentityDbContext, IApplicationDbContext
    {
        public LiberdadeDbContext(DbContextOptions<LiberdadeDbContext> options) : base(options)
        {
        }

        public DbSet<Ativo> Ativos { get; set; }
        public DbSet<MetaAlocacao> MetaAlocacoes { get; set; }
        public DbSet<PosicaoCarteira> PosicaoCarteiras { get; set; }
        public DbSet<Transacao> Transacoes { get; set; }
        public DbSet<HistoricoPatrimonio> HistoricoPatrimonios { get; set; }
        public DbSet<FeriadoNacional> FeriadosNacionais { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
