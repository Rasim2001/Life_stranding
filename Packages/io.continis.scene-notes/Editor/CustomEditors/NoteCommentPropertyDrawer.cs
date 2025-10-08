using System.IO;
using UnityEditor;
using UnityEngine.UIElements;

namespace SceneNotes.Editor.CustomEditors
{
    [CustomPropertyDrawer(typeof(NoteComment))]
    public class NoteCommentPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement customInspector = new();
            
            string templatePath = Path.Combine(Constants.packageAssetsFolder, Constants.uiToolkitTemplatesFolder, "CommentTemplate.uxml");
            VisualTreeAsset template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(templatePath);
            template.CloneTree(customInspector);
            
            return customInspector;
        }
    }
}