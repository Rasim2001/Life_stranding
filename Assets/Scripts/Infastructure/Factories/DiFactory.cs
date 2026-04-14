using Zenject;

namespace Infastructure.Factories
{
    public class DiFactory : IDiFactory
    {
        private readonly IInstantiator _instantiator;

        public DiFactory(IInstantiator instantiator) =>
            _instantiator = instantiator;

        public T Create<T>() where T : class =>
            _instantiator.Instantiate<T>();

        public T Create<T>(params object[] args) where T : class =>
            _instantiator.Instantiate<T>(args);
    }
}