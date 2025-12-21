using Application.Common.Interfaces;
using Application.Handlers.Feriados.Queries.ObterDiasUteisPorMes;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.IO.Compression;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Services
{
    public class DadosMercadoService : IDadosMercadoService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<DadosMercadoService> _logger;
        private readonly ISender _mediator;

        public DadosMercadoService(IHttpClientFactory httpClientFactory, ILogger<DadosMercadoService> logger, ISender sender)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _mediator = sender;
        }

        public async Task<decimal?> ObterTaxaSelicAtualAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BCB");

                // Buscamos os últimos 10 dias para garantir que pegamos o último dia útil, mesmo em feriados longos
                var response = await client.GetFromJsonAsync<List<BcbSerieDto>>("dados/serie/bcdata.sgs.11/dados/ultimos/10?formato=json");

                if (response != null && response.Any())
                {
                    // Ordena por data decrescente (do mais recente para o mais antigo)
                    var ultimoDado = response
                        .Select(x => new
                        {
                            Data = DateTime.ParseExact(x.Data, "dd/MM/yyyy", CultureInfo.InvariantCulture),
                            Valor = ParseDecimal(x.Valor)
                        })
                        .OrderByDescending(x => x.Data)
                        .FirstOrDefault();

                    if (ultimoDado != null)
                    {
                        // Obter dias úteis do mês via mediator de forma correta(await antes de acessar.Dados)
                        var diasUteisResponse = await _mediator.Send(new ObterDiasUteisPorMesQuery
                        {
                            Ano = ultimoDado.Data.Year,
                            Mes = ultimoDado.Data.Month,
                            Uf = "MG"
                        });

                        int diasUteisNoMes = diasUteisResponse.Dados;
                        if (diasUteisNoMes <= 0) diasUteisNoMes = 21; // fallback

                        // Fórmula: ((1 + taxa_dia/100) ^ dias_uteis) - 1 * 100
                        double taxaDia = (double)ultimoDado.Valor;
                        double taxaMensal = (Math.Pow(1 + (taxaDia / 100), diasUteisNoMes) - 1) * 100;

                        _logger.LogInformation("Selic Diária: {TaxaDia}% | Mensal Aprox: {TaxaMensal}% (Ref: {Data})",
                            taxaDia, taxaMensal.ToString("F4"), ultimoDado.Data.ToShortDateString());

                        return (decimal)taxaMensal;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter Selic Diária (Série 11)");
            }
            return null;
        }

        public async Task<decimal?> ObterPrecoTesouroDiretoAsync(string codigoTesouro)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("StatusInvest");
                var response = await client.GetFromJsonAsync<List<StatusInvestBondDto>>($"category/bondprice?ticker={codigoTesouro}&type=1");

                if (response != null && response.Any())
                {

                    var ultimoDado = response
                        .OrderByDescending(x => x.Date)
                        .FirstOrDefault();

                    return ultimoDado?.SellPrice;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter preço do Tesouro: {Ticker}", codigoTesouro);
            }
            return null;
        }

        public async Task<decimal?> ObterPrecoTesouroDiretoBcbAsync(string codigoIsin, int ano, int mes)
        {
            var url = $"http://www4.bcb.gov.br/pom/demab/negociacoes/download/NegE{ano}{mes:D2}.ZIP";

            try
            {
                var client = _httpClientFactory.CreateClient("BCB");

                using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Falha ao baixar ZIP do BCB. Status: {Status}. URL: {Url}", response.StatusCode, url);
                    return null;
                }

                using var stream = await response.Content.ReadAsStreamAsync();
                using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

                var csvEntry = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase));

                if (csvEntry == null)
                {
                    _logger.LogWarning("CSV não encontrado no ZIP: {Url}", url);
                    return null;
                }

                using var entryStream = csvEntry.Open();
                using var reader = new StreamReader(entryStream, Encoding.GetEncoding("ISO-8859-1"));

                string? headerLine = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(headerLine)) return null;

                var headers = headerLine.Split(';');
                int indexData = Array.IndexOf(headers, "DATA MOV");
                int indexIsin = Array.IndexOf(headers, "CODIGO ISIN");
                int indexPu = Array.IndexOf(headers, "PU MED");

                if (indexIsin == -1 || indexPu == -1)
                {
                    indexData = 0; indexIsin = 1; indexPu = 3;
                }

                decimal? ultimoPuEncontrado = null;
                DateTime dataMaisRecente = DateTime.MinValue;
                var culturaBr = new CultureInfo("pt-BR");

                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    var colunas = line.Split(';');
                    if (colunas.Length <= indexPu) continue;

                    if (colunas[indexIsin].Trim().Equals(codigoIsin, StringComparison.OrdinalIgnoreCase))
                    {
                        if (DateTime.TryParse(colunas[indexData], culturaBr, DateTimeStyles.None, out var dataMov))
                        {
                            if (dataMov >= dataMaisRecente)
                            {
                                if (decimal.TryParse(colunas[indexPu], NumberStyles.Number, culturaBr, out var puMed))
                                {
                                    dataMaisRecente = dataMov;
                                    ultimoPuEncontrado = puMed;
                                }
                            }
                        }
                    }
                }

                return ultimoPuEncontrado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar Título ISIN: {Isin}", codigoIsin);
                return null;
            }
        }


        public async Task<(decimal Preco, decimal UltimoRendimento)?> ObterDadosFiiAsync(string ticker)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("StatusInvest");

                decimal preco = 0;
                var jsonPreco = await client.GetStringAsync($"fii/tickerprice?ticker={ticker}&type=1&currences=1");

                using (JsonDocument doc = JsonDocument.Parse(jsonPreco))
                {
                    var root = doc.RootElement;
                    if (root.GetArrayLength() > 0)
                    {
                        var pricesArray = root[0].GetProperty("prices");
                        if (pricesArray.GetArrayLength() > 0)
                        {
                            var ultimoPreco = pricesArray[pricesArray.GetArrayLength() - 1];
                            preco = ultimoPreco.GetProperty("price").GetDecimal();
                        }
                    }
                }

                decimal rendimento = 0;
                var earningsResponse = await client.GetFromJsonAsync<StatusInvestEarningsRoot>($"fii/getearnings?Start={DateTime.Now.AddMonths(-2):yyyy-MM-dd}&End={DateTime.Now:yyyy-MM-dd}&Filter={ticker}");

                if (earningsResponse?.DateCom != null)
                {
                    var ultimoProvento = earningsResponse.DateCom
                        .Where(x => x.EarningType == "Rendimento")
                        .OrderByDescending(x => x.RankDateCom)
                        .FirstOrDefault();

                    if (ultimoProvento != null)
                    {
                        rendimento = ParseDecimal(ultimoProvento.ResultAbsoluteValue);
                    }
                }

                return (preco, rendimento);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter dados FII: {Ticker}", ticker);
                return null;
            }
        }

        private static decimal ParseDecimal(string valor)
        {
            if (string.IsNullOrEmpty(valor)) return 0;
            valor = valor.Trim();

            if (valor.Contains('.') && !valor.Contains(','))
                if (decimal.TryParse(valor, NumberStyles.Any, CultureInfo.InvariantCulture, out var rInv)) return rInv;

            if (decimal.TryParse(valor, NumberStyles.Any, new CultureInfo("pt-BR"), out var rBr)) return rBr;

            return 0;
        }
    }

    public class BcbSerieDto
    {
        [JsonPropertyName("data")] public string Data { get; set; }
        [JsonPropertyName("valor")] public string Valor { get; set; } // Vem como string
    }

    public class StatusInvestBondDto
    {
        [JsonPropertyName("sellprice")] public decimal SellPrice { get; set; } // Vem como number
        [JsonPropertyName("date")] public string Date { get; set; }
    }

    public class StatusInvestEarningsRoot
    {
        [JsonPropertyName("dateCom")] public List<StatusInvestEarningDto> DateCom { get; set; }
    }

    public class StatusInvestEarningDto
    {
        [JsonPropertyName("earningType")] public string EarningType { get; set; }
        [JsonPropertyName("resultAbsoluteValue")] public string ResultAbsoluteValue { get; set; } // Vem como string "0,85"
        [JsonPropertyName("rankDateCom")] public int RankDateCom { get; set; }
    }
}