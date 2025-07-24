using Infastructure.StaticData.Windows;

namespace Infastructure.Services.Window
{
    public interface IWindowService
    {
        void Open(WindowId windowId);
    }
}