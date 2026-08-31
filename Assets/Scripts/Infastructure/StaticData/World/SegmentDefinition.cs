using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Infastructure.StaticData.World
{
    /// <summary>
    /// Описание одного сегмента столба. Один ассет на сегмент, владелец — автор сегмента.
    /// Высотной полосы здесь нет — только ссылка на сгенерированный <see cref="SegmentBakedData"/>
    /// (см. scene-architecture.md §7.6, тикет 08).
    /// </summary>
    [CreateAssetMenu(fileName = "DATA_Segment", menuName = "StaticData/World/Segment Definition")]
    public class SegmentDefinition : ScriptableObject, ISceneReferenceOwner
    {
        [SerializeField] private SceneReference _scene;
        [SerializeField] private SceneReference[] _additionalScenes = System.Array.Empty<SceneReference>();
        [SerializeField] private SegmentBakedData _baked;

        public SceneReference Scene => _scene;
        public IReadOnlyList<SceneReference> AdditionalScenes => _additionalScenes;
        public SegmentBakedData Baked => _baked;

        public IEnumerable<SceneReference> SceneReferences =>
            _additionalScenes == null
                ? new[] { _scene }
                : new[] { _scene }.Concat(_additionalScenes);

#if UNITY_EDITOR
        /// <summary>Мутатор для окна GD Tools/Scenes. Наружу (в рантайм) не смотрит.</summary>
        public void SetScene(UnityEditor.SceneAsset asset)
        {
            _scene ??= new SceneReference();
            _scene.SetAsset(asset);
        }

        /// <summary>Мутатор для SegmentBands.Sync. Наружу (в рантайм) не смотрит.</summary>
        public void SetBaked(SegmentBakedData baked) => _baked = baked;
#endif
    }
}
