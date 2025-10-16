using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SceneNotes.Editor.CustomEditors
{
    [CustomEditor(typeof(SceneNoteBehaviour))]
    [CanEditMultipleObjects]
    public class SceneNoteBehaviourEditor : UnityEditor.Editor
    {
        private SceneNoteBehaviour _sceneNoteBehaviour;
        private Transform _transform;
        private Vector3 _lastPosition;

        private void OnEnable()
        {
            _sceneNoteBehaviour = (SceneNoteBehaviour)serializedObject.targetObject;
            _transform = _sceneNoteBehaviour.transform;
            _lastPosition = _transform.position;
            
            SceneView.duringSceneGui += OnSceneGUI;
        }

        public override VisualElement CreateInspectorGUI()
        {
            if (targets.Length == 1)
            {
                PropertyField sceneNotePropField = new PropertyField();
                sceneNotePropField.BindProperty(serializedObject.FindProperty(nameof(SceneNoteBehaviour.note)));
                return sceneNotePropField;
            }
            else
            {
                Object[] notes = new Object[Selection.gameObjects.Length];
                for (int index = 0; index < Selection.gameObjects.Length; index++)
                {
                    SceneNote sceneNote = Selection.gameObjects[index].GetComponent<SceneNoteBehaviour>().note;
                    notes[index] = sceneNote;
                }
                SerializedObject so = new(notes);
                
                SceneNoteEditor sceneNoteEditor = (SceneNoteEditor)CreateEditor(notes, typeof(SceneNoteEditor));
                
                VisualElement inspector = sceneNoteEditor.CreateInspectorGUI(true);
                inspector.Bind(so);
                return inspector;
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            UpdatePositionFromTransform();
        }

        private void UpdatePositionFromTransform()
        {
            // Update note's position data from the Transform's position
            if (_sceneNoteBehaviour.note != null
                && _transform.position != _lastPosition)
            {
                PositionChanged();
                _lastPosition = _transform.position;
                Repaint();
            }
        }

        private void PositionChanged()
        {
            SceneNote noteSO = (SceneNote)serializedObject.FindProperty(nameof(_sceneNoteBehaviour.note)).objectReferenceValue;
            SerializedObject noteSerializedObject = new SerializedObject(noteSO);
            noteSerializedObject.Update();

            SerializedProperty worldPosProp = noteSerializedObject.FindProperty(nameof(SceneNote.worldPosition));
            SerializedProperty localOffsetProp = noteSerializedObject.FindProperty(nameof(SceneNote.localOffset));

            Vector3 finalPosition = _transform.position;
            worldPosProp.vector3Value = finalPosition;
            if (noteSO.ConnectedTransform != null)
            {
                localOffsetProp.vector3Value = noteSO.ConnectedTransform.InverseTransformPoint(finalPosition);
                Vector3 scaler = noteSO.ConnectedTransform.localScale;
                if(SceneNoteEditor.ParentConstraint != null) SceneNoteEditor.ParentConstraint.SetTranslationOffset(0, Vector3.Scale(localOffsetProp.vector3Value, scaler));
            }

            noteSerializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private void OnDestroy()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }
    }
}