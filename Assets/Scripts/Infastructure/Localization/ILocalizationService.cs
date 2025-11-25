using Localization;

namespace Infastructure.Localization
{
    public interface ILocalizationService
    {
        LanguageId CurrentLanguage { get; set; }
    }
}