using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Wrappers;
using Core.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace Application.Handlers.Transacoes.Commands.UpdateTransacao
{
    public class UpdateTransacaoCommand : IRequestWrapper
    {
        [JsonIgnore]
        public Guid Id { get; set; }
        public decimal Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public DateTime Data { get; set; }
        public string Observacoes { get; set; }
    }

    public class UpdateTransacaoCommandHandler : IHandlerWrapper<UpdateTransacaoCommand>
    {
        private readonly IApplicationDbContext _context;

        public UpdateTransacaoCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Response> Handle(UpdateTransacaoCommand request, CancellationToken cancellationToken)
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
                    var totalAntigo = transacao.Quantidade * transacao.PrecoUnitario;

                    var qtdAposReversao = posicao.Quantidade - transacao.Quantidade;
                    var totalAposReversao = totalAtual - totalAntigo;

                    var totalNovo = request.Quantidade * request.PrecoUnitario;
                    var qtdFinal = qtdAposReversao + request.Quantidade;
                    var totalFinal = totalAposReversao + totalNovo;

                    if (qtdFinal > 0)
                    {
                        posicao.PrecoMedio = totalFinal / qtdFinal;
                        posicao.Quantidade = qtdFinal;
                    }
                    else
                    {
                        posicao.PrecoMedio = 0;
                        posicao.Quantidade = 0;
                    }
                }
            }

            transacao.Quantidade = request.Quantidade;
            transacao.PrecoUnitario = request.PrecoUnitario;
            transacao.ValorTotal = request.Quantidade * request.PrecoUnitario;
            transacao.Data = request.Data;
            transacao.Observacoes = request.Observacoes;

            await _context.SaveChangesAsync(cancellationToken);

            return Response.Success();
        }
    }
}
