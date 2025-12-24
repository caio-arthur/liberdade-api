using System.Threading.Tasks;

namespace Application.Common.Interfaces
{
    public interface ITranslationService
    {
        Task<string> TranslateTextAsync(string text, string targetLanguage, string sourceLanguage);
    }
}
