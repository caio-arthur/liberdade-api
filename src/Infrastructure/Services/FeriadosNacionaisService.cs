using Application.Common.DTOs;
using Application.Common.Interfaces;
using AutoMapper;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace Infrastructure.Services
{
    public class FeriadosNacionaisService : IFeriadosNacionaisService
    {
        private readonly IApplicationDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly string _token;
        private readonly IMapper _mapper;

        public FeriadosNacionaisService(
            IApplicationDbContext context,
            HttpClient client,
            IConfiguration config,
            IMapper mapper)
        {
            _context = context;
            _httpClient = client;
            _token = config["ApiInvertexto:Key"];
            _mapper = mapper;
        }

        public async Task<List<FeriadoNacionalDto>> GetFeriadosNacionaisPorEstadoUfEAno(string uf, int ano, CancellationToken cancellationToken = default)
        {
            var feriadosExistentes = await _context.FeriadosNacionais
                .Where(f => f.Uf == uf && f.Data.Year == ano && f.Tipo != "facultativo")
                .AsNoTracking() 
                .ToListAsync(cancellationToken); 

            if (feriadosExistentes.Count > 0)
            {
                return _mapper.Map<List<FeriadoNacionalDto>>(feriadosExistentes);
            }

            var url = $"https://api.invertexto.com/v1/holidays/{ano}?token={_token}&state={uf}";

            using var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode) return []; 

            var feriadosDto = await response.Content.ReadFromJsonAsync<List<FeriadoNacionalDto>>(cancellationToken: cancellationToken);

            if (feriadosDto is null || feriadosDto.Count == 0) return [];

            var entidadesFeriados = _mapper.Map<List<FeriadoNacional>>(feriadosDto);

            foreach (var f in entidadesFeriados)
            {
                f.Uf = uf;
            }

            _context.FeriadosNacionais.AddRange(entidadesFeriados);

            await _context.SaveChangesAsync(cancellationToken);

            // Retorna os feriados obtidos da API, exceto facultativos
            return [.. feriadosDto.Where(f => f.Tipo != "facultativo")];
        }

        public async Task<bool> EhDiaUtilAsync(DateTime data, string uf, CancellationToken cancellationToken = default)
        {
            if (data.DayOfWeek == DayOfWeek.Saturday || data.DayOfWeek == DayOfWeek.Sunday)
            {
                return false;
            }
            var feriados = await GetFeriadosNacionaisPorEstadoUfEAno(uf, data.Year, cancellationToken);
            return !feriados.Any(f => f.Data.Date == data.Date);
        }

    }
}