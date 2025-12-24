using Application.Common.DTOs;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Mapping;
using Core.Entities;

namespace Application.Handlers.MetaAlocacoes.Responses
{
    public class MetaAlocacaoDto : IMapFrom<MetaAlocacao>
    {
        public Guid Id { get; set; }
        public EnumDto Categoria { get; set; }
        public decimal PercentualAlvo { get; set; }
        public int NumeroFase { get; set; }
        public bool Ativa { get; set; }

        public void Mapping(MappingProfile profile)
        {
            profile.CreateMap<MetaAlocacao, MetaAlocacaoDto>()
                .ForMember(dest => dest.Categoria, opt => opt.MapFrom(src => src.Categoria.ToEnumDto()))
                ;
        }
    }
}
