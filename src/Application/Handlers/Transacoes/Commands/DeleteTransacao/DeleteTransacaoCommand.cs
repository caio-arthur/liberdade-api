using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Wrappers;
using Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers.Transacoes.Commands.DeleteTransacao
{
    public class DeleteTransacaoCommand : IRequestWrapper
    {
        public Guid Id { get; set; }
    }

    public class DeleteTransacaoCommandHandler : IHandlerWrapper<DeleteTransacaoCommand>
    {
        private readonly IApplicationDbContext _context;

        public DeleteTransacaoCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Response> Handle(DeleteTransacaoCommand request, CancellationToken cancellationToken)
        {
            var transacao = await _context.Transacoes
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (transacao == null)
                return Response.Failure(new Error(404, "NotFound", "Transação não encontrada."));

            if (transacao.AtivoId.HasValue && transacao.TipoTransacao == TransacaoTipo.Compra)
            {
                var posicao = await _context.PosicaoCarteiras
                    .FirstOrDefaultAsync(p => p.AtivoId == transacao.AtivoId, cancellationToken);

                if (posicao != null)
                {
                    var totalAtual = posicao.Quantidade * posicao.PrecoMedio;
                    var totalTransacao = transacao.Quantidade * transacao.PrecoUnitario;

                    var novaQuantidade = posicao.Quantidade - transacao.Quantidade;

                    if (novaQuantidade > 0)
                    {
                        var novoTotal = totalAtual - totalTransacao;
                        if (novoTotal < 0) novoTotal = 0;

                        posicao.PrecoMedio = novoTotal / novaQuantidade;
                        posicao.Quantidade = novaQuantidade;
                    }
                    else
                    {
                        posicao.Quantidade = 0;
                        posicao.PrecoMedio = 0;
                    }
                }
            }

            _context.Transacoes.Remove(transacao);
            await _context.SaveChangesAsync(cancellationToken);

            return Response.Success();
        }
    }
}
