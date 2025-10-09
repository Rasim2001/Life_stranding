using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace SceneNotes.Editor.CustomEditors
{
    [CustomPropertyDrawer(typeof(SceneNote))]
    public class SceneNotePropertyDrawer : PropertyDrawer
    {
        private SceneNoteEditor _sceneNoteEditor;
        private SerializedObject _serializedObject;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property.objectReferenceValue != null)
            {
                if (_serializedObject == null) _serializedObject = new SerializedObject(property.objectReferenceValue);
                
                if (_sceneNoteEditor == null)
                    _sceneNoteEditor = (SceneNoteEditor)UnityEditor.Editor.CreateEditor(property.objectReferenceValue, typeof(SceneNoteEditor));
                
                VisualElement inspector = _sceneNoteEditor.CreateInspectorGUI(true);
                
                inspector.Bind(_serializedObject);
                
                return inspector;
            }
            else
            {
                return base.CreatePropertyGUI(property);
            }
        }
    }
}