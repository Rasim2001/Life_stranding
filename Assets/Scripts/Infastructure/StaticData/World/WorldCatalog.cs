using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Infastructure.StaticData.World
{
    /// <summary>
    /// Единственный источник правды о составе мира.
    /// </summary>
    [CreateAssetMenu(fileName = "DATA_World_", menuName = "StaticData/World/World Catalog")]
    public class WorldCatalog : ScriptableObject, ISceneReferenceOwner
    {
        [SerializeField] private SceneReference _bootstrapScene;
        [SerializeField] private SceneReference[] _residentScenes = System.Array.Empty<SceneReference>();
        [SerializeField] private SceneReference _atmosphereScene;
        [SerializeField] private SceneReference _entryScene;
        [SerializeField] private SegmentDefinition[] _segments = System.Array.Empty<SegmentDefinition>();
        [SerializeField] private string _levelDataKey;
        [SerializeField] private bool _showsFirstEncounter;

        /// <summary>Сцена загрузчика, всегда индекс 0 в списке сборки.</summary>
        public SceneReference BootstrapScene => _bootstrapScene;

        /// <summary>Резидентный слой и служебные сцены, грузимые по имени (например, ExitGameLoop).</summary>
        public IReadOnlyList<SceneReference> ResidentScenes => _residentScenes;

        /// <summary>Сцена атмосферы (погодный риг, глобальный Volume). Грузится Single, становится активной.</summary>
        public SceneReference AtmosphereScene => _atmosphereScene;

        /// <summary>Грузится аддитивно поверх атмосферы.</summary>
        public SceneReference EntryScene => _entryScene;

        /// <summary>
        /// Отсортированное по возрастанию <c>Baked.BottomY</c> представление (scene-architecture.md
        /// §7.6). Сегменты без валидной запечённой полосы уходят в конец — их ловит
        /// ProjectScenesWindow.ValidateConfiguration. Пустые слоты массива (null после resize в
        /// инспекторе) отфильтрованы здесь — потребители вроде SegmentLoadingDirector полагаются
        /// на то, что список не содержит null. Публичного доступа к сериализованному списку нет:
        /// переставить элементы в инспекторе и повлиять на поведение нельзя.
        /// </summary>
        public IReadOnlyList<SegmentDefinition> Segments =>
            _segments
                .Where(s => s != null)
                .OrderBy(s => s.Baked != null && s.Baked.IsValid ? s.Baked.BottomY : float.MaxValue)
                .ToList();

        public string LevelDataKey => _levelDataKey;

        /// <summary>Глобальный рубильник попапов первой встречи (spec §10).</summary>
        public bool ShowsFirstEncounter => _showsFirstEncounter;

        public IEnumerable<SceneReference> SceneReferences
        {
            get
            {
                yield return _bootstrapScene;
                yield return _atmosphereScene;
                yield return _entryScene;

                if (_residentScenes == null)
                    yield break;

                foreach (SceneReference reference in _residentScenes)
                    yield return reference;
            }
        }

#if UNITY_EDITOR
        /// <summary>Мутаторы для окна GD Tools/Scenes. Наружу (в рантайм) не смотрят.</summary>
        public void SetEntryScene(UnityEditor.SceneAsset asset)
        {
            _entryScene ??= new SceneReference();
            _entryScene.SetAsset(asset);
        }

        public void SetAtmosphereScene(UnityEditor.SceneAsset asset)
        {
            _atmosphereScene ??= new SceneReference();
            _atmosphereScene.SetAsset(asset);
        }

        public void AddSegment(SegmentDefinition segment)
        {
            if (_segments.Contains(segment))
                return;

            _segments = _segments.Append(segment).ToArray();
        }

        public void RemoveSegment(SegmentDefinition segment) =>
            _segments = _segments.Where(s => s != segment).ToArray();

        public void SetShowsFirstEncounter(bool value) =>
            _showsFirstEncounter = value;
#endif
    }
}
