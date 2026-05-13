using System;
using Infastructure.Services.CursorVisible;
using UnityEngine;
using Zenject;

namespace UI.MVVM.Base
{
    public abstract class WindowBinder<T> : MonoBehaviour, IWindowBinder where T : WindowViewModel
    {
        private ICursorVisibleService _cursorVisibleService;

        protected T ViewModel;

        [Inject]
        public void Construct(ICursorVisibleService cursorVisibleService) =>
            _cursorVisibleService = cursorVisibleService;

        public void Bind(WindowViewModel viewModel)
        {
            _cursorVisibleService.ShowCursor(this);

            ViewModel = (T)viewModel;

            OnBind(ViewModel);
        }

        public virtual void Close()
        {
            _cursorVisibleService.HideCursor(this);

            Destroy(gameObject);
        }

        protected virtual void OnBind(T viewModel)
        {
        }
    }
}