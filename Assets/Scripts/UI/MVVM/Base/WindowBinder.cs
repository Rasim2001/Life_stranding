using UnityEngine;

namespace UI.MVVM.Base
{
    public abstract class WindowBinder<T> : MonoBehaviour, IWindowBinder where T : WindowViewModel
    {
        protected T ViewModel;

        public void Bind(WindowViewModel viewModel)
        {
            ViewModel = (T)viewModel;

            OnBind(ViewModel);
        }

        public virtual void Close() =>
            Destroy(gameObject);

        protected virtual void OnBind(T viewModel)
        {
        }
    }
}