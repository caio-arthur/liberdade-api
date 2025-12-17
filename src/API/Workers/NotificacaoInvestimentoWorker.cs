using Application.Common.Wrappers;
using Application.Handlers.Notificacoes.Commands;
using Application.Handlers.Previsoes.Queries;
using Application.Handlers.Previsoes.Responses;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Workers
{
    public class NotificacaoInvestimentosWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<NotificacaoInvestimentosWorker> _logger;

        // Configurações do agendamento
        private readonly TimeSpan _horarioEnvio = new TimeSpan(18, 0, 0); // 18:00
        private const decimal _aporteMensalPadrao = 1500m;
        private const decimal _metaRendaPadrao = 450m; // Ajuste conforme necessário para o casal

        public NotificacaoInvestimentosWorker(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<NotificacaoInvestimentosWorker> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Serviço de Motivação Financeira iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var proximaExecucao = CalcularProximaExecucao();
                var tempoEspera = proximaExecucao - DateTime.Now;

                _logger.LogInformation($"Próxima notificação agendada para: {proximaExecucao}");

                // Aguarda até o horário agendado
                if (tempoEspera.TotalMilliseconds > 0)
                {
                    await Task.Delay(tempoEspera, stoppingToken);
                }

                try
                {
                    await ProcessarNotificacaoAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao enviar notificação de investimentos.");
                }
            }
        }

        private async Task ProcessarNotificacaoAsync(CancellationToken stoppingToken)
        {
            // Precisamos criar um escopo novo para resolver o MediatR e o DbContext
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                // 1. Obter os dados (Previsão)
                var query = new GetPrevisaoQuery
                {
                    AporteMensal = _aporteMensalPadrao,
                    MetaRendaMensal = _metaRendaPadrao
                };

                var response = await mediator.Send(query, stoppingToken);

                if (response.Sucesso && response.Dados != null)
                {
                    // 2. Gerar a mensagem motivacional
                    var (titulo, mensagem) = GerarMensagemMotivacional(response.Dados);

                    // 3. Enviar a notificação
                    var command = new EnviarNotificacaoCommand(titulo, mensagem, Core.Notifications.NotificacaoPrioridade.High);
                    await mediator.Send(command, stoppingToken);

                    _logger.LogInformation("Notificação de investimentos enviada com sucesso.");
                }
                else
                {
                    _logger.LogWarning("Não foi possível obter os dados da previsão para a notificação.");
                }
            }
        }

        private (string Titulo, string Mensagem) GerarMensagemMotivacional(PrevisaoRetornoDto dados)
        {
            var titulo = "🚀 Resumo do Casal Investidor";

            var sb = new StringBuilder();

            // Saudação
            sb.AppendLine("Olá! Passando para atualizar nosso progresso hoje. 🌱");
            sb.AppendLine();

            // Dados Atuais
            sb.AppendLine($"💰 **Patrimônio Atual:** {dados.PatrimonioAtual:C2}");
            sb.AppendLine($"📈 **Renda Passiva Já Garantida:** {dados.RendaPassivaAtual:C2}/mês");
            sb.AppendLine();

            // Motivação baseada na meta
            if (dados.MesesRestantes <= 0)
            {
                sb.AppendLine("🎉 **PARABÉNS!** A meta de renda mensal foi atingida! Vocês são incríveis!");
            }
            else
            {
                sb.AppendLine("Estamos no caminho certo! 💪");
                sb.AppendLine($"Faltam apenas **{dados.MesesRestantes} meses** para atingirmos nossa meta de {dados.MetaRendaMensal:C2}.");

                // Formatação amigável da data
                var dataMeta = dados.DataAtingimentoMeta.ToString("MMMM 'de' yyyy");
                sb.AppendLine($"📅 Previsão de liberdade: **{dataMeta}**");
            }

            sb.AppendLine();
            sb.AppendLine("_'O segredo do sucesso é a constância no objetivo.'_ Continuem firmes nos aportes! 🚀");

            return (titulo, sb.ToString());
        }

        private DateTime CalcularProximaExecucao()
        {
            var agora = DateTime.Now;
            var hojeAs18 = agora.Date.Add(_horarioEnvio);

            DateTime proximaData;

            // Se já passou das 18h hoje, tenta amanhã
            if (agora > hojeAs18)
            {
                proximaData = hojeAs18.AddDays(1);
            }
            else
            {
                proximaData = hojeAs18;
            }

            // Garante que seja dia útil (Segunda a Sexta)
            while (proximaData.DayOfWeek == DayOfWeek.Saturday || proximaData.DayOfWeek == DayOfWeek.Sunday)
            {
                proximaData = proximaData.AddDays(1);
            }

            return proximaData;
        }
    }
}