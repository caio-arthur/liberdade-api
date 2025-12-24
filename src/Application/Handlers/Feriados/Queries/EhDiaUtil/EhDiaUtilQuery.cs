using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Wrappers;
using MediatR;

namespace Application.Handlers.Feriados.Queries.EhDiaUtil
{
    public class EhDiaUtilQuery : IRequestWrapper<bool>
    {
        public DateTime Data { get; set; } = DateTime.Today;
    }

    public class EhDiaUtilQueryHandler : IRequestHandler<EhDiaUtilQuery, Response<bool>>
    {
        private readonly IFeriadosNacionaisService _feriados;
        public EhDiaUtilQueryHandler(IFeriadosNacionaisService feriados)
        {
            _feriados = feriados;
        }
        public async Task<Response<bool>> Handle(EhDiaUtilQuery request, CancellationToken cancellationToken)
        {
            var hoje = DateTime.Today;
            bool ehDiaUtil = await _feriados.EhDiaUtilAsync(hoje, "MG", cancellationToken);
            return Response.Success(ehDiaUtil);
        }
    }
}
