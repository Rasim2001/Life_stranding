using System.Collections.Generic;
using Infastructure.Factories.GameFactories;
using UnityEngine;
using Zenject;

namespace UI.MVVM.Base
{
    public class WindowsContainer : MonoBehaviour
    {
        [SerializeField] private Transform _screensContainer;
        [SerializeField] private Transform _popupsContainer;

        private readonly Dictionary<WindowViewModel, IWindowBinder> _openedPopupBinders = new();

        private IWindowBinder _openedScreenBinder;
        private IGameUIFactory _gameUIFactory;

        [Inject]
        public void Construct(IGameUIFactory gameUIFactory) =>
            _gameUIFactory = gameUIFactory;

        public void OpenPopup(WindowViewModel viewModel)
        {
            IWindowBinder binder = _gameUIFactory.CreateWindow(viewModel, _popupsContainer);
            _openedPopupBinders.Add(viewModel, binder);
        }

        public void ClosePopup(WindowViewModel popupViewModel)
        {
            IWindowBinder binder = _openedPopupBinders[popupViewModel];

            binder?.Close();
            _openedPopupBinders.Remove(popupViewModel);
        }

        public void OpenScreen(WindowViewModel viewModel)
        {
            _openedScreenBinder?.Close();

            if (viewModel == null)
                return;

            IWindowBinder binder = _gameUIFactory.CreateWindow(viewModel, _screensContainer);
            _openedScreenBinder = binder;
        }
    }
}