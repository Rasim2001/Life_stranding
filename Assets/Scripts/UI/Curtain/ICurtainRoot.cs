namespace UI.Curtain
{
    public interface ICurtainRoot
    {
        void ShowAndHide();
        void Show();
        void Hide();
        bool IsShowing { get; }
        void FandeIn(float time);
    }
}