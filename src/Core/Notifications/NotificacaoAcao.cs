using System.Text.Json.Serialization;

namespace Core.Notifications
{
    public class NotificacaoAcao
    {
        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("label")]
        public string Label { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("clear")]
        public bool Clear { get; set; } = false;

        [JsonPropertyName("method")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Method { get; set; }

        [JsonPropertyName("body")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Body { get; set; }

        [JsonPropertyName("headers")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string>? Headers { get; set; }

        public static NotificacaoAcao CreateView(string label, string url, bool clear = false)
        {
            return new NotificacaoAcao { Action = "view", Label = label, Url = url, Clear = clear };
        }

        public static NotificacaoAcao CreateHttp(string label, string url, string body, string method = "POST")
        {
            return new NotificacaoAcao { Action = "http", Label = label, Url = url, Body = body, Method = method };
        }
    }
}
