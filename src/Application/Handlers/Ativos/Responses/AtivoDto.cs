using Application.Common.DTOs;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using AutoMapper;
using Core.Entities;

namespace Application.Handlers.Ativos.Responses
{
    public class AtivoDto : IMapFrom<Ativo>
    {
        public Guid Id { get; set; }
        public string Codigo { get; set; } // Ex: "SELIC2031", "KNCR11"
        public string Nome { get; set; } // Ex: "Tesouro Selic 2031", "Kinea Rendimentos"
        public EnumDto Categoria { get; set; }
        public decimal PrecoAtual { get; set; }
        public decimal ValorRendimentoMesAnterior { get; set; } // Último dividendo ou rendimento mensal recente
        public decimal PercentualDeRetornoMensalEsperado { get; set; } // Ex: 0.0105 para 1.05%
        public DateTime AtualizadoEm { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Ativo, AtivoDto>()
                .ForMember(dest => dest.Categoria, opt => opt.MapFrom(src => src.Categoria.ToEnumDto()))
                .ForMember(dest => dest.AtualizadoEm, opt => opt.MapFrom(src => src.AtualizadoEm.UtcToBrazilLocalTime()))
                .ForMember(dest => dest.ValorRendimentoMesAnterior, opt => opt.MapFrom(src => src.RendimentoValorMesAnterior))
                ;
        }
    }
}
