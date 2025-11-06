using PickupObjects;
using UI;

namespace Infastructure.Services.Window
{
    public interface IWindowService
    {
        void OpenStartSplashScreen();
        void OpenPausePopup();
        void OpenSettingsScreen();
        void OpenProductDescriptionPopup(ProductType productType);
        void OpenTaskPopup(TaskId taskId);
        void OpenDefeatPopup();
        void ClosePopup(string id);
        void TryOpenMainTaskPopup(bool isActive);
    }
}