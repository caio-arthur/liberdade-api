namespace Core.Entities
{
    public class HistoricoPatrimonio
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Data { get; set; } = DateTime.UtcNow;
        public decimal ValorTotal { get; set; }
        public decimal RendaPassivaCalculada { get; set; }
    }
}
