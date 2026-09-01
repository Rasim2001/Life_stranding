using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Infastructure.Common;
using Infastructure.StaticData;
using Infastructure.StaticData.StaticDataService;
using Infastructure.StaticData.World;
using Debug = UnityEngine.Debug;

namespace Infastructure.World
{
    public interface ISegmentLoadingDirector
    {
        UniTask LoadInitialAsync();
    }

    /// <summary>
    /// Грузит все сегменты мира снизу вверх на старте. Выгрузки и предзагрузки нет —
    /// решение спека, не упущение (docs/scene-regulations.md §3 «Сегменты столба»).
    /// </summary>
    public class SegmentLoadingDirector : ISegmentLoadingDirector
    {
        private readonly ISceneLoader _sceneLoader;
        private readonly IStaticDataService _staticDataService;

        public SegmentLoadingDirector(ISceneLoader sceneLoader, IStaticDataService staticDataService)
        {
            _sceneLoader = sceneLoader;
            _staticDataService = staticDataService;
        }

        public async UniTask LoadInitialAsync()
        {
            IReadOnlyList<SegmentDefinition> orderedSegments = _staticDataService.GameStaticData.WorldCatalog.Segments;

            foreach (SegmentDefinition segment in orderedSegments)
                await LoadSegmentAsync(segment);
        }

        private async UniTask LoadSegmentAsync(SegmentDefinition segment)
        {
            var stopwatch = Stopwatch.StartNew();

            foreach (SceneReference sceneReference in segment.SceneReferences)
            {
                if (sceneReference == null || !sceneReference.IsValid)
                    continue;

                await _sceneLoader.LoadAdditiveAsync(sceneReference.SceneName);
            }

            stopwatch.Stop();
            Debug.Log($"[SegmentDirector] '{segment.Scene?.SceneName}' loaded in {stopwatch.ElapsedMilliseconds} ms");
        }
    }
}
