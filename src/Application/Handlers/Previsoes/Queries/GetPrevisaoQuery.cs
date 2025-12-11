using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Wrappers;
using Application.Handlers.Previsoes.Responses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers.Previsoes.Queries
{
    public class GetPrevisaoQuery : IRequestWrapper<PrevisaoRetornoDto>
    {
        public decimal AporteMensal { get; set; } = 1500m; 
        public decimal MetaRendaMensal { get; set; } = 600m;
    }

    public class GetPrevisaoQueryHandler : IRequestHandler<GetPrevisaoQuery, Response<PrevisaoRetornoDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetPrevisaoQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Response<PrevisaoRetornoDto>> Handle(GetPrevisaoQuery request, CancellationToken cancellationToken)
        {
            var ativos = await _context.PosicaoCarteiras.ToListAsync(cancellationToken);

            decimal patrimonioTotal = ativos.Sum(a => a.Quantidade * a.PrecoAtual);


            var ativosList = await _context.Ativos
                .Where(a => a.Codigo.Contains("SELIC"))
                .ToListAsync(cancellationToken);

            var ativoReferencia = ativosList
                .OrderByDescending(a => a.PercentualDeRetornoMensalEsperado)
                .FirstOrDefault();

            decimal taxaMensal = (ativoReferencia?.PercentualDeRetornoMensalEsperado ?? 0.85m) / 100;

            var resposta = new PrevisaoRetornoDto
            {
                PatrimonioAtual = patrimonioTotal,
                MetaRendaMensal = request.MetaRendaMensal,
                RendaPassivaAtual = patrimonioTotal * taxaMensal
            };

            if (resposta.RendaPassivaAtual >= request.MetaRendaMensal)
            {
                resposta.DataAtingimentoMeta = DateTime.Today;
                resposta.MesesRestantes = 0;
                return Response.Success(resposta);
            }

            decimal saldoSimulado = patrimonioTotal;
            DateTime dataSimulada = DateTime.Today;
            int meses = 0;

            while (saldoSimulado * taxaMensal < request.MetaRendaMensal && meses < 600)
            {
                meses++;
                dataSimulada = dataSimulada.AddMonths(1);

                decimal rendimento = saldoSimulado * taxaMensal;

                saldoSimulado += rendimento + request.AporteMensal;

                resposta.EvolucaoMensal.Add(new EvolucaoMesDto
                {
                    MesNumero = meses,
                    Data = dataSimulada,
                    PatrimonioAcumulado = Math.Round(saldoSimulado, 2),
                    RendaGerada = Math.Round(saldoSimulado * taxaMensal, 2)
                });
            }

            resposta.MesesRestantes = meses;
            resposta.DataAtingimentoMeta = dataSimulada;
            resposta.PatrimonioNecessario = saldoSimulado; 

            return Response.Success(resposta);
        }
    }
}
