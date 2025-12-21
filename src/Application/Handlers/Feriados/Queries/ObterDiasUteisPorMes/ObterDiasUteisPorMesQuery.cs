using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Wrappers;
using MediatR;

namespace Application.Handlers.Feriados.Queries.ObterDiasUteisPorMes
{
    public class ObterDiasUteisPorMesQuery : IRequestWrapper<int>
    {
        public int Ano { get; set; }
        public int Mes { get; set; }
        public string Uf { get; set; } = "MG";
    }

    public class ObterDiasUteisPorMesQueryHandler : IRequestHandler<ObterDiasUteisPorMesQuery, Response<int>>
    {
        private readonly IFeriadosNacionaisService _feriadosService;
        public ObterDiasUteisPorMesQueryHandler(IFeriadosNacionaisService feriadosService)
        {
            _feriadosService = feriadosService;
        }
        public async Task<Response<int>> Handle(ObterDiasUteisPorMesQuery request, CancellationToken cancellationToken)
        {
            var totalDiasNoMes = DateTime.DaysInMonth(request.Ano, request.Mes);
            var feriados = await _feriadosService.GetFeriadosNacionaisPorEstadoUfEAno(request.Uf, request.Ano, cancellationToken);
            var diasUteis = 0;
            for (int dia = 1; dia <= totalDiasNoMes; dia++)
            {
                var dataAtual = new DateTime(request.Ano, request.Mes, dia);
                if (dataAtual.DayOfWeek != DayOfWeek.Saturday && dataAtual.DayOfWeek != DayOfWeek.Sunday &&
                    !feriados.Any(f => f.Data.Date == dataAtual.Date))
                {
                    diasUteis++;
                }
            }
            return Response.Success(diasUteis);
        }
    }
}
