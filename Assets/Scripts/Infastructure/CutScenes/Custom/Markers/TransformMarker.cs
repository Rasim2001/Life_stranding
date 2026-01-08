using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Infastructure.CutScenes.Custom.Markers
{
    [Serializable]
    public class TransformMarker : Marker, INotification, INotificationOptionProvider
    {
        public PropertyName id => new(nameof(TransformMarker));

        [Tooltip("Scene target resolved via PlayableDirector")]
        public ExposedReference<Transform> Target;

        [SerializeField, HideInInspector] private string _bindingKey;

        public NotificationFlags flags =>
            NotificationFlags.TriggerOnce | NotificationFlags.Retroactive;

#if UNITY_EDITOR
        public void RegenerateBindingKey()
        {
            _bindingKey = $"TransformTarget_{Guid.NewGuid():N}";
            Target.exposedName = new PropertyName(_bindingKey);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_bindingKey) || Target.exposedName == null)
                RegenerateBindingKey();
        }
#endif
    }
}