using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Infastructure.Common;
using Infastructure.Services.CurrentLevel;
using Infastructure.StaticData;
using Infastructure.StaticData.StaticDataService;
using Infastructure.StaticData.World;
using UnityEngine;
using Zenject;
using Debug = UnityEngine.Debug;

namespace Infastructure.World
{
    public interface ISegmentLoadingDirector
    {
        UniTask LoadInitialAsync();
        void Track(Transform target);
    }

    /// <summary>
    /// Загружает этажи столба снизу вверх, без пропусков, по мере приближения паука
    /// к нижней границе следующего незагруженного этажа (тикет 08, scene-architecture.md
    /// §7.6, §9.17). Выгрузки нет ни в каком виде — решение спека, не упущение.
    /// Живёт в ProjectContext (ProjectInstaller): ProjectContext держит свой Kernel,
    /// поэтому ITickable здесь работает так же, как в SceneContext.
    /// </summary>
    public class SegmentLoadingDirector : ISegmentLoadingDirector, ITickable
    {
        private readonly ISceneLoader _sceneLoader;
        private readonly IStaticDataService _staticDataService;
        private readonly ICurrentLevelService _currentLevel;

        private IReadOnlyList<SegmentDefinition> _orderedSegments = Array.Empty<SegmentDefinition>();
        private int _loadedCount;
        private bool _isLoading;
        private Transform _target;

        public SegmentLoadingDirector(ISceneLoader sceneLoader, IStaticDataService staticDataService,
            ICurrentLevelService currentLevel)
        {
            _sceneLoader = sceneLoader;
            _staticDataService = staticDataService;
            _currentLevel = currentLevel;
        }

        /// <summary>Пушится из BuildLevelState после InitSpider. До вызова Tick ничего не делает.</summary>
        public void Track(Transform target) => _target = target;

        public async UniTask LoadInitialAsync()
        {
            _orderedSegments = _staticDataService.GameStaticData.TowerCatalog.Segments;

            float spawnY = ResolveSpawnY();

            int targetCount = _orderedSegments.Count;
            for (int i = 0; i < _orderedSegments.Count; i++)
            {
                SegmentBakedData baked = _orderedSegments[i].Baked;
                if (baked != null && baked.IsValid && spawnY < baked.TopY)
                {
                    targetCount = i + 1;
                    break;
                }
            }

            await LoadUpToAsync(targetCount);
        }

        public void Tick()
        {
            if (_isLoading || _target == null)
                return;

            if (_loadedCount == 0 || _loadedCount >= _orderedSegments.Count)
                return;

            SegmentBakedData baked = _orderedSegments[_loadedCount].Baked;
            if (baked == null || !baked.IsValid)
                return;

            float lead = _staticDataService.GameStaticData.TowerCatalog.PreloadLeadMeters;
            if (_target.position.y + lead >= baked.BottomY)
                LoadUpToAsync(_loadedCount + 1).Forget();
        }

        private float ResolveSpawnY()
        {
            string key = _currentLevel.LevelDataKey;
            if (_staticDataService.GameStaticData.GameDatas.TryGetValue(key, out GameData data) &&
                data.SpiderSpawnData != null)
                return data.SpiderSpawnData.WorldPosition.y;

            return float.NegativeInfinity;
        }

        private async UniTask LoadUpToAsync(int targetCount)
        {
            if (targetCount <= _loadedCount || _isLoading)
                return;

            _isLoading = true;

            try
            {
                while (_loadedCount < targetCount && _loadedCount < _orderedSegments.Count)
                {
                    await LoadSegmentAsync(_orderedSegments[_loadedCount]);
                    _loadedCount++;
                }
            }
            finally
            {
                _isLoading = false;
            }
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
