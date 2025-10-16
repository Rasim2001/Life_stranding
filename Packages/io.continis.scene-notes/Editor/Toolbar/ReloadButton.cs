using System.IO;
using SceneNotes.Editor;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace SceneNotes.Toolbar
{
    [EditorToolbarElement(ID, typeof(SceneView))]
    public class ReloadButton : EditorToolbarButton
    {
        public const string ID = "SceneNotes.ReloadButton";

        private CategoryDropdown _categoryDropdown;
        private StateDropdown _stateDropdown;
        
        public ReloadButton()
        {
            name = ID;
            string imagesPath = Path.Combine(Constants.packageAssetsFolder, Constants.uiImagesFolder, Constants.toolbarIconsFolder);
            EditorApplication.delayCall += () => icon = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(imagesPath, $"{Constants.imagePrefix}Reload.png"));
            tooltip = "Reload notes. This could be useful in case new notes have been created in the Project view by duplicating note ScriptableObjects.";
            clicked += Clicked;
            RegisterCallback<AttachToPanelEvent>(OnAttach);
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            _categoryDropdown = parent.Q<CategoryDropdown>();
            _stateDropdown = parent.Q<StateDropdown>();
        }

        private void Clicked()
        {
            _categoryDropdown.Reset();
            _stateDropdown.Reset();
            NotesDatabase.instance.LoadAllNotes();
        }
    }
}