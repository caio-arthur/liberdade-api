using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Wrappers;
using Application.Handlers.Ativos.Responses;
using Core.Entities;
using Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers.Transacoes.Commands.RealizarCompra
{
    public class RealizarCompraCommand : IRequestWrapper
    {
        public Guid AtivoId { get; set; }
        public decimal Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public DateTime Data { get; set; }
        public string Observacoes { get; set; }
    }

    public class RealizarCompraCommandHandler : IHandlerWrapper<RealizarCompraCommand>
    {
        private readonly IApplicationDbContext _context;

        public RealizarCompraCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Response> Handle(RealizarCompraCommand request, CancellationToken cancellationToken)
        {
            var ativo = await _context.Ativos.FindAsync([request.AtivoId], cancellationToken);
            if (ativo == null)
            {
                throw new DomainException(AtivoErrors.NotFound(request.AtivoId));
            }

            var transacao = new Transacao
            {
                AtivoId = request.AtivoId,
                TipoTransacao = TransacaoTipo.Compra,
                Quantidade = request.Quantidade,
                PrecoUnitario = request.PrecoUnitario,
                ValorTotal = request.Quantidade * request.PrecoUnitario,
                Data = request.Data,
                Observacoes = request.Observacoes ?? $"Compra de {ativo.Codigo}"
            };

            _context.Transacoes.Add(transacao);

            var posicao = await _context.PosicaoCarteiras
                .FirstOrDefaultAsync(p => p.AtivoId == request.AtivoId, cancellationToken);

            if (posicao == null)
            {
                posicao = new PosicaoCarteira
                {
                    AtivoId = request.AtivoId,
                    Codigo = ativo.Codigo,
                    Categoria = ativo.Categoria,
                    Quantidade = request.Quantidade,
                    PrecoMedio = request.PrecoUnitario,
                    PrecoAtual = ativo.PrecoAtual 
                };
                _context.PosicaoCarteiras.Add(posicao);
            }
            else
            {
                var totalAtual = posicao.Quantidade * posicao.PrecoMedio;
                var totalCompra = request.Quantidade * request.PrecoUnitario;
                var novaQuantidade = posicao.Quantidade + request.Quantidade;

                posicao.PrecoMedio = (totalAtual + totalCompra) / novaQuantidade;
                posicao.Quantidade = novaQuantidade;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Response.Success();
        }
    }
}