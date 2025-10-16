#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace SceneNotes
{
    public partial class SceneNote : ScriptableObject
    {
        public SceneAsset scene;
    }
}

#endif