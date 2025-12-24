using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Wrappers;
using Application.Handlers.Recomendacoes.Responses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers.Recomendacoes.Queries
{
    public class ObterRecomendacaoInvestimentoQuery : IRequestWrapper<IEnumerable<RecomendacaoDto>>
    {
        public decimal ValorAporte { get; set; } = 0;
    }

    public class ObterRecomendacaoInvestimentoHandler : IRequestHandler<ObterRecomendacaoInvestimentoQuery, Response<IEnumerable<RecomendacaoDto>>>
    {
        private readonly IApplicationDbContext _context;

        public ObterRecomendacaoInvestimentoHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Response<IEnumerable<RecomendacaoDto>>> Handle(ObterRecomendacaoInvestimentoQuery request, CancellationToken cancellationToken)
        {
            // 1. Obter Metas Ativas (Define as regras do jogo atual)
            var metas = await _context.MetaAlocacoes
                .Where(m => m.Ativa)
                .ToListAsync(cancellationToken);

            if (!metas.Any())
                throw new Exception("Nenhuma meta de alocação ativa encontrada. Configure a fase atual.");

            // 2. Obter Posição Atual (O que temos hoje)
            var posicoes = await _context.PosicaoCarteiras.ToListAsync(cancellationToken);

            // 3. Obter TODOS os ativos cadastrados (Para sugerir compras de coisas que ainda não temos na carteira)
            var todosAtivos = await _context.Ativos.ToListAsync(cancellationToken);

            // Cálculo do Patrimônio Total (Considerando o Aporte)
            // Nota: Como ValorTotalAtual é ignorado no banco, calculamos em memória
            decimal totalPatrimonioAtual = posicoes.Sum(p => p.Quantidade * p.PrecoAtual);
            decimal patrimonioFuturo = totalPatrimonioAtual + request.ValorAporte;

            var recomendacoes = new List<RecomendacaoDto>();

            foreach (var meta in metas)
            {
                // Quanto $$ deveríamos ter nesta categoria?
                decimal valorAlvoCategoria = patrimonioFuturo * (meta.PercentualAlvo / 100);

                // Quanto $$ temos atualmente nesta categoria?
                var posicoesDaCategoria = posicoes.Where(p => p.Categoria == meta.Categoria).ToList();
                decimal valorAtualCategoria = posicoesDaCategoria.Sum(p => p.Quantidade * p.PrecoAtual);

                // Diferença (Positivo = Precisa Comprar / Negativo = Precisa Vender/Aguardar)
                decimal diferenca = valorAlvoCategoria - valorAtualCategoria;

                // Percentual atual real
                decimal percentualAtual = patrimonioFuturo > 0 ? (valorAtualCategoria / patrimonioFuturo) * 100 : 0;

                // Lógica de Seleção de Ativo Específico
                string codigoAtivoSugerido = "N/A";
                decimal precoReferencia = 0;
                string acao = "AGUARDAR";
                decimal valorSugerido = 0;

                // Threshold: Só sugere ação se o desvio for maior que R$ 10,00 (evita recomendações de centavos)
                if (Math.Abs(diferenca) > 10)
                {
                    if (diferenca > 0) // COMPRA
                    {
                        acao = "COMPRAR";
                        valorSugerido = diferenca;

                        // Estratégia: Comprar o ativo que está mais "para trás" na categoria ou um novo se não tiver nenhum
                        if (posicoesDaCategoria.Any())
                        {
                            // Já temos ativos nessa categoria? Compra o que tem menor valor alocado (rebalanceamento intra-categoria)
                            var ativoMenorPosicao = posicoesDaCategoria
                                .OrderBy(p => p.Quantidade * p.PrecoAtual)
                                .First();

                            codigoAtivoSugerido = ativoMenorPosicao.Codigo;
                            precoReferencia = ativoMenorPosicao.PrecoAtual;
                        }
                        else
                        {
                            // Não temos posição nessa categoria (Ex: Entrando na Fase 2)
                            // Busca um ativo dessa categoria na tabela de Ativos (Geral)
                            var candidato = todosAtivos
                                .Where(a => a.Categoria == meta.Categoria)
                                .OrderBy(a => a.Codigo) // Aqui poderia ser uma lógica de Ranking/Valuation futuro
                                .FirstOrDefault();

                            if (candidato != null)
                            {
                                codigoAtivoSugerido = candidato.Codigo;
                                precoReferencia = candidato.PrecoAtual;
                            }
                        }
                    }
                    else // VENDA (diferenca < 0)
                    {
                        // Só sugerimos venda se NÃO estivermos na fase de acumulação pura
                        // Mas como regra geral: Vende o que tem maior posição para realizar lucro/equilibrar
                        acao = "VENDER";
                        valorSugerido = Math.Abs(diferenca);

                        var ativoMaiorPosicao = posicoesDaCategoria
                            .OrderByDescending(p => p.Quantidade * p.PrecoAtual)
                            .FirstOrDefault();

                        if (ativoMaiorPosicao != null)
                        {
                            codigoAtivoSugerido = ativoMaiorPosicao.Codigo;
                            precoReferencia = ativoMaiorPosicao.PrecoAtual;
                        }
                    }
                }

                // Monta o DTO
                recomendacoes.Add(new RecomendacaoDto(
                    meta.Categoria.ToString(),
                    codigoAtivoSugerido,
                    acao,
                    Math.Round(valorSugerido, 2),
                    precoReferencia > 0 ? Math.Round(valorSugerido / precoReferencia, 2) : 0 // Qtd Estimada
                ));
            }

            // Ordena por maior necessidade de compra (quem tem maior "gap" financeiro)
            var result = recomendacoes
                .Where(r => r.Acao != "AGUARDAR")
                .OrderByDescending(r => r.ValorSugerido);

            return Response.Success<IEnumerable<RecomendacaoDto>>(result);
        }
    }
}