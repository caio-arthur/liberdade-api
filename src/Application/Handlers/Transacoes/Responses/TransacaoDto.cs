using Application.Common.DTOs;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using AutoMapper;
using Core.Entities;

namespace Application.Handlers.Transacoes.Responses
{
    public class TransacaoDto : IMapFrom<Transacao>
    {
        public Guid Id { get; set; }
        public Guid? AtivoId { get; set; }
        public string? AtivoCodigo { get; set; }
        public EnumDto TipoTransacao { get; set; }
        public decimal Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public decimal ValorTotal { get; set; }
        public DateTime Data { get; set; }
        public string Observacoes { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Transacao, TransacaoDto>()
                .ForMember(d => d.AtivoCodigo, opt => opt.MapFrom(s => s.Ativo.Codigo))
                .ForMember(d => d.TipoTransacao, opt => opt.MapFrom(s => s.TipoTransacao.ToEnumDto()))
                ;
        }
    }
}
