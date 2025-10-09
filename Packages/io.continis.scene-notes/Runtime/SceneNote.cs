using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SceneNotes
{
    [CreateAssetMenu(fileName = "SceneNote", menuName = "Scene Notes/Note")]
    public partial class SceneNote : ScriptableObject
    {
        // Data visualised in the Inspector, editable
        public string title;
        public string contents;
        [FormerlySerializedAs("status")] public State state;
        [FormerlySerializedAs("position")] public Vector3 worldPosition;
        public Vector3 localOffset;
        public int categoryId;
        public List<Texture2D> screenshots;

        // Not shown
        public string connectedObjectGlobalObjectID;
        public string suffix;

        public Transform ConnectedTransform { get; set; }

        // Visualised, non-editable
        public SceneNoteBehaviour referencingNoteBehaviour;
        public string author;
        public string creationDate;
        public string modifiedDate;

        public NoteComment[] comments;

        // State enum and conversion functions
        public enum State
        {
            NotStarted,
            InProgress,
            Done,
        }

    }

    [Serializable]
    public struct NoteComment
    {
        public string author;
        public string contents;
        public string date;
    }
}