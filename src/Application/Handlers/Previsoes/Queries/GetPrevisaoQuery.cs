using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Wrappers;
using Application.Handlers.Feriados.Queries.ObterDiasUteisPorMes;
using Application.Handlers.Previsoes.Responses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

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
        private readonly string _codigoSelic;
        private readonly ISender _mediator;

        public GetPrevisaoQueryHandler(IApplicationDbContext context, IFeriadosNacionaisService feriados, IConfiguration config, ISender mediator)
        {
            _context = context;
            _feriados = feriados;
            _codigoSelic = config["BancoCentral:CodigoIsinSelic2031"] ?? "BRSTNCLF1RU6";
            _mediator = mediator;
        }

        public async Task<Response<PrevisaoRetornoDto>> Handle(GetPrevisaoQuery request, CancellationToken cancellationToken)
        {
            var posicoes = await _context.PosicaoCarteiras.ToListAsync(cancellationToken);
            decimal patrimonioTotal = posicoes.Sum(p => p.Quantidade * p.PrecoAtual);

            var ativosSelic = await _context.Ativos
                .Where(a => a.Codigo.Contains(_codigoSelic))
                .ToListAsync(cancellationToken);

            var ativoReferencia = ativosSelic
                .OrderByDescending(a => a.PercentualDeRetornoMensalEsperado)
                .FirstOrDefault();

            decimal taxaMensal = (ativoReferencia?.PercentualDeRetornoMensalEsperado ?? 0.85m) / 100;
            if (taxaMensal <= 0) taxaMensal = 0.0085m;

            var hoje = DateTime.Today;
            var dataInicio = hoje;

            var diasUteisQueryResult = await _mediator.Send(new ObterDiasUteisPorMesQuery() { Ano = hoje.Year, Mes = hoje.Month, Uf = "MG" }, cancellationToken);
            int diasUteisEsteMes = diasUteisQueryResult.Dados;

            int divisorDias = diasUteisEsteMes > 0 ? diasUteisEsteMes : 21;

            double taxaDiariaDouble = Math.Pow((double)(1 + taxaMensal), 1.0 / divisorDias) - 1;
            decimal taxaDiaria = (decimal)taxaDiariaDouble;

            var dataFimMesAtual = new DateTime(hoje.Year, hoje.Month, 1).AddMonths(1).AddDays(-1);

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

            DateTime dataSimulada = dataInicio.AddDays(1);

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

                    int diasCorridosReais = (dataSimulada - dataInicio).Days;

                    resposta.EvolucaoDiaria.Add(new EvolucaoPontoDto
                    {
                        DiasDecorridos = diasCorridosReais,
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