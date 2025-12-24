using Core.Enums;

namespace Core.Entities
{
    public class PosicaoCarteira
    {
        public Guid AtivoId { get; set; } = Guid.NewGuid();
        public string Codigo { get; set; }
        public AtivoCategoria Categoria { get; set; }
        public decimal Quantidade { get; set; }
        public decimal PrecoMedio { get; set; } // Preço Médio
        public decimal ValorTotalAtual => Quantidade * PrecoAtual;
        public decimal PrecoAtual { get; set; } // Preço de mercado atual
    }
}
