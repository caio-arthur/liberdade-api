using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Wrappers;
using Core.Entities;
using Core.Enums;

namespace Application.Handlers.Ativos.Commands.Create
{
    public class CreateAtivoCommand : IRequestWrapper
    {
        public string Codigo { get; set; } // Ex: "SELIC2031", "KNCR11"
        public string Nome { get; set; } // Ex: "Tesouro Selic 2031", "Kinea Rendimentos"
        public AtivoCategoria Categoria { get; set; }
        public decimal PrecoAtual { get; set; }
        public decimal RendimentoValorMesAnterior { get; set; } // Para FIIs: último dividendo; para RF: rendimento mensal recente.
        public decimal PercentualDeRetornoMensalEsperado { get; set; } // Ex: 0.0105 para 1.05%
    }

    public class CreateAtivoCommandHandler : IHandlerWrapper<CreateAtivoCommand>
    {
        private readonly IApplicationDbContext _context;
        public CreateAtivoCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<Response> Handle(CreateAtivoCommand request, CancellationToken cancellationToken)
        {
            var entity = new Ativo
            {
                Codigo = request.Codigo,
                Nome = request.Nome,
                Categoria = request.Categoria,
                PrecoAtual = request.PrecoAtual,
                RendimentoValorMesAnterior = request.RendimentoValorMesAnterior,
                PercentualDeRetornoMensalEsperado = request.PercentualDeRetornoMensalEsperado,
                AtualizadoEm = DateTime.UtcNow
            };
            _context.Ativos.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return Response.Success();
        }
    }
}
