using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Wrappers;
using Application.Handlers.HistoricosPatrimonio.Responses;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Gridify;
using Gridify.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers.HistoricosPatrimonio.Queries
{
    public class GetHistoricoPatrimonioQuery : GridifyQuery, IRequestWrapper<Paging<HistoricoPatrimonioDto>>
    {

    }

    public class GetHistoricoPatrimonioQueryHandler : IHandlerWrapper<GetHistoricoPatrimonioQuery, Paging<HistoricoPatrimonioDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;


        public GetHistoricoPatrimonioQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;

        }

        public async Task<Response<Paging<HistoricoPatrimonioDto>>> Handle(GetHistoricoPatrimonioQuery request, CancellationToken cancellationToken)
        {
            var query = _context.HistoricoPatrimonios.AsNoTracking().ProjectTo<HistoricoPatrimonioDto>(_mapper.ConfigurationProvider);
            var paging = await query.GridifyAsync(request, cancellationToken);
            return Response.Success(paging);
        }
    }
}
