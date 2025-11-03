using PickupObjects;
using UI;

namespace Infastructure.Services.Window
{
    public interface IWindowService
    {
        void OpenStartSplashScreen();
        void OpenSettingsPopup();
        void OpenSettingsScreen();
        void OpenProductDescriptionPopup(ProductType productType);
        void OpenTaskPopup(TaskId taskId);
        void OpenDefeatPopup();
    }
}