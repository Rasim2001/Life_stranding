using System;
using System.Collections.Generic;
using System.Linq;
using ObservableCollections;
using R3;
using UnityEngine;

namespace UI.MVVM.Base
{
    public class UIRootViewModel : IDisposable
    {
        public ReadOnlyReactiveProperty<WindowViewModel> OpenedScreen => _openedScreen;
        public IObservableCollection<WindowViewModel> OpenedPopups => _openedPopups;

        private readonly ReactiveProperty<WindowViewModel> _openedScreen = new(null);
        private readonly ObservableList<WindowViewModel> _openedPopups = new();

        private readonly Dictionary<WindowViewModel, IDisposable> _popupSubscriptions = new();

        public void Dispose()
        {
            CloseAllPopups();

            _openedScreen.Value?.Dispose();
        }

        public bool CanOpenWindow() =>
            _openedPopups.Count == 0 && _openedScreen.Value == null;

        public void OpenScreen(WindowViewModel screenViewModel)
        {
            _openedScreen.Value?.Dispose();
            _openedScreen.Value = screenViewModel;

            screenViewModel.CloseRequested.Subscribe(CloseScreen);
        }


        public void OpenPopup(WindowViewModel popupViewModel)
        {
            if (_openedPopups.Contains(popupViewModel))
                return;

            IDisposable subscription = popupViewModel.CloseRequested.Subscribe(CloseLastPopup);
            _popupSubscriptions.Add(popupViewModel, subscription);
            _openedPopups.Add(popupViewModel);
        }

        public void ClosePopup(string popupId)
        {
            WindowViewModel openedPopupViewModel = _openedPopups.FirstOrDefault(p => p.Id == popupId);

            if (_openedPopups.Contains(openedPopupViewModel))
                ClosePopup(openedPopupViewModel);
        }

        public bool IsOpenedAnyWindow() =>
            _openedPopups.Count != 0 || _openedScreen.Value != null;

        private void CloseAllPopups()
        {
            foreach (WindowViewModel openedPopup in _openedPopups.ToList())
            {
                if (_openedPopups.Contains(openedPopup))
                    ClosePopup(openedPopup);
            }
        }

        private void CloseLastPopup(WindowViewModel popupViewModel)
        {
            if (_openedPopups.Contains(popupViewModel) && IsLastPopup(popupViewModel))
                ClosePopup(popupViewModel);
        }

        private void ClosePopup(WindowViewModel popupViewModel)
        {
            popupViewModel.Dispose();
            _openedPopups.Remove(popupViewModel);

            IDisposable popupSubscription = _popupSubscriptions[popupViewModel];
            popupSubscription?.Dispose();
            _popupSubscriptions.Remove(popupViewModel);
        }

        private void CloseScreen(WindowViewModel popupViewModel)
        {
            _openedScreen.Value.Dispose();
            _openedScreen.Value = null;
        }

        private bool IsLastPopup(WindowViewModel vm)
        {
            int count = _openedPopups.Count;
            if (count == 0)
                return false;

            int idx = _openedPopups.IndexOf(vm);
            return idx == count - 1;
        }
    }
}