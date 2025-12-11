using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Wrappers;
using Core.Entities;
using Core.Enums;

namespace Application.Handlers.Transacoes.Commands.RegistrarAporte
{
    public class RegistrarAporteCommand : IRequestWrapper
    {
        public decimal Valor { get; set; }
        public DateTime Data { get; set; }
        public string Observacoes { get; set; }
    }

    public class RegistrarAporteCommandHandler : IHandlerWrapper<RegistrarAporteCommand>
    {
        private readonly IApplicationDbContext _context;

        public RegistrarAporteCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Response> Handle(RegistrarAporteCommand request, CancellationToken cancellationToken)
        {
            var transacao = new Transacao
            {
                AtivoId = null, 
                TipoTransacao = TransacaoTipo.Aporte,
                Quantidade = 1, 
                PrecoUnitario = request.Valor,
                ValorTotal = request.Valor,
                Data = request.Data,
                Observacoes = string.IsNullOrEmpty(request.Observacoes) ? "Aporte Mensal" : request.Observacoes
            };

            _context.Transacoes.Add(transacao);
            await _context.SaveChangesAsync(cancellationToken);

            return Response.Success();
        }
    }
}