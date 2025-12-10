using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Wrappers;
using Application.Handlers.Ativos.DTOs;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Gridify;
using Gridify.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers.Ativos.Queries.GetAtivos
{
    public class GetAtivosQuery : GridifyQuery, IRequestWrapper<Paging<AtivoDto>>
    {

    }

    public class GetAtivosQueryHandler : IHandlerWrapper<GetAtivosQuery, Paging<AtivoDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetAtivosQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Response<Paging<AtivoDto>>> Handle(GetAtivosQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Ativos.AsNoTracking().ProjectTo<AtivoDto>(_mapper.ConfigurationProvider);
            var paging = await query.GridifyAsync(request, cancellationToken);
            return Response.Success(paging);
        }
    }
}