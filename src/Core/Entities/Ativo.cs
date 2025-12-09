using Core.Enums;

namespace Core.Entities
{
    public class Ativo
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Codigo { get; set; } // Ex: "SELIC2031", "KNCR11" 
        public string Nome { get; set; } // Ex: "Tesouro Selic 2031", "Kinea Rendimentos"
        public AtivoCategoria Categoria { get; set; }
        public decimal PrecoAtual { get; set; } 
        public decimal RendimentoValorMesAnterior { get; set; } // Para FIIs: Último dividendo pago. Para Renda Fixa: Rendimento mensal recente.
        public decimal PercentualDeRetornoMensalEsperado { get; set; } // Ex: 0.0105 para 1.05% (Selic) ou 0.0080 para 0.80% (FII Shopping)
        public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
    }
}
