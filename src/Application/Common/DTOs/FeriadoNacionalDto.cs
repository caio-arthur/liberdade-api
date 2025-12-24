using Application.Common.Interfaces;
using AutoMapper;
using Core.Entities;
using System.Text.Json.Serialization;

namespace Application.Common.DTOs
{
    public class FeriadoNacionalDto : IMapFrom<FeriadoNacional>
    {
        [JsonPropertyName("date")]
        public DateTime Data { get; set; }
        [JsonPropertyName("name")]
        public string Nome { get; set; }
        [JsonPropertyName("type")]
        public string Tipo { get; set; }
        [JsonPropertyName("level")]
        public string Nivel { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<FeriadoNacional, FeriadoNacionalDto>()
                .ForMember(dest => dest.Data, opt => opt.MapFrom(src => src.Data))
                .ForMember(dest => dest.Nome, opt => opt.MapFrom(src => src.Nome))
                .ForMember(dest => dest.Tipo, opt => opt.MapFrom(src => src.Tipo))
                .ForMember(dest => dest.Nivel, opt => opt.MapFrom(src => src.Nivel));
            profile.CreateMap<FeriadoNacionalDto, FeriadoNacional>()
                .ForMember(dest => dest.Data, opt => opt.MapFrom(src => src.Data))
                .ForMember(dest => dest.Nome, opt => opt.MapFrom(src => src.Nome))
                .ForMember(dest => dest.Tipo, opt => opt.MapFrom(src => src.Tipo))
                .ForMember(dest => dest.Nivel, opt => opt.MapFrom(src => src.Nivel));
        }
    }
}