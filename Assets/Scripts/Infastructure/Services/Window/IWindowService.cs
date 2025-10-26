using PickupObjects;

namespace Infastructure.Services.Window
{
    public interface IWindowService
    {
        void OpenStartSplashScreen();
        void OpenSettingsPopup();
        void OpenSettingsScreen();
        void OpenProductDescriptionPopup(ProductType productType);
    }
}