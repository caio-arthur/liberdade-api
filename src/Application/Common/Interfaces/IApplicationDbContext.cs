using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<Ativo> Ativos { get; }
        DbSet<MetaAlocacao> MetaAlocacoes { get; }
        DbSet<PosicaoCarteira> PosicaoCarteiras { get; }
        DbSet<Transacao> Transacoes { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
        DatabaseFacade Database { get; }
    }
}