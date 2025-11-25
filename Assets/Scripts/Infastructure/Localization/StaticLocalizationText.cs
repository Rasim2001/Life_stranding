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
        private TextMeshProUGUI _text;
        private ILocalizationService _localizationService;

        [Inject]
        public void Construct(IStaticDataService staticDataService, ILocalizationService localizationService)
        {
            _localizationService = localizationService;
            _staticDataService = staticDataService;
        }

        private void Awake() =>
            _text = GetComponent<TextMeshProUGUI>();

        private void Start()
        {
            LocalizationText localizationText = _staticDataService.WindowsLocalizationStaticData.Texts[_textStaticId];
            _text.text = localizationText.Get(_localizationService.CurrentLanguage);
        }
    }
}