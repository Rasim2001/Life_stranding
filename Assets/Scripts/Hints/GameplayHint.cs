using System.Collections;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Infastructure.Localization;
using Infastructure.Services.Hint;
using Infastructure.StaticData.StaticDataService;
using Localization;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using Zenject;

namespace Hints
{
    public class GameplayHint : HintBase
    {
        private const float ShowTime = 2;
        private const float AnchorPositionX = -20;

        [SerializeField] private TextMeshProUGUI _description;

        private IHintReceiverService _hintReceiverService;
        private IStaticDataService _staticDataService;
        private ILocalizationService _localizationService;


        [Inject]
        public void Construct(IHintReceiverService hintReceiverService, IStaticDataService staticDataService,
            ILocalizationService localizationService)
        {
            _localizationService = localizationService;
            _staticDataService = staticDataService;
            _hintReceiverService = hintReceiverService;
        }

        protected override void Start()
        {
            base.Start();

            _hintReceiverService.OnProductHint += ShowProduct;
            _hintReceiverService.OnCheckpointHint += ShowCheckpointHint;
            _hintReceiverService.OnGeneratorHint += ShowGeneratorHint;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _hintReceiverService.OnProductHint -= ShowProduct;
            _hintReceiverService.OnCheckpointHint -= ShowCheckpointHint;
            _hintReceiverService.OnGeneratorHint -= ShowGeneratorHint;
        }

        private void ShowProduct()
        {
            _description.text = GetHintText(TextStaticId.Product_HintPopup);

            Show(ShowTime, AnchorPositionX);
        }

        private void ShowCheckpointHint()
        {
            _description.text = GetHintText(TextStaticId.Checkpoint_HintPopup);

            Show(ShowTime, AnchorPositionX);
        }

        private void ShowGeneratorHint()
        {
            _description.text = GetHintText(TextStaticId.Generator_HintPopup);

            Show(ShowTime, AnchorPositionX);
        }


        private string GetHintText(TextStaticId textStaticId)
        {
            LocalizationText localizationText =
                _staticDataService.WindowsLocalizationStaticData.Texts[textStaticId];
            return localizationText.Get(_localizationService.CurrentLanguage);
        }
    }
}