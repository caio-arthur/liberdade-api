using Application.Common.Interfaces;
using Core.Entities;
using Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace API.Workers
{
    public class AtualizarMercadoWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AtualizarMercadoWorker> _logger;
        private readonly TimeSpan _horarioAlvo = new TimeSpan(18, 0, 0);

        public AtualizarMercadoWorker(IServiceProvider serviceProvider, ILogger<AtualizarMercadoWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var agora = DateTime.Now;
                var proximaExecucao = agora.Date.Add(_horarioAlvo);

                if (agora > proximaExecucao)
                    proximaExecucao = proximaExecucao.AddDays(1);

                var tempoEspera = proximaExecucao - agora;
                _logger.LogInformation($"Próxima execução do relatório diário em: {tempoEspera}");

                await Task.Delay(tempoEspera, stoppingToken);

                if (stoppingToken.IsCancellationRequested) break;

                try
                {
                    if (DateTime.Now.DayOfWeek != DayOfWeek.Saturday && DateTime.Now.DayOfWeek != DayOfWeek.Sunday)
                    {
                        await ProcessarAtualizacaoAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar fechamento diário.");
                }
            }
        }

        private async Task ProcessarAtualizacaoAsync(CancellationToken cancellationToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var marketService = scope.ServiceProvider.GetRequiredService<IDadosMercadoService>();

                // 1. Carregamento inicial de dados (Ativos e Posições)
                var ativos = await context.Ativos.ToListAsync(cancellationToken);
                var posicoesCarteira = await context.PosicaoCarteiras.ToListAsync(cancellationToken);

                var taxaSelicMensalEstimada = await marketService.ObterTaxaSelicAtualAsync();

                // Flag para saber se precisaremos salvar algo no final
                bool precisaSalvar = false;

                // --- ETAPA 1: ATUALIZAÇÃO DE PREÇOS DOS ATIVOS ---
                foreach (var ativo in ativos)
                {
                    if (ativo.AtualizadoEm.Date >= DateTime.UtcNow.Date) continue;

                    bool deveAtualizarAtivo = false;
                    bool ehSelic = ativo.Codigo.ToUpper().Contains("SELIC");
                    bool ehFii = ativo.Categoria.ToString().StartsWith("Fii"); // Cuidado com string mágica, prefira Enums se possível

                    decimal? novoPreco = null;

                    if (ehSelic)
                    {
                        novoPreco = await marketService.ObterPrecoTesouroDiretoAsync("Tesouro-Selic-2031");

                        if (taxaSelicMensalEstimada.HasValue)
                        {
                            ativo.PercentualDeRetornoMensalEsperado = taxaSelicMensalEstimada.Value;

                            if (novoPreco.HasValue)
                                ativo.RendimentoValorMesAnterior = novoPreco.Value * (taxaSelicMensalEstimada.Value / 100m); // Ajustei divisor para 100m (assumindo percentual padrão), verifique sua lógica do 2100m.

                            deveAtualizarAtivo = true;
                        }
                    }
                    else if (ehFii)
                    {
                        var dadosFii = await marketService.ObterDadosFiiAsync(ativo.Codigo);
                        if (dadosFii.HasValue)
                        {
                            novoPreco = dadosFii.Value.Preco;
                            ativo.RendimentoValorMesAnterior = dadosFii.Value.UltimoRendimento;

                            if (novoPreco > 0)
                                ativo.PercentualDeRetornoMensalEsperado = (ativo.RendimentoValorMesAnterior / novoPreco.Value) * 100;

                            deveAtualizarAtivo = true;
                        }
                    }

                    if (novoPreco.HasValue && novoPreco.Value > 0)
                    {
                        ativo.PrecoAtual = novoPreco.Value;
                        deveAtualizarAtivo = true;
                    }

                    if (deveAtualizarAtivo)
                    {
                        ativo.AtualizadoEm = DateTime.UtcNow;
                        precisaSalvar = true;
                    }
                }

                // --- ETAPA 2: ATUALIZAÇÃO DAS POSIÇÕES DA CARTEIRA ---
                // Cria dicionário em memória (não vai no banco de novo) para busca rápida
                var ativosDict = ativos.ToDictionary(a => a.Codigo, a => a);

                if (precisaSalvar) // Só atualiza carteira se houve mudança nos ativos
                {
                    foreach (var posicao in posicoesCarteira)
                    {
                        if (ativosDict.TryGetValue(posicao.Codigo, out var ativoAtualizado))
                        {
                            posicao.PrecoAtual = ativoAtualizado.PrecoAtual;
                        }
                    }
                }

                // --- ETAPA 3: GERAÇÃO DE HISTÓRICO DIÁRIO ---
                bool historicoExiste = await context.HistoricoPatrimonios
                    .AnyAsync(h => h.Data.Date == DateTime.UtcNow.Date, cancellationToken);

                if (!historicoExiste)
                {
                    decimal patrimonioTotal = posicoesCarteira.Sum(p => p.Quantidade * p.PrecoAtual);
                    decimal rendaPassivaEstimada = 0;

                    foreach (var pos in posicoesCarteira)
                    {
                        if (ativosDict.TryGetValue(pos.Codigo, out var ativoRef))
                        {
                            if (ativoRef.Codigo.Contains("SELIC") || ativoRef.Categoria == AtivoCategoria.RendaFixaLiquidez)
                            {
                                decimal valorAlocadoHoje = pos.Quantidade * pos.PrecoAtual;
                                decimal taxaMensal = ativoRef.PercentualDeRetornoMensalEsperado / 100m;
                                rendaPassivaEstimada += valorAlocadoHoje * taxaMensal;
                            }
                            else
                            {
                                rendaPassivaEstimada += pos.Quantidade * ativoRef.RendimentoValorMesAnterior;
                            }
                        }
                    }

                    var historico = new HistoricoPatrimonio
                    {
                        Id = Guid.NewGuid(),
                        Data = DateTime.UtcNow,
                        ValorTotal = patrimonioTotal,
                        RendaPassivaCalculada = rendaPassivaEstimada
                    };

                    context.HistoricoPatrimonios.Add(historico);

                    _logger.LogInformation("Histórico diário gerado. Patrimônio: {Total} | Renda Est.: {Renda}", patrimonioTotal, rendaPassivaEstimada);

                    precisaSalvar = true;
                }

                if (precisaSalvar)
                {
                    await context.SaveChangesAsync(cancellationToken);
                }
            }
        }
    }
}