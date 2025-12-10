using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Wrappers;
using Application.Handlers.Ativos.Responses;
using AutoMapper;

namespace Application.Handlers.Ativos.Queries.GetAtivoById
{
    public class GetAtivoByIdQuery : IRequestWrapper<AtivoDto>
    {
        public Guid Id { get; set; }
    }

    public class GetAtivoByIdQueryHandler : IHandlerWrapper<GetAtivoByIdQuery, AtivoDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetAtivoByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Response<AtivoDto>> Handle(GetAtivoByIdQuery request, CancellationToken cancellationToken)
        {
            var entity = await _context.Ativos.FindAsync([request.Id], cancellationToken);
        
            if (entity == null)
            {
                throw new DomainException(AtivoErrors.NotFound(request.Id));
            }

            return Response.Success(_mapper.Map<AtivoDto>(entity));
        }
    }
}
