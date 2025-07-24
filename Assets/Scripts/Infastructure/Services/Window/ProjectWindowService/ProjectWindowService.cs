using Infastructure.Factories.ProjectFactories;
using Infastructure.StaticData.Windows;

namespace Infastructure.Services.Window.ProjectWindowService
{
    public class ProjectWindowService : IProjectWindowService
    {
        private readonly IProjectUIFactory _projectUIFactory;

        public ProjectWindowService(IProjectUIFactory projectUIFactory) =>
            _projectUIFactory = projectUIFactory;

        public void Open(WindowId windowId)
        {
            switch (windowId)
            {
                /*
                case WindowId.MainMenuWindow:
                    _projectUIFactory.CreateMenuWindow(WindowId.MainMenuWindow);
                    break;
                    */

                /*
                case WindowId.AuthenticationWindow:
                    _projectUIFactory.CreateMenuWindow(WindowId.AuthenticationWindow);
                    break;
            */
            }
        }
    }
}