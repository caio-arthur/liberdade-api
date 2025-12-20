using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Wrappers;
using Application.Handlers.Previsoes.Responses;
using Core.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers.Previsoes.Queries
{
    public class GetPrevisaoQuery : IRequestWrapper<PrevisaoRetornoDto>
    {
        public decimal AporteMensal { get; set; } = 1500m;
        public decimal MetaRendaMensal { get; set; } = 450m;
    }

    public class GetPrevisaoQueryHandler : IRequestHandler<GetPrevisaoQuery, Response<PrevisaoRetornoDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IFeriadosNacionaisService _feriados;

        public GetPrevisaoQueryHandler(IApplicationDbContext context, IFeriadosNacionaisService feriados)
        {
            _context = context;
            _feriados = feriados;
        }

        public async Task<Response<PrevisaoRetornoDto>> Handle(GetPrevisaoQuery request, CancellationToken cancellationToken)
        {
            var posicoes = await _context.PosicaoCarteiras.ToListAsync(cancellationToken);
            decimal patrimonioTotal = posicoes.Sum(p => p.Quantidade * p.PrecoAtual);

            var ativosSelic = await _context.Ativos
                .Where(a => a.Codigo.Contains("SELIC"))
                .ToListAsync(cancellationToken);

            var ativoReferencia = ativosSelic
                .OrderByDescending(a => a.PercentualDeRetornoMensalEsperado)
                .FirstOrDefault();

            decimal taxaMensal = (ativoReferencia?.PercentualDeRetornoMensalEsperado ?? 0.85m) / 100;

            if (taxaMensal <= 0) taxaMensal = 0.0085m;

            double taxaDiariaDouble = Math.Pow((double)(1 + taxaMensal), 1.0 / 21.0) - 1;
            decimal taxaDiaria = (decimal)taxaDiariaDouble;

            var hoje = DateTime.Today;
            var dataInicio = hoje;

            if (dataInicio == DateTime.MinValue) dataInicio = hoje;

            var primeiroDiaDesteMes = new DateTime(dataInicio.Year, dataInicio.Month, 1);
            var dataFimMesAtual = primeiroDiaDesteMes.AddMonths(1).AddDays(-1);

            decimal patrimonioNecessario = request.MetaRendaMensal / taxaMensal;

            var resposta = new PrevisaoRetornoDto
            {
                PatrimonioAtual = patrimonioTotal,
                MetaRendaMensal = request.MetaRendaMensal,
                PatrimonioNecessario = Math.Round(patrimonioNecessario, 2),
                RendaPassivaAtual = Math.Round(patrimonioTotal * taxaMensal, 2),
                EvolucaoDiaria = new List<EvolucaoPontoDto>()
            };

            if (resposta.RendaPassivaAtual >= request.MetaRendaMensal)
            {
                resposta.MesesRestantes = 0;
                resposta.DataAtingimentoMeta = hoje;
                return Response.Success(resposta);
            }

            decimal saldoSimulado = patrimonioTotal;
            DateTime dataSimulada = dataInicio;
            int diasDecorridos = 0;

            var feriadosNacionais = await _feriados.GetFeriadosNacionaisPorEstadoUfEAno("MG", hoje.Year, cancellationToken);
            if (dataFimMesAtual.Year > hoje.Year)
            {
                var feriadosProximo = await _feriados.GetFeriadosNacionaisPorEstadoUfEAno("MG", dataFimMesAtual.Year, cancellationToken);
                feriadosNacionais.AddRange(feriadosProximo);
            }

            var feriadosSet = feriadosNacionais.Select(f => f.Data.Date).ToHashSet();

            while (dataSimulada <= dataFimMesAtual)
            {
                bool ehFimDeSemana = dataSimulada.DayOfWeek == DayOfWeek.Saturday ||
                                     dataSimulada.DayOfWeek == DayOfWeek.Sunday;
                bool ehFeriado = feriadosSet.Contains(dataSimulada.Date);
                bool ehDiaUtil = !ehFimDeSemana && !ehFeriado;

                if (ehDiaUtil)
                {
                    saldoSimulado += saldoSimulado * taxaDiaria;

                    resposta.EvolucaoDiaria.Add(new EvolucaoPontoDto
                    {
                        DiasDecorridos = diasDecorridos++,
                        Data = dataSimulada,
                        PatrimonioAcumulado = Math.Round(saldoSimulado, 2),
                        RendaMensalEstimada = Math.Round(saldoSimulado * taxaMensal, 2)
                    });
                }
                dataSimulada = dataSimulada.AddDays(1);
            }

            decimal saldoProjecao = saldoSimulado;
            int mesesProjecao = 0;

            int maxMeses = 1200;

            while (saldoProjecao * taxaMensal < request.MetaRendaMensal && mesesProjecao < maxMeses)
            {
                saldoProjecao += saldoProjecao * taxaMensal;

                saldoProjecao += request.AporteMensal;

                mesesProjecao++;
            }

            resposta.MesesRestantes = mesesProjecao;
            resposta.DataAtingimentoMeta = dataFimMesAtual.AddMonths(mesesProjecao);

            if (mesesProjecao >= maxMeses)
            {
                resposta.DataAtingimentoMeta = DateTime.MaxValue;
            }

            return Response.Success(resposta);
        }
    }
}