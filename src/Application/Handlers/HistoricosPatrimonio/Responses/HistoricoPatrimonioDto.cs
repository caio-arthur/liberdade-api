using Application.Common.Interfaces;
using Application.Common.Mapping;
using Core.Entities;

namespace Application.Handlers.HistoricosPatrimonio.Responses
{
    public class HistoricoPatrimonioDto : IMapFrom<HistoricoPatrimonio>
    {
        public Guid Id { get; set; }
        public DateTime Data { get; set; }
        public decimal Valor { get; set; }
        public decimal RendaPassivaCalculada { get; set; }

        public void Mapping(MappingProfile profile)
        {
            profile.CreateMap<HistoricoPatrimonio, HistoricoPatrimonioDto>()
                .ForMember(d => d.Valor , opt => opt.MapFrom(s => s.ValorTotal))
                ;
        }
    }
}
