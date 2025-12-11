namespace Application.Handlers.Previsoes.Responses
{
    public class EvolucaoMesDto
    {
        public int MesNumero { get; set; }
        public DateTime Data { get; set; }
        public decimal PatrimonioAcumulado { get; set; }
        public decimal RendaGerada { get; set; }
    }
}