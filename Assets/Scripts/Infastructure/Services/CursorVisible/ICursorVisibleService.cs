using System;

namespace Infastructure.Services.CursorVisible
{
    public interface ICursorVisibleService
    {
        void ShowCursor();
        void HideCursor();
        void Initialize();
        event Action OnHideCursorHappened;
    }
}