using Application.Common.Interfaces;
using Core.Entities;
using Core.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Handlers.Rotinas.Commands
{
    public class AtualizarMercadoCommand : IRequest<Unit>
    {
    }

    public class AtualizarMercadoCommandHandler : IRequestHandler<AtualizarMercadoCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly IDadosMercadoService _marketService;
        private readonly IConfiguration _config;
        private readonly ILogger<AtualizarMercadoCommandHandler> _logger;
        private readonly string _codigoSelic;

        public AtualizarMercadoCommandHandler(
            IApplicationDbContext context,
            IDadosMercadoService marketService,
            IConfiguration config,
            ILogger<AtualizarMercadoCommandHandler> logger)
        {
            _context = context;
            _marketService = marketService;
            _config = config;
            _logger = logger;
            _codigoSelic = config["BancoCentral:CodigoIsinSelic2031"] ?? "BRSTNCLF1RU6";
        }

        public async Task<Unit> Handle(AtualizarMercadoCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Iniciando processamento de atualização de ativos (Via Command)...");

            var ativos = await _context.Ativos.ToListAsync(cancellationToken);
            var posicoesCarteira = await _context.PosicaoCarteiras.ToListAsync(cancellationToken);

            var taxaSelicMensalEstimada = await _marketService.ObterTaxaSelicAtualAsync();

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
                    var (precoTesouro, dataTesouro) = await _marketService.ObterPrecoTesouroDiretoBcbAsync(ativo.Codigo, dataAtual.Year, dataAtual.Month);

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
                //else if (ehFii)
                //{
                //    var dadosFii = await _marketService.ObterDadosFiiAsync(ativo.Codigo);
                //    if (dadosFii.HasValue)
                //    {
                //        novoPreco = dadosFii.Value.Preco;
                //        dataReferenciaPreco = DateTime.UtcNow;

                //        ativo.RendimentoValorMesAnterior = dadosFii.Value.UltimoRendimento;
                //        precisaSalvar = true;

                //        if (novoPreco > 0)
                //            ativo.PercentualDeRetornoMensalEsperado = (ativo.RendimentoValorMesAnterior / novoPreco.Value) * 100;
                //    }
                //}

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
                await _context.SaveChangesAsync(cancellationToken);
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
                    }
                }
            }

            // --- ETAPA 3: GERAÇÃO DE HISTÓRICO DIÁRIO ---
            bool historicoExiste = await _context.HistoricoPatrimonios
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

                _context.HistoricoPatrimonios.Add(historico);
                precisaSalvar = true;

                _logger.LogInformation("Histórico diário gerado. Patrimônio: {Total:C} | Renda Est.: {Renda:C}", patrimonioTotal, rendaPassivaEstimada);
            }
            else
            {
                _logger.LogInformation("Histórico para a data de hoje já existe.");
            }

            if (precisaSalvar)
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Ciclo de atualização concluído com sucesso.");
            }

            return Unit.Value;
        }
    }
}
