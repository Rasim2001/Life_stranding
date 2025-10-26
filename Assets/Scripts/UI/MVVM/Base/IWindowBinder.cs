namespace UI.MVVM.Base
{
    public interface IWindowBinder
    {
        void Bind(WindowViewModel viewModel);
        void Close();
    }
}