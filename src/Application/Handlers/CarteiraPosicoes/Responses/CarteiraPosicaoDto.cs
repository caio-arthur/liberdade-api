using Application.Common.DTOs;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Mapping;
using Core.Entities;

namespace Application.Handlers.CarteiraPosicoes.Responses
{
    public class CarteiraPosicaoDto : IMapFrom<PosicaoCarteira>
    {
        public Guid AtivoId { get; set; }
        public string Codigo { get; set; }
        public EnumDto Categoria { get; set; }
        public decimal Quantidade { get; set; }
        public decimal PrecoMedio { get; set; }
        public decimal PrecoAtual { get; set; }
        public void Mapping(MappingProfile profile)
        {
            profile.CreateMap<PosicaoCarteira, CarteiraPosicaoDto>()
                .ForMember(dest => dest.Categoria, opt => opt.MapFrom(src => src.Categoria.ToEnumDto()))
                ;
        }
    }
}
