namespace SpiderController.StateMachine
{
    public interface ISpiderStateMachine
    {
        void SwitchState<T>() where T : ISpiderState;
        void HandleInput();
        void Update();
        void LateUpdate();
        bool IsCurrentState<T>() where T : ISpiderState;
    }
}