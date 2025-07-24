using Zenject;

namespace Infastructure.Factories.ProjectFactories
{
    public class ProjectUIFactory : IProjectUIFactory
    {
        private readonly DiContainer _diContainer;

        public ProjectUIFactory(DiContainer diContainer) =>
            _diContainer = diContainer;
    }
}