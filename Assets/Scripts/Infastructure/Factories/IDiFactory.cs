namespace Infastructure.Factories
{
    public interface IDiFactory
    {
        T Create<T>() where T : class;
        T Create<T>(params object[] args) where T : class;
    }
}