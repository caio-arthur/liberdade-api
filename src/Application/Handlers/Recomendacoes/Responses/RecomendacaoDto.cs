namespace Application.Handlers.Recomendacoes.Responses
{
    public record RecomendacaoDto(
            string Categoria,
            string CodigoAtivo,       // Ex: HGLG11
            string Acao,              // COMPRAR, VENDER, AGUARDAR
            decimal ValorSugerido,    // Quanto comprar/vender financeiramente
            decimal QuantidadeEstimada // Quantas cotas (ValorSugerido / PrecoAtual)
        );
}
