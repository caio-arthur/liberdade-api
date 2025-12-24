using Application.Common.Interfaces;
using Google.Cloud.Translation.V2;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class GoogleTranslationService : ITranslationService
    {
        private readonly TranslationClient _translationClient;

        public GoogleTranslationService(TranslationClient translationClient)
        {
            _translationClient = translationClient;
        }

        public Task<string> TranslateTextAsync(string text, string targetLanguage, string sourceLanguage)
        {
            var response = _translationClient.TranslateText(text, targetLanguage, sourceLanguage);
            return Task.FromResult(response.TranslatedText);
        }
    }
}
