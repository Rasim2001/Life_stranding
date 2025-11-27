using System;
using Localization;

namespace Infastructure.Localization
{
    public class LocalizationService : ILocalizationService
    {
        public event Action OnLanguageChanged;
        public LanguageId CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage == value)
                    return;

                _currentLanguage = value;

                OnLanguageChanged?.Invoke();
            }
        }
        private LanguageId _currentLanguage = LanguageId.EN;
    }
}