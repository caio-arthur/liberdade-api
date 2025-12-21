using Application.Common.Interfaces;
using Application.Handlers.Notificacoes.Commands;
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
        private readonly TimeSpan _horarioAlvo = new TimeSpan(19, 00, 0);

        public DiarioFinanceiroWorker(
            IServiceProvider serviceProvider,
            ILogger<DiarioFinanceiroWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        // debug
        //protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        //{
        //    _logger.LogInformation("DiarioFinanceiroWorker iniciado em modo de TESTE (1 em 1 minuto).");

        //    while (!stoppingToken.IsCancellationRequested)
        //    {
        //        try
        //        {
        //            _logger.LogInformation($"Executando processamento às: {DateTime.Now}");

        //            // Removi a trava de final de semana para seus testes funcionarem agora
        //            await ProcessarRelatorioAsync(stoppingToken);

        //            _logger.LogInformation("Processamento concluído. Aguardando 1 minuto para o próximo ciclo...");
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "Erro ao processar fechamento.");
        //        }

        //        // Define o intervalo de 1 minuto
        //        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        //    }
        //}

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
                    if (DateTime.Now.DayOfWeek != DayOfWeek.Saturday && DateTime.Now.DayOfWeek != DayOfWeek.Sunday)
                    {
                        await ProcessarRelatorioAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar fechamento diário.");
                }
            }
        }



        private async Task ProcessarRelatorioAsync(CancellationToken token)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var aiService = scope.ServiceProvider.GetRequiredService<IAgenteFinanceiroService>();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();

                // 1. Busca o "Fato" do dia (Histórico já salvo pelo outro worker)
                var historicoHoje = await context.HistoricoPatrimonios
                    .Where(h => h.Data.Date == DateTime.UtcNow.Date)
                    .OrderByDescending(h => h.Data)
                    .FirstOrDefaultAsync(token);

                // Se não tem histórico hoje, o mercado não atualizou ou é feriado/fim de semana sem worker ativo
                if (historicoHoje == null)
                {
                    _logger.LogWarning("Nenhum histórico encontrado para hoje. Relatório adiado.");
                    return;
                }

                // 2. Busca o histórico de "Ontem" (ou o último disponível antes de hoje) para comparar
                var historicoAnterior = await context.HistoricoPatrimonios
                    .Where(h => h.Data.Date < DateTime.UtcNow.Date)
                    .OrderByDescending(h => h.Data)
                    .FirstOrDefaultAsync(token);

                decimal patrimonioOntem = historicoAnterior?.ValorTotal ?? historicoHoje.ValorTotal;
                decimal variacao = historicoHoje.ValorTotal - patrimonioOntem;

                // 3. Busca movimentações do dia (Aportes/Vendas) para dar contexto à IA
                // Se o patrimônio subiu 10k, foi valorização ou foi um aporte? A IA precisa saber.
                var movimentacoesHoje = await context.Transacoes
                    .Where(t => t.Data.Date == DateTime.UtcNow.Date)
                    .Select(t => $"{t.TipoTransacao}: {t.ValorTotal:C} ({t.Observacoes})")
                    .ToListAsync(token);

                // 4. Monta o DTO para a IA
                var contextoDTO = new ContextoFinanceiroDto(
                    NomeUsuario: "Caio", // Idealmente viria de um UserConfig
                    NomeConjuge: "Letícya",
                    PatrimonioTotal: historicoHoje.ValorTotal,
                    MetaRenda: 600m, // Pode buscar de uma tabela de Metas
                    RendaAtual: historicoHoje.RendaPassivaCalculada,
                    VariacaoDiaria: variacao,
                    FaseAtual: "Etapa 1 (Acumulação)",
                    UltimasMovimentacoes: movimentacoesHoje
                );

                // 5. Gera e Envia
                var mensagemIA = await aiService.GerarRelatorioDiarioAsync(contextoDTO);

                await sender.Send(new EnviarNotificacaoCommand(
                    "Fechamento Diário 🌙",
                    mensagemIA
                ), token);

                _logger.LogInformation("Relatório diário gerado e enviado com sucesso.");

            }
        }
    }
}