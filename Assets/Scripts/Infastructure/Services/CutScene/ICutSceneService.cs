using System;

namespace Infastructure.Services.CutScene
{
    public interface ICutSceneService
    {
        bool IsActive { get; set; }
        float LerpForwardSpeed { get; set; }
        event Action<bool> OnCutsceneActiveChanged;
    }
}