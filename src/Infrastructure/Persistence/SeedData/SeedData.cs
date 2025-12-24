using Core.Entities;
using Core.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Persistence.SeedData
{
    public static class SeedData
    {
        public static async Task SeedAsync(LiberdadeDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {
            await SeedIdentityAsync(userManager, roleManager, configuration);
            SeedDomain(context);
        }

        private static async Task SeedIdentityAsync(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {
            // Seed Role
            if (!await roleManager.RoleExistsAsync("admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("admin"));
            }

            // Seed User
            if (!userManager.Users.Any())
            {
                var email = configuration["Admin:Username"] ?? "admin@liberdade.com";
                var password = configuration["Admin:Password"] ?? "Admin@123";

                var user = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "admin");
                }
                else
                {
                    throw new Exception($"Falha ao criar usurio admin: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }

        private static void SeedDomain(LiberdadeDbContext context)
        {
            if (!context.Ativos.Any())
            {
                context.Ativos.AddRange(
                    new Ativo { Codigo = "BRSTNCLF1RU6", Nome = "Tesouro Selic 2031", Categoria = AtivoCategoria.RendaFixaLiquidez, AtualizadoEm = DateTime.MinValue },
                    new Ativo { Codigo = "RBRR11", Nome = "FII RBR Rendimento High Grade", Categoria = AtivoCategoria.FiiPapel, AtualizadoEm = DateTime.MinValue },
                    new Ativo { Codigo = "CPTS11", Nome = "Capitania Securities II", Categoria = AtivoCategoria.FiiPapel, AtualizadoEm = DateTime.MinValue },
                    new Ativo { Codigo = "KNCR11", Nome = "Kinea Rendimentos Imobilirios", Categoria = AtivoCategoria.FiiPapel, AtualizadoEm = DateTime.MinValue },
                    new Ativo { Codigo = "XPML11", Nome = "XP Malls", Categoria = AtivoCategoria.FiiTijoloShopping, AtualizadoEm = DateTime.MinValue },
                    new Ativo { Codigo = "VISC11", Nome = "Vinci Shopping Centers", Categoria = AtivoCategoria.FiiTijoloShopping, AtualizadoEm = DateTime.MinValue },
                    new Ativo { Codigo = "HGLG11", Nome = "PATRIA LOG", Categoria = AtivoCategoria.FiiTijoloLogistica, AtualizadoEm = DateTime.MinValue },
                    new Ativo { Codigo = "XPLG11", Nome = "XP Log", Categoria = AtivoCategoria.FiiTijoloLogistica, AtualizadoEm = DateTime.MinValue }
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
                var ativoSelic2031 = context.Ativos.First(a => a.Codigo == "BRSTNCLF1RU6");

                var transacoes = new[]
                {
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
                    },
                    new Transacao
                    {
                        AtivoId = ativoSelic2031.Id,
                        TipoTransacao = TransacaoTipo.Compra,
                        Quantidade = 0.05m,
                        PrecoUnitario = 17942.34m,
                        ValorTotal = 897.11m,
                        Data = new DateTime(2025, 12, 22),
                        Observacoes = "Compra de SELIC2031"
                    }
                };

                foreach (var transacao in transacoes)
                {
                    context.Transacoes.Add(transacao);

                    var posicao = context.PosicaoCarteiras.Local.FirstOrDefault(p => p.AtivoId == transacao.AtivoId) 
                                  ?? context.PosicaoCarteiras.FirstOrDefault(p => p.AtivoId == transacao.AtivoId);

                    if (posicao == null)
                    {
                        posicao = new PosicaoCarteira
                        {
                            AtivoId = (Guid)transacao.AtivoId,
                            Codigo = ativoSelic2031.Codigo,
                            Categoria = ativoSelic2031.Categoria,
                            Quantidade = transacao.Quantidade,
                            PrecoMedio = transacao.PrecoUnitario,
                            PrecoAtual = ativoSelic2031.PrecoAtual
                        };
                        context.PosicaoCarteiras.Add(posicao);
                    }
                    else
                    {
                        var totalAtual = posicao.Quantidade * posicao.PrecoMedio;
                        var totalCompra = transacao.Quantidade * transacao.PrecoUnitario;
                        var novaQuantidade = posicao.Quantidade + transacao.Quantidade;

                        posicao.PrecoMedio = (totalAtual + totalCompra) / novaQuantidade;
                        posicao.Quantidade = novaQuantidade;
                    }
                }

                context.SaveChanges();
            }
        }
    }
}