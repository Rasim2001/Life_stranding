using UnityEngine;

namespace Infastructure.World
{
    /// <summary>
    /// Метка живой ветки (docs/scene-regulations.md §6). Сохраняется выключенной;
    /// включается один раз, разом для всех веток, из SegmentActivation.ActivateAll().
    /// </summary>
    [DisallowMultipleComponent]
    public class SegmentLiveRoot : MonoBehaviour
    {
    }
}
