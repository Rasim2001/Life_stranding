using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Infastructure.CutScene.Custom.Markers
{
    public class ResetAllTargetsMarker : Marker, INotification, INotificationOptionProvider
    {
        public PropertyName id => new(nameof(ResetAllTargetsMarker));

        [SerializeField, HideInInspector] private string _bindingKey;

        public NotificationFlags flags =>
            NotificationFlags.TriggerOnce | NotificationFlags.Retroactive;

#if UNITY_EDITOR
        private void RegenerateBindingKey()
        {
            _bindingKey = $"TransformTarget_{Guid.NewGuid():N}";
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_bindingKey))
                RegenerateBindingKey();
        }
#endif
    }
}