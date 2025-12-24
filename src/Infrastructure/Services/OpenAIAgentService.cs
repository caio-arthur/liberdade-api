using Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace Infrastructure.Services
{
    public class OpenRouterAgentService : IAgenteFinanceiroService
    {
        private readonly string _apiKey;
        private readonly string _model = "tngtech/deepseek-r1t2-chimera:free";
        private readonly Uri _endpoint = new("https://openrouter.ai/api/v1");

        public OpenRouterAgentService(IConfiguration configuration)
        {
            _apiKey = configuration["OpenRouter:ApiKey"];
        }

        public async Task<string> GerarRelatorioDiarioAsync(ContextoFinanceiroDto contexto)
        {
            var options = new OpenAIClientOptions { Endpoint = _endpoint };
            var client = new OpenAIClient(new ApiKeyCredential(_apiKey), options);
            var chatClient = client.GetChatClient(_model);

            string focoInstrucao;

            bool houveMovimentacao = contexto.UltimasMovimentacoes != null && contexto.UltimasMovimentacoes.Any();
            decimal percentualConcluido = contexto.MetaRenda > 0 ? (contexto.RendaAtual / contexto.MetaRenda) * 100 : 0;

            if (houveMovimentacao)
            {
                focoInstrucao = "PRIORITY FOCUS: There were NEW TRANSACTIONS (investments) YESTERDAY. Celebrate the fact that we bought new assets. List the transactions briefly and enthusiastically.";
            }
            else
            {
                int dayStrategy = DateTime.UtcNow.DayOfYear % 3;

                focoInstrucao = dayStrategy switch
                {
                    0 => $"FOCUS TOPIC: Highlight the 'Estimated Daily Passive Income' (R$ {contexto.RendimentoPassivoDiario:N2}). Explain that this is money generated automatically yesterday.",
                    1 => $"FOCUS TOPIC: Highlight the 'Current Monthly Passive Income' (R$ {contexto.RendaAtual:N2}). Reinforce how this amount covers part of the monthly expenses.",
                    _ => $"FOCUS TOPIC: Highlight the 'Goal Completion Percentage' ({percentualConcluido:N1}% completed). Focus on how close the finish line is getting."
                };
            }

            var transactionsString = houveMovimentacao
                ? string.Join(", ", contexto.UltimasMovimentacoes)
                : "No manual transactions yesterday.";

            var promptContexto = $@"
                TIMING CONTEXT:
                - Current Time: 9:00 AM (Morning).
                - Data Reference: These values refer to YESTERDAY'S market close (retrieved overnight).

                FINANCIAL DATA (REFERENCE: YESTERDAY):
                - Recipient Name: {contexto.NomeConjuge}
                - Sender Name: {contexto.NomeUsuario}
                - Total Net Worth: R$ {contexto.PatrimonioTotal:N2}
                - Daily Variation: R$ {contexto.VariacaoPatrimonialDiaria:N2} (Change from yesterday vs day before).
                
                GOALS STATUS:
                - Passive Income Goal: R$ {contexto.MetaRenda:N2} / month
                - Current Passive Income: R$ {contexto.RendaAtual:N2} / month
                - Progress: {percentualConcluido:N1}% achieved
                - Time Remaining: {contexto.MesesRestantes} months (Target: {contexto.DataEstimadaMeta:MM/yyyy})
                
                DAILY PERFORMANCE (YESTERDAY):
                - Estimated Daily Passive Income (The money produced yesterday): R$ {contexto.RendimentoPassivoDiario:N2}
                - Recent Transactions (Yesterday): {transactionsString}
                ";

            var systemInstruction = $@"
                ROLE:
                You are {contexto.NomeUsuario}, sending a daily text message to your wife, {contexto.NomeConjuge}. 
                You are strictly focused on Financial Independence (FIRE).

                TONE:
                - Affectionate but not cheesy (Use: 'Love', 'Honey', 'Darling').
                - Confident, stoic, and partnership-oriented ('We are building this', 'Our future').
                - Concise and direct.

                MANDATORY INSTRUCTIONS:
                1. {focoInstrucao} (THIS IS THE MOST IMPORTANT RULE).
                2. Always mention the 'Total Net Worth' value at the start or end.
                3. Length: Maximum 3 sentences. No exceptions.
                4. Formatting: Plain text only. NO Markdown, NO bold (**), NO headers.
                5. Emojis: Use maximum 1 or 2 relevant emojis.
                6. Time Context: It is 9 AM. You are reporting the results of YESTERDAY. 
                   - Use verbs in the past tense for gains (e.g., 'Yesterday we generated...', 'Last night closed at...').
                   - Do NOT imply the money was made this morning.
                7. When mentioning the remaining months ({contexto.MesesRestantes}), DO NOT use minimizing words like 'only', 'just', or 'barely'. Be objective.
                8. Language: Output MUST be in English.

                SCENARIO HANDLING:
                - If 'Daily Variation' is negative: Ignore the drop. Focus purely on the dividends/income generated (Daily Passive Income).
                - If 'Daily Variation' is positive: You may briefly celebrate the growth.
                ";

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