namespace Application.Handlers.Previsoes.Responses
{
    public class EvolucaoPontoDto
    {
        public int DiasDecorridos { get; set; }
        public DateTime Data { get; set; }
        public decimal PatrimonioAcumulado { get; set; }
        public decimal RendaMensalEstimada { get; set; } // Quanto renderia num mês com esse saldo
    }

}
