using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Wrappers;
using Application.Handlers.MetaAlocacoes.Responses;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers.MetaAlocacoes.Queries
{
    public class ObterMetaAlocacaoQuery : IRequestWrapper<List<MetaAlocacaoDto>>
    {

    }

    public class ObterMetaAlocacaoQueryHandler : IHandlerWrapper<ObterMetaAlocacaoQuery, List<MetaAlocacaoDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public ObterMetaAlocacaoQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Response<List<MetaAlocacaoDto>>> Handle(ObterMetaAlocacaoQuery request, CancellationToken cancellationToken)
        {
            var result = await _context.MetaAlocacoes
                .Where(ma => ma.Ativa)
                .ToListAsync(cancellationToken);
            if (result == null || result.Count == 0)
                return Response.Success(new List<MetaAlocacaoDto>());

            return Response.Success(_mapper.Map<List<MetaAlocacaoDto>>(result));
        }
    }
}
