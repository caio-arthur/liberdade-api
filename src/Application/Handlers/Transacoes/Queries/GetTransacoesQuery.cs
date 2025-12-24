using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Wrappers;
using Application.Handlers.Transacoes.Responses;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Gridify;
using Gridify.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers.Transacoes.Queries
{
    public class GetTransacoesQuery : GridifyQuery, IRequestWrapper<Paging<TransacaoDto>>
    {

    }

    public class GetTransacoesQueryHandler : IHandlerWrapper<GetTransacoesQuery, Paging<TransacaoDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetTransacoesQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Response<Paging<TransacaoDto>>> Handle(GetTransacoesQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Transacoes.AsNoTracking().ProjectTo<TransacaoDto>(_mapper.ConfigurationProvider);
            var paging = await query.GridifyAsync(request, cancellationToken);
            return Response.Success(paging);
        }
    }
}
