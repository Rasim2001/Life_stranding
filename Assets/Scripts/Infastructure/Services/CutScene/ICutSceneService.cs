using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infastructure.CutScenes;

namespace Infastructure.Services.CutScene
{
    public interface ICutSceneService
    {
        bool IsActive { get; set; }
        CutsceneId CutsceneId { get; }
        event Action<bool> OnCutsceneActiveChanged;
        event Action OnSkipHappened;
        UniTask StartCutsceneAsync(CutsceneId cutsceneId, CancellationToken ct = default);
    }
}