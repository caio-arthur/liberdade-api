using Application.Common.Interfaces;
using Application.Handlers.Feriados.Queries.EhDiaUtil;
using Core.Entities;
using Core.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        private readonly string _codigoSelic;
        private readonly ISender _mediator;
        // Define o horário de execução (08:00)
        private readonly TimeSpan _horarioAlvo = new(8, 0, 0);

        public AtualizarMercadoWorker(IServiceProvider serviceProvider, ILogger<AtualizarMercadoWorker> logger, IConfiguration config, ISender mediator)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _codigoSelic = config["BancoCentral:CodigoIsinSelic2031"] ?? "BRSTNCLF1RU6";
            _mediator = mediator;
        }

        // debug
        //protected override Task ExecuteAsync(CancellationToken stoppingToken)
        //{
        //    // Executa a atualização uma vez ao iniciar o serviço
        //    return ProcessarAtualizacaoAsync(stoppingToken);
        //}


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker de Atualização de Mercado iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var agora = DateTime.Now;
                    var proximaExecucao = agora.Date.Add(_horarioAlvo);

                    // Se já passou das 18h hoje, agenda para amanhã
                    if (agora > proximaExecucao)
                        proximaExecucao = proximaExecucao.AddDays(1);

                    var tempoEspera = proximaExecucao - agora;
                    _logger.LogInformation("Próxima execução agendada para: {Data} (Espera: {Tempo})", proximaExecucao, tempoEspera);

                    // Aguarda até o horário agendado
                    await Task.Delay(tempoEspera, stoppingToken);

                    if (stoppingToken.IsCancellationRequested) break;

                    // Verifica se é dia útil antes de rodar a lógica pesada
                    bool ehDiaUtil;
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
                        var ehDiaUtilResponse = await mediator.Send(new EhDiaUtilQuery { Data = DateTime.Today }, stoppingToken);
                        ehDiaUtil = ehDiaUtilResponse.Dados;
                    }

                    if (ehDiaUtil)
                    {
                        await ProcessarAtualizacaoAsync(stoppingToken);
                    }
                    else
                    {
                        _logger.LogInformation("Fim de semana detectado. Pulando atualização.");
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro crítico no loop do Worker.");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }

        private async Task ProcessarAtualizacaoAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Iniciando processamento de atualização de ativos...");

            using var scope = _serviceProvider.CreateScope();
            {
                var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var marketService = scope.ServiceProvider.GetRequiredService<IDadosMercadoService>();

                var ativos = await context.Ativos.ToListAsync(cancellationToken);
                var posicoesCarteira = await context.PosicaoCarteiras.ToListAsync(cancellationToken);

                var taxaSelicMensalEstimada = await marketService.ObterTaxaSelicAtualAsync();

                bool precisaSalvar = false;
                var dataAtual = DateTime.Now;

                // --- ETAPA 1: ATUALIZAÇÃO DE PREÇOS DOS ATIVOS ---
                foreach (var ativo in ativos)
                {
                    if (ativo.AtualizadoEm.Date >= DateTime.UtcNow.Date) continue;

                    bool ehSelic = ativo.Codigo.ToUpper().Contains(_codigoSelic) || ativo.Categoria == AtivoCategoria.RendaFixaLiquidez;
                    bool ehFii = ativo.Categoria.ToString().StartsWith("Fii");

                    decimal? novoPreco = null;
                    DateTime? dataReferenciaPreco = null;

                    if (ehSelic)
                    {
                        var (precoTesouro, dataTesouro) = await marketService.ObterPrecoTesouroDiretoBcbAsync(ativo.Codigo, dataAtual.Year, dataAtual.Month);

                        novoPreco = precoTesouro;
                        dataReferenciaPreco = dataTesouro;

                        if (taxaSelicMensalEstimada.HasValue)
                        {
                            ativo.PercentualDeRetornoMensalEsperado = taxaSelicMensalEstimada.Value;
                            precisaSalvar = true;

                            if (novoPreco.HasValue)
                            {
                                // Cálculo da Renda ($) = Valor investido * (Taxa% / 100)
                                ativo.RendimentoValorMesAnterior = novoPreco.Value * (taxaSelicMensalEstimada.Value / 100m);
                            }
                        }
                    }
                    else if (ehFii)
                    {
                        var dadosFii = await marketService.ObterDadosFiiAsync(ativo.Codigo);
                        if (dadosFii.HasValue)
                        {
                            novoPreco = dadosFii.Value.Preco;
                            dataReferenciaPreco = DateTime.UtcNow;

                            ativo.RendimentoValorMesAnterior = dadosFii.Value.UltimoRendimento;
                            precisaSalvar = true;

                            if (novoPreco > 0)
                                ativo.PercentualDeRetornoMensalEsperado = (ativo.RendimentoValorMesAnterior / novoPreco.Value) * 100;
                        }
                    }

                    // Se encontrou preço novo, atualiza
                    if (novoPreco.HasValue && novoPreco.Value > 0)
                    {
                        ativo.PrecoAtual = novoPreco.Value;
                        ativo.AtualizadoEm = dataReferenciaPreco ?? DateTime.UtcNow;

                        precisaSalvar = true;
                        _logger.LogInformation("Ativo {Codigo} atualizado para R$ {Preco} com data base {Data}", ativo.Codigo, ativo.PrecoAtual, ativo.AtualizadoEm);
                    }
                }

                if (precisaSalvar)
                {
                    await context.SaveChangesAsync(cancellationToken);
                }

                // --- ETAPA 2: ATUALIZAÇÃO DAS POSIÇÕES DA CARTEIRA ---
                var ativosDict = ativos.ToDictionary(a => a.Codigo, a => a);

                if (precisaSalvar)
                {
                    foreach (var posicao in posicoesCarteira)
                    {
                        if (ativosDict.TryGetValue(posicao.Codigo, out var ativoAtualizado))
                        {
                            posicao.PrecoAtual = ativoAtualizado.PrecoAtual;
                            posicao.PrecoMedio = ativoAtualizado.PrecoAtual;
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
                            if (ativoRef.Codigo.ToUpper().Contains(_codigoSelic) || ativoRef.Categoria == AtivoCategoria.RendaFixaLiquidez)
                            {
                                decimal valorAlocado = pos.Quantidade * pos.PrecoAtual;
                                decimal taxaDecimal = ativoRef.PercentualDeRetornoMensalEsperado / 100m;
                                rendaPassivaEstimada += valorAlocado * taxaDecimal;
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
                        Data = DateTime.UtcNow, // Snapshot do momento
                        ValorTotal = patrimonioTotal,
                        RendaPassivaCalculada = rendaPassivaEstimada
                    };

                    context.HistoricoPatrimonios.Add(historico);
                    precisaSalvar = true;

                    _logger.LogInformation("Histórico diário gerado. Patrimônio: {Total:C} | Renda Est.: {Renda:C}", patrimonioTotal, rendaPassivaEstimada);
                }
                else
                {
                    _logger.LogInformation("Histórico para a data de hoje já existe.");
                }

                if (precisaSalvar)
                {
                    await context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Ciclo de atualização concluído com sucesso.");
                }
            }
        }
    }
}