namespace Core.Entities
{
    public class FeriadoNacional
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Data { get; set; }
        public string Nome { get; set; }
        public string Tipo { get; set; }
        public string Nivel { get; set; }
        public string Uf { get; set; }
    }
}
