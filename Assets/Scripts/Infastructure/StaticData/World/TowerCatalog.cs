using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Infastructure.StaticData.World
{
    /// <summary>
    /// Единственный источник правды о составе столба.
    /// </summary>
    [CreateAssetMenu(fileName = "DATA_TowerCatalog", menuName = "StaticData/World/Tower Catalog")]
    public class TowerCatalog : ScriptableObject, ISceneReferenceOwner
    {
        [SerializeField] private SceneReference _bootstrapScene;
        [SerializeField] private SceneReference[] _residentScenes = System.Array.Empty<SceneReference>();
        [SerializeField] private SceneReference _entryScene;
        [SerializeField] private SegmentDefinition[] _segments = System.Array.Empty<SegmentDefinition>();
        [SerializeField] private LoadPolicy _policy = LoadPolicy.KeepAllLoaded;
        [SerializeField] private string _levelDataKey;
        [SerializeField] private bool _showsFirstEncounter;

        /// <summary>Сцена загрузчика, всегда индекс 0 в списке сборки.</summary>
        public SceneReference BootstrapScene => _bootstrapScene;

        /// <summary>Резидентный слой и служебные сцены, грузимые по имени (например, ExitGameLoop).</summary>
        public IReadOnlyList<SceneReference> ResidentScenes => _residentScenes;

        /// <summary>Грузится Single.</summary>
        public SceneReference EntryScene => _entryScene;

        /// <summary>
        /// Отсортированное представление. Сегодня отдаёт список как есть — порядок
        /// сегментов по высотной полосе не реализован (scene-architecture.md §7.6,
        /// тикет 08 заменит тело этого свойства сортировкой по полосе).
        /// Публичного доступа к сериализованному списку нет: переставить элементы
        /// в инспекторе и повлиять на поведение нельзя.
        /// </summary>
        public IReadOnlyList<SegmentDefinition> Segments => _segments;

        /// <summary>Выгрузки нет по решению спека; второе значение появится только после замеров.</summary>
        public LoadPolicy Policy => _policy;

        public string LevelDataKey => _levelDataKey;

        /// <summary>Глобальный рубильник попапов первой встречи (spec §10).</summary>
        public bool ShowsFirstEncounter => _showsFirstEncounter;

        public IEnumerable<SceneReference> SceneReferences
        {
            get
            {
                yield return _bootstrapScene;
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
