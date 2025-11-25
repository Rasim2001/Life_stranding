using Localization;

namespace Infastructure.Localization
{
    public class LocalizationService : ILocalizationService
    {
        public LanguageId CurrentLanguage { get; set; } = LanguageId.RU;
    }
}