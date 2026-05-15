using System;

namespace Infastructure.Services.CursorVisible
{
    public interface ICursorVisibleService
    {
        void ShowCursor(object owner);
        void HideCursor(object owner);
        void Initialize();
        event Action OnHideCursorHappened;
    }
}