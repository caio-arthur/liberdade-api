using Core.Enums;

namespace Core.Entities
{
    public class Transacao
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? AtivoId { get; set; }
        public Ativo? Ativo { get; set; }
        public TransacaoTipo TipoTransacao { get; set; }
        public decimal Quantidade { get; set; } // Aceita frações
        public decimal PrecoUnitario { get; set; }
        public decimal ValorTotal { get; set; } // Quantidade × Preço ou valor absoluto (aporte)
        public DateTime Data { get; set; } = DateTime.UtcNow;
        public string Observacoes { get; set; }
    }

}
