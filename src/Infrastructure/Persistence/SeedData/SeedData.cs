using Core.Entities;
using Core.Enums;
using Infrastructure.Persistence;

namespace Infrastructure.Persistence.SeedData
{
    public static class SeedData
    {
        public static void Seed(LiberdadeDbContext context)
        {
            if (!context.MetaAlocacoes.Any())
            {
                context.MetaAlocacoes.AddRange(
                    new MetaAlocacao { NumeroFase = 1, Categoria = AtivoCategoria.RendaFixaLiquidez, PercentualAlvo = 100, Ativa = true }
                );
            }

            if (!context.Ativos.Any())
            {
                context.Ativos.AddRange(
                    new Ativo { Codigo = "SELIC2031", Nome = "Tesouro Selic 2031", Categoria = AtivoCategoria.RendaFixaLiquidez }
                );
            }

            context.SaveChanges();
        }
    }
}
