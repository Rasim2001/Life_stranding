using System;
using Common.Lock;
using UnityEngine;

namespace Infastructure.Services.CursorVisible
{
    public class CursorVisibleService : ICursorVisibleService
    {
        public event Action OnHideCursorHappened;

        private readonly LockHandler _lockHandler = new LockHandler();

        public void ShowCursor(object owner)
        {
            _lockHandler.Acquire(owner, () => Cursor.visible = true);
        }

        public void HideCursor(object owner)
        {
            _lockHandler.Release(owner, onAction: () =>
            {
                Cursor.visible = false;
                OnHideCursorHappened?.Invoke();
            });
        }

        public void Initialize() =>
            Cursor.lockState = CursorLockMode.Confined;
    }
}