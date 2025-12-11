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
        public decimal AporteMensal { get; set; } = 1500m; // Valor padrão do seu contexto
        public decimal MetaRendaMensal { get; set; } = 600m; // Valor padrão do seu aluguel
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
            // 1. Obter Posição Atual (Patrimônio já alocado)
            var ativos = await _context.PosicaoCarteiras.ToListAsync(cancellationToken);

            decimal patrimonioTotal = ativos.Sum(a => a.Quantidade * a.PrecoAtual);

            // 2. Calcular Taxa Média Ponderada da Carteira
            // Se 90% é Selic (1% a.m.) e 10% é Caixa (0%), a média é 0.9%.
            // Para simplificar na Fase 1 (Acumulação), vamos pegar a taxa do ATIVO PRINCIPAL (Selic) 
            // ou fazer uma média ponderada dos ativos de renda fixa.

            // Buscamos a taxa Selic que o Worker atualizou no banco
            var ativoReferencia = await _context.Ativos
                .Where(a => a.Codigo.Contains("SELIC"))
                .OrderByDescending(a => a.PercentualDeRetornoMensalEsperado)
                .FirstOrDefaultAsync(cancellationToken);

            // Se não tiver dado, assumimos conservadoramente 0.85% a.m. (~10.5% a.a.)
            decimal taxaMensal = (ativoReferencia?.PercentualDeRetornoMensalEsperado ?? 0.85m) / 100;

            // 3. Setup da Projeção
            var resposta = new PrevisaoRetornoDto
            {
                PatrimonioAtual = patrimonioTotal,
                MetaRendaMensal = request.MetaRendaMensal,
                RendaPassivaAtual = patrimonioTotal * taxaMensal
            };

            // Se já atingiu a meta, retorna agora
            if (resposta.RendaPassivaAtual >= request.MetaRendaMensal)
            {
                resposta.DataAtingimentoMeta = DateTime.Today;
                resposta.MesesRestantes = 0;
                return Response.Success(resposta);
            }

            // 4. Loop de Juros Compostos (Mês a Mês)
            decimal saldoSimulado = patrimonioTotal;
            DateTime dataSimulada = DateTime.Today;
            int meses = 0;

            // Trava de segurança para loop infinito (ex: 50 anos)
            while (saldoSimulado * taxaMensal < request.MetaRendaMensal && meses < 600)
            {
                meses++;
                dataSimulada = dataSimulada.AddMonths(1);

                // A. Rendimento do mês (Juros sobre o saldo)
                decimal rendimento = saldoSimulado * taxaMensal;

                // B. Somar Rendimento + Novo Aporte ao Saldo
                saldoSimulado += rendimento + request.AporteMensal;

                // Adiciona na lista para gráfico de evolução
                resposta.EvolucaoMensal.Add(new EvolucaoMesDto
                {
                    MesNumero = meses,
                    Data = dataSimulada,
                    PatrimonioAcumulado = Math.Round(saldoSimulado, 2),
                    RendaGerada = Math.Round(saldoSimulado * taxaMensal, 2)
                });
            }

            // 5. Finalização
            resposta.MesesRestantes = meses;
            resposta.DataAtingimentoMeta = dataSimulada;
            resposta.PatrimonioNecessario = saldoSimulado; // Quanto precisou juntar para bater a meta

            return Response.Success(resposta);
        }
    }
}
