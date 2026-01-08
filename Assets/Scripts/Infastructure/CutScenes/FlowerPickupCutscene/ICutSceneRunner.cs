using System.Threading;
using Cysharp.Threading.Tasks;

namespace Infastructure.CutScenes.FlowerPickupCutscene
{
    public interface ICutSceneRunner
    {
        UniTask PlayAsync(CancellationToken ct = default);
        float BlendingTime { get; }
    }
}