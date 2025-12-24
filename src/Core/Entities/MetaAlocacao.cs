using Core.Enums;

namespace Core.Entities
{
    public class MetaAlocacao
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public AtivoCategoria Categoria { get; set; }
        public decimal PercentualAlvo { get; set; } // Quanto eu quero ter disso? Ex: 0.40 (40%)
        public int NumeroFase { get; set; } // Fase 1 (Acumulação), Fase 2 (Renda), etc.
        public bool Ativa { get; set; } // Se TRUE, o sistema usa isso para recomendar compras/vendas
    }
}
