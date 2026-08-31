using UnityEngine;

namespace Infastructure.World
{
    /// <summary>
    /// Контракт сцены сегмента: авторская высотная полоса этажа. Единственное редактируемое
    /// место — SegmentBakedData генерируется отсюда (Editor.World.SegmentBands, тикет 08,
    /// scene-architecture.md §7.6). Перекрытие смежных полос законно, отдельного поля под
    /// него нет.
    /// </summary>
    public class SegmentBounds : MonoBehaviour
    {
        [SerializeField] private float _bottomY;
        [SerializeField] private float _topY;

        public float BottomY => _bottomY;
        public float TopY => _topY;

        public bool IsValid => _topY > _bottomY;
    }
}
