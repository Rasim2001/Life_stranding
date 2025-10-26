using Infastructure.StaticData.Product;
using Infastructure.StaticData.StaticDataService;
using PickupObjects;
using UI.MVVM.View.ProductDescriptionPopup;
using UI.MVVM.View.Root;
using UI.MVVM.View.SettingsPopup;
using UI.MVVM.View.SettingsScreen;
using UI.MVVM.View.StartSplashScreen;

namespace Infastructure.Services.Window
{
    public class WindowService : IWindowService
    {
        private readonly UIGameplayRootViewModel _gamePlayViewModel;
        private readonly IStaticDataService _staticDataService;

        public WindowService(UIGameplayRootViewModel gamePlayViewModel, IStaticDataService staticDataService)
        {
            _staticDataService = staticDataService;
            _gamePlayViewModel = gamePlayViewModel;
        }

        public void OpenStartSplashScreen()
        {
            StartSplashScreenViewModel viewModel = new StartSplashScreenViewModel(this);

            _gamePlayViewModel.OpenScreen(viewModel);
        }

        public void OpenSettingsPopup()
        {
            SettingsPopupViewModel model = new SettingsPopupViewModel();

            _gamePlayViewModel.OpenPopup(model);
        }

        public void OpenSettingsScreen()
        {
            SettingsScreenViewModel model = new SettingsScreenViewModel(this);

            _gamePlayViewModel.OpenScreen(model);
        }

        public void OpenProductDescriptionPopup(ProductType productType)
        {
            ProductData productData = _staticDataService.ProductsStaticData.ProductsDictionary[productType];

            ProductDescriptionPopupViewModel model =
                new ProductDescriptionPopupViewModel(productData.ProductDescription);

            _gamePlayViewModel.OpenPopup(model);
        }
    }
}