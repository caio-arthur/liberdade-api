namespace Application.Handlers.Previsoes.Responses
{
    public class PrevisaoRetornoDto
    {
        public decimal PatrimonioAtual { get; set; }
        public decimal RendaPassivaAtual { get; set; }
        public decimal MetaRendaMensal { get; set; }

        // O resultado principal
        public DateTime DataAtingimentoMeta { get; set; }
        public int MesesRestantes { get; set; }
        public decimal PatrimonioNecessario { get; set; }

        // Para gráficos (opcional, mas útil)
        public List<EvolucaoMesDto> EvolucaoMensal { get; set; } = new();
    }
}

