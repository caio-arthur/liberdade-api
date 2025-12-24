using System.Text.Json.Serialization;

namespace Core.Notifications
{
    public class NotificacaoMensagem
    {
        [JsonPropertyName("topic")]
        public string Topic { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("priority")]
        public int Priority { get; set; } = (int)NotificacaoPrioridade.Default;

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();

        [JsonPropertyName("markdown")]
        public bool Markdown { get; set; } = true; 

        [JsonPropertyName("actions")]
        public List<NotificacaoAcao> Actions { get; set; } = new();
    }
}
