using Infastructure.Services.Pause;
using TMPro;
using UI.MVVM.Base;
using UnityEngine;
using Zenject;

namespace UI.MVVM.View.ProductDescriptionPopup
{
    public class ProductDescriptionPopupBinder : PopupBinder<ProductDescriptionPopupViewModel>
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _howToUseText;
        [SerializeField] private TextMeshProUGUI _descriptionText;

        private IPauseService _pauseService;

        [Inject]
        public void Construct(IPauseService pauseService) =>
            _pauseService = pauseService;

        protected override void Start()
        {
            base.Start();

            _pauseService.StartPause();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _pauseService.StopPause();
        }

        protected override void OnBind(ProductDescriptionPopupViewModel viewModel)
        {
            _titleText.text = viewModel.Description.TitleText;
            _howToUseText.text = viewModel.Description.HowToUseText;
            _descriptionText.text = viewModel.Description.DescriptionText;
        }
    }
}