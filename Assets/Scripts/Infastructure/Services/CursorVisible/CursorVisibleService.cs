using System;
using UnityEngine;

namespace Infastructure.Services.CursorVisible
{
    public class CursorVisibleService : ICursorVisibleService
    {
        public event Action OnHideCursorHappened;

        public void ShowCursor() =>
            Cursor.visible = true;

        public void HideCursor()
        {
            OnHideCursorHappened?.Invoke();

            Cursor.visible = false;
        }

        public void Initialize() =>
            Cursor.lockState = CursorLockMode.Confined;
    }
}