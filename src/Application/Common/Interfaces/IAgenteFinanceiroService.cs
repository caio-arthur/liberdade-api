namespace Application.Common.Interfaces
{
    public interface IAgenteFinanceiroService
    {
        Task<string> GerarRelatorioDiarioAsync(ContextoFinanceiroDto contexto);
    }

    // DTO que resume tudo que a IA precisa saber
    public record ContextoFinanceiroDto(
        string NomeUsuario,
        string NomeConjuge,
        decimal PatrimonioTotal,
        decimal MetaRenda,
        decimal RendaAtual,
        List<string> UltimasMovimentacoes,
        string FaseAtual,
        decimal VariacaoDiaria
    );
}
