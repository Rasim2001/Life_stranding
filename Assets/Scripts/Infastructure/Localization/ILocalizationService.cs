using System;
using Localization;

namespace Infastructure.Localization
{
    public interface ILocalizationService
    {
        LanguageId CurrentLanguage { get; set; }
        event Action OnLanguageChanged;
    }
}