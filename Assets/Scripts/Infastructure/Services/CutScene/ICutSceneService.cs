using System;

namespace Infastructure.Services.CutScene
{
    public interface ICutSceneService
    {
        bool IsActive { get; set; }
        float LerpForwardSpeed { get; set; }
        bool HasPlayed { get; set; }
        Action OnWeatherChanged { get; set; }
        event Action<bool> OnCutsceneActiveChanged;
        event Action OnSkipHappened;
        void Skip();
        void Clear();
    }
}