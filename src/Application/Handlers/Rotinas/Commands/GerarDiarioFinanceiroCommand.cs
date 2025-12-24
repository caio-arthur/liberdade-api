using Application.Common.Interfaces;
using Application.Handlers.Feriados.Queries.ObterDiasUteisPorMes;
using Application.Handlers.Notificacoes.Commands;
using Application.Handlers.Previsoes.Queries;
using Core.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Handlers.Rotinas.Commands
{
    public class GerarDiarioFinanceiroCommand : IRequest<Unit>
    {
    }

    public class GerarDiarioFinanceiroCommandHandler : IRequestHandler<GerarDiarioFinanceiroCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly IAgenteFinanceiroService _aiService;
        private readonly ISender _sender;
        private readonly ITranslationService _translationService;
        private readonly ILogger<GerarDiarioFinanceiroCommandHandler> _logger;

        public GerarDiarioFinanceiroCommandHandler(
            IApplicationDbContext context,
            IAgenteFinanceiroService aiService,
            ISender sender,
            ITranslationService translationService,
            ILogger<GerarDiarioFinanceiroCommandHandler> logger)
        {
            _context = context;
            _aiService = aiService;
            _sender = sender;
            _translationService = translationService;
            _logger = logger;
        }

        public async Task<Unit> Handle(GerarDiarioFinanceiroCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Iniciando geração de diário financeiro (Via Command)...");

            var historicoHoje = await _context.HistoricoPatrimonios
                .Where(h => h.Data.Date == DateTime.UtcNow.Date)
                .OrderByDescending(h => h.Data)
                .FirstOrDefaultAsync(cancellationToken);

            if (historicoHoje == null)
            {
                _logger.LogWarning("Nenhum histórico encontrado para hoje. Abortando relatório diário.");
                return Unit.Value;
            }

            var historicoAnterior = await _context.HistoricoPatrimonios
                .Where(h => h.Data.Date < DateTime.UtcNow.Date)
                .OrderByDescending(h => h.Data)
                .FirstOrDefaultAsync(cancellationToken);

            decimal patrimonioOntem = historicoAnterior?.ValorTotal ?? historicoHoje.ValorTotal;
            decimal variacaoPatrimonial = historicoHoje.ValorTotal - patrimonioOntem;

            var previsaoResponse = await _sender.Send(new GetPrevisaoQuery(), cancellationToken);
            var dadosPrevisao = previsaoResponse.Dados;

            var diasUteisResponse = await _sender.Send(new ObterDiasUteisPorMesQuery
            {
                Ano = DateTime.UtcNow.Year,
                Mes = DateTime.UtcNow.Month,
                Uf = "MG"
            }, cancellationToken);
            int diasUteisEsteMes = diasUteisResponse.Dados;

            decimal rendimentoPassivoDiario = diasUteisEsteMes > 0 ? dadosPrevisao.RendaPassivaAtual / diasUteisEsteMes : 0;
            var percentualMetaAtingido = dadosPrevisao.MetaRendaMensal > 0 ? (dadosPrevisao.RendaPassivaAtual / dadosPrevisao.MetaRendaMensal * 100) : 0;

            var movimentacoesHoje = await _context.Transacoes
                .Where(t => t.Data.Date == DateTime.UtcNow.Date)
                .Select(t => $"{t.TipoTransacao}: {t.ValorTotal:C} ({t.Observacoes})")
                .ToListAsync(cancellationToken);

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

            var mensagemIA = await _aiService.GerarRelatorioDiarioAsync(contextoDTO);

            var mensagemTraduzida = await _translationService.TranslateTextAsync(
                mensagemIA,
                targetLanguage: "pt",
                sourceLanguage: "en"
            );

            await _sender.Send(new EnviarNotificacaoCommand()
            {
                Title = ObterTituloAleatorio(),
                Message = mensagemTraduzida,
                Priority = NotificacaoPrioridade.Default,
                Tags = [ObterTagPorPercentualMeta(percentualMetaAtingido)]
            }, cancellationToken);

            _logger.LogInformation("Relatório diário gerado e enviado.");

            return Unit.Value;
        }

        private static string ObterTituloAleatorio()
        {
            var titulos = new List<string>
            {
                "Um dia mais perto",
                "Nosso progresso diário",
                "Atualização do nosso patrimônio",
                "Resumo financeiro",
                "Rumo à independência financeira",
                "Atualização da jornada",
                "Cada dia conta",
                "Resultado da nossa persistência",
                "Nada que vale a pena é fácil",
                "Nós caminhamos juntos",
                "Um dia após o outro",
                "Construindo nosso futuro",
                "Pela família que vamos construir",
                "Até alcançarmos nosso sonho"
            };
            var random = new Random();
            int index = random.Next(titulos.Count);
            return titulos[index];
        }

        private static string ObterTagPorPercentualMeta(decimal percentualMetaCumprida)
        {
            return percentualMetaCumprida switch
            {
                <= 20 => "seedling",
                <= 40 => "potted_plant",
                <= 60 => "palm_tree",
                <= 80 => "evergreen_tree",
                _ => "deciduous_tree",
            };
        }
    }
}

