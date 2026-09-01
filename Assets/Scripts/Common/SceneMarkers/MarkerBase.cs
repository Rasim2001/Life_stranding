using Common;
using UnityEngine;

namespace Common.SceneMarkers
{
    [RequireComponent(typeof(MarkerUniqueId))]
    public abstract class MarkerBase : MonoBehaviour
    {
        public string UniqueId => GetComponent<MarkerUniqueId>()?.UniqueId;
    }
}
