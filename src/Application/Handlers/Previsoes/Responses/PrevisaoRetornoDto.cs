namespace Application.Handlers.Previsoes.Responses
{
    public class PrevisaoRetornoDto
    {
        public decimal PatrimonioAtual { get; set; }
        public decimal RendaPassivaAtual { get; set; }
        public decimal MetaRendaMensal { get; set; }
        public DateTime DataAtingimentoMeta { get; set; }
        public int MesesRestantes { get; set; } // Mantemos para fácil leitura
        public decimal PatrimonioNecessario { get; set; }
        public List<EvolucaoPontoDto> EvolucaoDiaria { get; set; } = new();
    }
}

