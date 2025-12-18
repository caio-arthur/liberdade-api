using Application.Common.Interfaces;
using OpenAI;
using OpenAI.Chat;
using Microsoft.Extensions.Configuration;
using System.ClientModel;

namespace Infrastructure.Services
{
    public class OpenRouterAgentService : IAgenteFinanceiroService
    {
        private readonly string _apiKey;
        private readonly string _model = "tngtech/deepseek-r1t2-chimera:free";
        private readonly Uri _endpoint = new Uri("https://openrouter.ai/api/v1");

        public OpenRouterAgentService(IConfiguration configuration)
        {
            _apiKey = configuration["OpenRouter:ApiKey"];
        }

        public async Task<string> GerarRelatorioDiarioAsync(ContextoFinanceiroDto contexto)
        {
            var options = new OpenAIClientOptions { Endpoint = _endpoint };
            var client = new OpenAIClient(new ApiKeyCredential(_apiKey), options);
            var chatClient = client.GetChatClient(_model);

            var promptContexto = $@"
                DADOS FINANCEIROS DO DIA:
                Nome da destinatária: {contexto.NomeConjuge}
                Fase atual: {contexto.FaseAtual}
                Patrimônio total: R$ {contexto.PatrimonioTotal:N2}
                Variação diária: R$ {contexto.VariacaoDiaria:N2}
                Renda passiva: R$ {contexto.RendaAtual:N2}
                Meta de renda passiva: R$ {contexto.MetaRenda:N2}
                Últimas movimentações: {string.Join(", ", contexto.UltimasMovimentacoes)}";

            var systemInstruction = $@"
                PAPEL:
                Você é {contexto.NomeUsuario}, enviando diariamente uma mensagem para sua esposa sobre os investimentos.

                PERSONALIDADE:
                Tom simpático, calmo e confiante.
                Motivacional sem exageros.
                Extremamente objetivo e direto.

                OBJETIVO:
                Analisar os dados financeiros fornecidos e gerar um relatório diário curto.

                REGRAS OBRIGATÓRIAS:
                - Máximo de 3 frases.
                - Texto corrido (sem títulos, listas ou quebras de linha).
                - Sempre mencionar:
                  • Patrimônio total.
                  • Progresso da renda passiva em relação à meta.
                - Se o patrimônio subiu: comemore de forma discreta.
                - Se caiu: reforce a visão de longo prazo.
                - Use no máximo 1 ou 2 emojis, apenas se fizer sentido (ex: 📈 ou 📉).
                - Não invente dados, não faça perguntas, não dê conselhos extensos.";

            var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(systemInstruction),
                    new UserChatMessage(promptContexto)
                };

            ChatCompletion completion = await chatClient.CompleteChatAsync(messages);

            return completion.Content[0].Text;
        }
    }
}