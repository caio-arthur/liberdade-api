using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
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
        private readonly TimeSpan _periodoAtualizacao = TimeSpan.FromHours(12); // Roda a cada 12h

        public AtualizarMercadoWorker(IServiceProvider serviceProvider, ILogger<AtualizarMercadoWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AtualizarMercadoWorker iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await AtualizarAtivosAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha crítica no Worker de atualização.");
                }

                await Task.Delay(_periodoAtualizacao, stoppingToken);
            }
        }

        private async Task AtualizarAtivosAsync(CancellationToken cancellationToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var marketService = scope.ServiceProvider.GetRequiredService<IDadosMercadoService>();
                var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                var ativos = await dbContext.Ativos.ToListAsync();

                var taxaSelicMensal = await marketService.ObterTaxaSelicAtualAsync();

                foreach (var ativo in ativos)
                {
                    bool houveAtualizacao = false;

                    if (ativo.Codigo.Contains("SELIC", StringComparison.OrdinalIgnoreCase))
                    {
                        var precoTesouro = await marketService.ObterPrecoTesouroDiretoAsync("Tesouro-Selic-2031");

                        if (precoTesouro.HasValue)
                        {
                            ativo.PrecoAtual = precoTesouro.Value; 
                            houveAtualizacao = true;
                        }

                        if (taxaSelicMensal.HasValue) 
                        {
                            ativo.PercentualDeRetornoMensalEsperado = taxaSelicMensal.Value;

                            ativo.RendimentoValorMesAnterior = ativo.PrecoAtual * (taxaSelicMensal.Value / 100m);

                            houveAtualizacao = true;
                        }
                    }
                    else if (ativo.Categoria.ToString().StartsWith("Fii")) 
                    {
                        var dadosFii = await marketService.ObterDadosFiiAsync(ativo.Codigo); 

                        if (dadosFii.HasValue)
                        {
                            ativo.PrecoAtual = dadosFii.Value.Preco;
                            ativo.RendimentoValorMesAnterior = dadosFii.Value.UltimoRendimento;

                            if (ativo.PrecoAtual > 0)
                            {
                                ativo.PercentualDeRetornoMensalEsperado = (ativo.RendimentoValorMesAnterior / ativo.PrecoAtual) * 100;
                            }

                            houveAtualizacao = true;
                        }
                    }

                    if (houveAtualizacao)
                    {
                        ativo.AtualizadoEm = DateTime.UtcNow;
                    }
                }

                await dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Cotações atualizadas com sucesso em {Data}", DateTime.Now);
            }
        }
    }
}

