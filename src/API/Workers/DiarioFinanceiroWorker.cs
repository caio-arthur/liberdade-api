using Application.Common.Interfaces;
using Application.Handlers.Feriados.Queries.EhDiaUtil;
using Application.Handlers.Feriados.Queries.ObterDiasUteisPorMes;
using Application.Handlers.Notificacoes.Commands;
using Application.Handlers.Previsoes.Queries;
using Core.Notifications;
using Google.Cloud.Translation.V2;
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
        private readonly TranslationClient _translationClient;

        // Configuração: 09:00
        private readonly TimeSpan _horarioAlvo = new(9, 0, 0);

        public DiarioFinanceiroWorker(
            IServiceProvider serviceProvider,
            ILogger<DiarioFinanceiroWorker> logger,
            TranslationClient translationClient)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _translationClient = translationClient;

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
                    }

                    if (ehDiaUtil)
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


        private async Task ProcessarRelatorioAsync(CancellationToken token)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var aiService = scope.ServiceProvider.GetRequiredService<IAgenteFinanceiroService>();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            var historicoHoje = await context.HistoricoPatrimonios
                .Where(h => h.Data.Date == DateTime.UtcNow.Date)
                .OrderByDescending(h => h.Data)
                .FirstOrDefaultAsync(token);

            if (historicoHoje == null) return;

            var historicoAnterior = await context.HistoricoPatrimonios
                .Where(h => h.Data.Date < DateTime.UtcNow.Date)
                .OrderByDescending(h => h.Data)
                .FirstOrDefaultAsync(token);

            decimal patrimonioOntem = historicoAnterior?.ValorTotal ?? historicoHoje.ValorTotal;
            decimal variacaoPatrimonial = historicoHoje.ValorTotal - patrimonioOntem;

            var previsaoResponse = await sender.Send(new GetPrevisaoQuery(), token);
            var dadosPrevisao = previsaoResponse.Dados;

            int diasUteisEsteMes = sender.Send(new ObterDiasUteisPorMesQuery
            {
                Ano = DateTime.UtcNow.Year,
                Mes = DateTime.UtcNow.Month,
                Uf = "MG"
            }, token).Result.Dados;

            decimal rendimentoPassivoDiario = dadosPrevisao.RendaPassivaAtual / diasUteisEsteMes;
            var percentualMetaAtingido = dadosPrevisao.RendaPassivaAtual / dadosPrevisao.MetaRendaMensal * 100;

            var movimentacoesHoje = await context.Transacoes
                .Where(t => t.Data.Date == DateTime.UtcNow.Date)
                .Select(t => $"{t.TipoTransacao}: {t.ValorTotal:C} ({t.Observacoes})")
                .ToListAsync(token);

            // 4. Monta o Contexto Enriquecido
            var contextoDTO = new ContextoFinanceiroDto(
                NomeUsuario: "Caio",
                NomeConjuge: "Letícya",
                PatrimonioTotal: historicoHoje.ValorTotal,
                MetaRenda: dadosPrevisao.MetaRendaMensal,
                RendaAtual: dadosPrevisao.RendaPassivaAtual,
                VariacaoPatrimonialDiaria: variacaoPatrimonial,
                RendimentoPassivoDiario: rendimentoPassivoDiario, // O valor calculado
                PercentualMetaAtingido: percentualMetaAtingido,
                FaseAtual: "Etapa 1 (Acumulação)",
                MesesRestantes: dadosPrevisao.MesesRestantes,     // Vindo da Query
                DataEstimadaMeta: dadosPrevisao.DataAtingimentoMeta, // Vindo da Query
                UltimasMovimentacoes: movimentacoesHoje
            );

            var mensagemIA = await aiService.GerarRelatorioDiarioAsync(contextoDTO);

            var mensagemTraduzida = _translationClient.TranslateText(
                mensagemIA,
                targetLanguage: "pt",
                sourceLanguage: "en"
            ).TranslatedText;

            await sender.Send(new EnviarNotificacaoCommand()
            {
                Title = "Bom dia amor",
                Message = mensagemTraduzida,
                Priority = NotificacaoPrioridade.Default,
                Tags = ["seedling"]
            }, token);

            _logger.LogInformation("Relatório diário gerado e enviado.");
        }
    }
}