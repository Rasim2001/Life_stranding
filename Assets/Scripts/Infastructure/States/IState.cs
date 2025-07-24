namespace Infastructure.States
{
    public interface IState : IExitableState
    {
        void Enter();
    }
}