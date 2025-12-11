using Core.Entities;
using Core.Enums;

namespace Infrastructure.Persistence.SeedData
{
    public static class SeedData
    {
        public static void Seed(LiberdadeDbContext context)
        {
            if (!context.Ativos.Any())
            {
                context.Ativos.AddRange(
                    new Ativo { Codigo = "SELIC2031", Nome = "Tesouro Selic 2031", Categoria = AtivoCategoria.RendaFixaLiquidez },
                    new Ativo { Codigo = "RBRR11", Nome = "FII RBR Rendimento High Grade", Categoria = AtivoCategoria.FiiPapel },
                    new Ativo { Codigo = "CPTS11", Nome = "Capitania Securities II", Categoria = AtivoCategoria.FiiPapel },
                    new Ativo { Codigo = "KNCR11", Nome = "Kinea Rendimentos Imobiliários", Categoria = AtivoCategoria.FiiPapel },
                    new Ativo { Codigo = "XPML11", Nome = "XP Malls", Categoria = AtivoCategoria.FiiTijoloShopping },
                    new Ativo { Codigo = "VISC11", Nome = "Vinci Shopping Centers", Categoria = AtivoCategoria.FiiTijoloShopping },
                    new Ativo { Codigo = "HGLG11", Nome = "PATRIA LOG", Categoria = AtivoCategoria.FiiTijoloLogistica },
                    new Ativo { Codigo = "XPLG11", Nome = "XP Log", Categoria = AtivoCategoria.FiiTijoloLogistica }
                );
                context.SaveChanges(); 
            }

            if (!context.MetaAlocacoes.Any())
            {
                context.MetaAlocacoes.Add(
                    new MetaAlocacao { NumeroFase = 1, Categoria = AtivoCategoria.RendaFixaLiquidez, PercentualAlvo = 100, Ativa = true }
                );

                context.MetaAlocacoes.AddRange(
                    new MetaAlocacao { NumeroFase = 2, Categoria = AtivoCategoria.FiiPapel, PercentualAlvo = 20, Ativa = false },
                    new MetaAlocacao { NumeroFase = 2, Categoria = AtivoCategoria.FiiHibrido, PercentualAlvo = 30, Ativa = false },
                    new MetaAlocacao { NumeroFase = 2, Categoria = AtivoCategoria.FiiTijoloLogistica, PercentualAlvo = 30, Ativa = false },
                    new MetaAlocacao { NumeroFase = 2, Categoria = AtivoCategoria.FiiTijoloShopping, PercentualAlvo = 20, Ativa = false }
                );

                context.SaveChanges();
            }

            if (!context.Transacoes.Any())
            {
                var ativoSelic2031 = context.Ativos.First(a => a.Codigo == "SELIC2031");

                context.Transacoes.AddRange(
                    new Transacao
                    {
                        AtivoId = ativoSelic2031.Id,
                        TipoTransacao = TransacaoTipo.Compra,
                        Quantidade = 0.41m,
                        PrecoUnitario = 17420.60m,
                        ValorTotal = 7142.446m,
                        Data = new DateTime(2025, 10, 07),
                        Observacoes = "Compra de SELIC2031"
                    },
                    new Transacao
                    {
                        AtivoId = ativoSelic2031.Id,
                        TipoTransacao = TransacaoTipo.Compra,
                        Quantidade = 0.06m,
                        PrecoUnitario = 17518.23m,
                        ValorTotal = 1051.0938m,
                        Data = new DateTime(2025, 10, 21),
                        Observacoes = "Compra de SELIC2031"
                    },
                    new Transacao
                    {
                        AtivoId = ativoSelic2031.Id,
                        TipoTransacao = TransacaoTipo.Compra,
                        Quantidade = 0.04m,
                        PrecoUnitario = 17655.03m,
                        ValorTotal = 706.2012m,
                        Data = new DateTime(2025, 11, 10),
                        Observacoes = "Compra de SELIC2031"
                    },
                    new Transacao
                    {
                        AtivoId = ativoSelic2031.Id,
                        TipoTransacao = TransacaoTipo.Compra,
                        Quantidade = 0.06m,
                        PrecoUnitario = 17734.17m,
                        ValorTotal = 1064.0502m,
                        Data = new DateTime(2025, 11, 21),
                        Observacoes = "Compra de SELIC2031"
                    },
                    new Transacao
                    {
                        AtivoId = ativoSelic2031.Id,
                        TipoTransacao = TransacaoTipo.Compra,
                        Quantidade = 0.04m,
                        PrecoUnitario = 17830.23m,
                        ValorTotal = 713.2092m,
                        Data = new DateTime(2025, 12, 05),
                        Observacoes = "Compra de SELIC2031"
                    }
                );
            }
        }
    }
}