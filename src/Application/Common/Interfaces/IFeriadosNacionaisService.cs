using Application.Common.DTOs;

namespace Application.Common.Interfaces
{
    public interface IFeriadosNacionaisService
    {
        Task<List<FeriadoNacionalDto>> GetFeriadosNacionaisPorEstadoUfEAno(string uf, int ano, CancellationToken cancellationToken = default);
        Task<bool> EhDiaUtilAsync(DateTime data, string uf, CancellationToken cancellationToken = default);
    }
}