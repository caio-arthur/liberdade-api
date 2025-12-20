using Application.Common.DTOs;

namespace Application.Common.Interfaces
{
    public interface IFeriadosNacionaisService
    {
        Task<List<FeriadoNacionalDto>> GetFeriadosNacionaisPorEstadoUfEAno(string uf, int ano, CancellationToken cancellationToken = default);
    }
}