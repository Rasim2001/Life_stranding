using Infastructure.Localization;
using Localization;
using UnityEngine;

namespace Infastructure.Services.QuitApplication
{
    public class QuitGameService : IQuitGameService
    {
        private const string GoogleFormURL_RU =
            "https://docs.google.com/forms/d/e/1FAIpQLSfsm5Mn2L-zDcGv_erMAK4GqCyS-s5gQNl5Q41TTrQ1v_1RmQ/viewform";

        private const string GoogleFormURL_EN =
            "https://docs.google.com/forms/d/e/1FAIpQLSfO4u6e_eUZlNH9YLHZFp-uCvDjuwa1IYy-zgyXBZuq8ktqgg/viewform?usp=send_form";


        private readonly ILocalizationService _localizationService;

        public QuitGameService(ILocalizationService localizationService) =>
            _localizationService = localizationService;

        public void QuitGame()
        {
            /*Application.OpenURL(_localizationService.CurrentLanguage == LanguageId.RU
                ? GoogleFormURL_RU
                : GoogleFormURL_EN);*/

            Application.Quit();
        }
    }
}