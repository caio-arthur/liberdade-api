namespace Application.Common.Interfaces
{
    public interface IAgenteFinanceiroService
    {
        Task<string> GerarRelatorioDiarioAsync(ContextoFinanceiroDto contexto);
    }

    public record ContextoFinanceiroDto(
        string NomeUsuario,
        string NomeConjuge,
        decimal PatrimonioTotal,
        decimal MetaRenda,
        decimal RendaAtual,
        decimal VariacaoPatrimonialDiaria, 
        decimal RendimentoPassivoDiario,
        decimal PercentualMetaAtingido,
        string FaseAtual,
        int MesesRestantes,
        DateTime DataEstimadaMeta,         
        List<string> UltimasMovimentacoes
    );
}
