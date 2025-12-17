using Application.Common.Interfaces;
using Core.Notifications;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace Infrastructure.Services
{
    public class NtfyConfigs
    {
        public string BaseUrl { get; set; }
        public string DefaultTopic { get; set; }
    }

    public class NtfyNotificacaoService : INotificacaoService
    {
        private readonly HttpClient _httpClient;
        private readonly NtfyConfigs _configs;

        public NtfyNotificacaoService(HttpClient httpClient, IOptions<NtfyConfigs> settings)
        {
            _httpClient = httpClient;
            _configs = settings.Value;
        }

        public async Task EnviarAsync(NotificacaoMensagem mensagem)
        {
            if (string.IsNullOrEmpty(mensagem.Topic))
            {
                mensagem.Topic = _configs.DefaultTopic;
            }

            var response = await _httpClient.PostAsJsonAsync(_configs.BaseUrl, mensagem);

            response.EnsureSuccessStatusCode();

        }
    }
}
