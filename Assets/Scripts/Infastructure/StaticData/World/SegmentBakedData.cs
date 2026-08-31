using UnityEngine;

namespace Infastructure.StaticData.World
{
    /// <summary>
    /// Сгенерированная высотная полоса сегмента. Не создаётся руками (нет
    /// [CreateAssetMenu]) — см. Editor.World.SegmentBands.Sync (тикет 08). Редактируемый
    /// источник — SegmentBounds в сцене сегмента.
    /// </summary>
    public class SegmentBakedData : ScriptableObject
    {
        [SerializeField] private float _bottomY;
        [SerializeField] private float _topY;
        [SerializeField] private string _sourceScene;

        public float BottomY => _bottomY;
        public float TopY => _topY;
        public string SourceScene => _sourceScene;

        public bool IsValid => _topY > _bottomY;

#if UNITY_EDITOR
        /// <summary>Сеттер для SegmentBands.Sync. Наружу (в рантайм) не смотрит.</summary>
        public void SetBand(float bottomY, float topY, string sourceScene)
        {
            _bottomY = bottomY;
            _topY = topY;
            _sourceScene = sourceScene;
        }
#endif
    }
}
