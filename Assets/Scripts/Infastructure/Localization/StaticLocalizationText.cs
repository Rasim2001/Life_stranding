using System;
using Infastructure.StaticData.StaticDataService;
using Localization;
using TMPro;
using UnityEngine;
using Zenject;

namespace Infastructure.Localization
{
    public class LocalizationStaticText : MonoBehaviour
    {
        [SerializeField] private TextStaticId _textStaticId;

        private IStaticDataService _staticDataService;
        private ILocalizationService _localizationService;

        private TextMeshProUGUI _text;
        private LocalizationText _localizationText;

        [Inject]
        public void Construct(IStaticDataService staticDataService, ILocalizationService localizationService)
        {
            _localizationService = localizationService;
            _staticDataService = staticDataService;
        }

        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
            _localizationText = _staticDataService.WindowsLocalizationStaticData.Texts[_textStaticId];

            UpdateLocalizationText();
        }

        private void Start() =>
            _localizationService.OnLanguageChanged += UpdateLocalizationText;

        private void OnDestroy() =>
            _localizationService.OnLanguageChanged -= UpdateLocalizationText;


        private void UpdateLocalizationText() =>
            _text.text = _localizationText.Get(_localizationService.CurrentLanguage);
    }
}