using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Wrappers;
using Application.Handlers.CarteiraPosicoes.Responses;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Gridify;
using Gridify.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers.CarteiraPosicoes.Queries
{
    public class GetCarteiraPosicaoQuery : GridifyQuery, IRequestWrapper<Paging<CarteiraPosicaoDto>>
    {

    }

    public class GetCarteiraPosicaoQueryHandler : IHandlerWrapper<GetCarteiraPosicaoQuery, Paging<CarteiraPosicaoDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetCarteiraPosicaoQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Response<Paging<CarteiraPosicaoDto>>> Handle(GetCarteiraPosicaoQuery request, CancellationToken cancellationToken)
        {
            var query = _context.PosicaoCarteiras.AsNoTracking().ProjectTo<CarteiraPosicaoDto>(_mapper.ConfigurationProvider);
            var paging = await query.GridifyAsync(request, cancellationToken);
            return Response.Success(paging);
        }
    }
}