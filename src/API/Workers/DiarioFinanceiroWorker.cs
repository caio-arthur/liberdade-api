using Application.Handlers.Feriados.Queries.EhDiaUtil;
using Application.Handlers.Rotinas.Commands;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace API.Workers
{
    public class DiarioFinanceiroWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DiarioFinanceiroWorker> _logger;

        // Configuração: 09:00
        private readonly TimeSpan _horarioAlvo = new(9, 0, 0);

        public DiarioFinanceiroWorker(
            IServiceProvider serviceProvider,
            ILogger<DiarioFinanceiroWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DiarioFinanceiroWorker iniciado.");

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
                    bool ehDiaUtil;
                    using var scope = _serviceProvider.CreateScope();
                    {
                        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
                        var ehDiaUtilResponse = await mediator.Send(new EhDiaUtilQuery { Data = DateTime.Today }, stoppingToken);
                        ehDiaUtil = ehDiaUtilResponse.Dados;
                    
                        if (ehDiaUtil)
                        {
                            await mediator.Send(new GerarDiarioFinanceiroCommand(), stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar fechamento diário.");
                }
            }
        }
    }
}