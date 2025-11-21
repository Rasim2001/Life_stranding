using Unity.VisualScripting;
using UnityEngine;

namespace Infastructure.Services.CursorVisible
{
    public class CursorVisibleService : ICursorVisibleService
    {
        public void ShowCursor() =>
            Cursor.visible = true;

        public void HideCursor() =>
            Cursor.visible = false;

        public void Initialize()
        {
            Debug.Log("Initialize");

            Cursor.lockState = CursorLockMode.Confined;
        }
    }
}