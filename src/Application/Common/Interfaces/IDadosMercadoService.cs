namespace Application.Common.Interfaces
{
    public interface IDadosMercadoService
    {
        Task<decimal?> ObterTaxaSelicAtualAsync(); // Retorna a taxa Selic anualizada (ex: 11.25)
        Task<decimal?> ObterPrecoTesouroDiretoAsync(string codigoTesouro); // Retorna o preço unitário de venda (resgate) do Tesouro
        Task<(decimal Preco, decimal UltimoRendimento)?> ObterDadosFiiAsync(string ticker);// Retorna o preço atual e o último rendimento (DY, Dividendo)
    }
}
