namespace SpiderController.StateMachine
{
    public interface ISpiderState
    {
        void Enter();
        void Exit();
        void HandleInput();
        void Update();
        void FixedUpdate();
        void LateUpdate();
    }
}