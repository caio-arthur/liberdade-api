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
    public class AtualizarMercadoWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AtualizarMercadoWorker> _logger;
        // Define o horário de execução (08:00)
        private readonly TimeSpan _horarioAlvo = new(8, 0, 0);

        public AtualizarMercadoWorker(IServiceProvider serviceProvider, ILogger<AtualizarMercadoWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

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

                        if (ehDiaUtil)
                        {
                            await mediator.Send(new AtualizarMercadoCommand(), stoppingToken);
                        }
                        else
                        {
                            _logger.LogInformation("Fim de semana detectado. Pulando atualização.");
                        }
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
    }
}