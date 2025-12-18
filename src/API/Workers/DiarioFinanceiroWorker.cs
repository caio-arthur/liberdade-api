using Application.Common.Interfaces;
using Application.Handlers.Notificacoes.Commands;
using Application.Handlers.Previsoes.Queries; 
using Core.Entities;
using MediatR;
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
    public class DiarioFinanceiroWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DiarioFinanceiroWorker> _logger;

        // Configuração: 18:00
        private readonly TimeSpan _horarioAlvo = new TimeSpan(18, 0, 0);

        public DiarioFinanceiroWorker(
            IServiceProvider serviceProvider,
            ILogger<DiarioFinanceiroWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        //protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        //{
        //    _logger.LogInformation("DiarioFinanceiroWorker iniciado.");

        //    while (!stoppingToken.IsCancellationRequested)
        //    {
        //        var agora = DateTime.Now;
        //        var proximaExecucao = agora.Date.Add(_horarioAlvo);

        //        if (agora > proximaExecucao)
        //            proximaExecucao = proximaExecucao.AddDays(1);

        //        var tempoEspera = proximaExecucao - agora;
        //        _logger.LogInformation($"Próxima execução do relatório diário em: {tempoEspera}");

        //        await Task.Delay(tempoEspera, stoppingToken);

        //        if (stoppingToken.IsCancellationRequested) break;

        //        try
        //        {
        //            if (DateTime.Now.DayOfWeek != DayOfWeek.Saturday && DateTime.Now.DayOfWeek != DayOfWeek.Sunday)
        //            {
        //                await ProcessarFechamentoDiario(stoppingToken);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "Erro ao processar fechamento diário.");
        //        }
        //    }
        //}

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DiarioFinanceiroWorker iniciado em modo de TESTE (1 em 1 minuto).");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation($"Executando processamento às: {DateTime.Now}");

                    // Removi a trava de final de semana para seus testes funcionarem agora
                    await ProcessarFechamentoDiario(stoppingToken);

                    _logger.LogInformation("Processamento concluído. Aguardando 1 minuto para o próximo ciclo...");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar fechamento.");
                }

                // Define o intervalo de 1 minuto
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task ProcessarFechamentoDiario(CancellationToken token)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var aiService = scope.ServiceProvider.GetRequiredService<IAgenteFinanceiroService>();
                var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                var previsaoQuery = new GetPrevisaoQuery
                {
                    AporteMensal = 1500m, // Pode vir de config
                    MetaRendaMensal = 600m
                };

                var resultadoPrevisao = await sender.Send(previsaoQuery, token);
                var dadosAtuais = resultadoPrevisao.Dados; 

                if (dadosAtuais == null) return;

                var ultimoHistorico = await dbContext.HistoricoPatrimonios
                    .OrderByDescending(h => h.Data)
                    .FirstOrDefaultAsync(token);

                decimal patrimonioOntem = ultimoHistorico?.ValorTotal ?? dadosAtuais.PatrimonioAtual;
                decimal variacao = dadosAtuais.PatrimonioAtual - patrimonioOntem;

                var movimentacoesHoje = await dbContext.Transacoes
                    .Where(t => t.Data.Date == DateTime.Today)
                    .Select(t => $"{t.TipoTransacao}: {t.ValorTotal:C}")
                    .ToListAsync(token);

                var contextoDTO = new ContextoFinanceiroDto(
                    NomeUsuario: "Caio",
                    NomeConjuge: "Letícya",
                    PatrimonioTotal: dadosAtuais.PatrimonioAtual,
                    MetaRenda: previsaoQuery.MetaRendaMensal,
                    RendaAtual: dadosAtuais.RendaPassivaAtual,
                    VariacaoDiaria: variacao,
                    FaseAtual: "Etapa 1 (Acumulação)",
                    UltimasMovimentacoes: movimentacoesHoje
                );

                var mensagemIA = await aiService.GerarRelatorioDiarioAsync(contextoDTO);

                await sender.Send(new EnviarNotificacaoCommand(
                    "Fechamento Diário 🌙",
                    mensagemIA
                ), token);

                var novoHistorico = new HistoricoPatrimonio
                {
                    Data = DateTime.UtcNow,
                    ValorTotal = dadosAtuais.PatrimonioAtual,
                    RendaPassivaCalculada = dadosAtuais.RendaPassivaAtual
                };

                dbContext.HistoricoPatrimonios.Add(novoHistorico);
                await dbContext.SaveChangesAsync(token);

                _logger.LogInformation("Fechamento diário concluído e histórico salvo.");
            }
        }
    }
}