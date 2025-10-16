using System.IO;
using SceneNotes.Editor;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneNotes.Toolbar
{
    [EditorToolbarElement(ID, typeof(SceneView))]
    public class AddNoteButton : EditorToolbarButton
    {
        public const string ID = "SceneNotes.AddNoteButton";

        public AddNoteButton()
        {
            name = ID;
            string imagesPath = Path.Combine(Constants.packageAssetsFolder, Constants.uiImagesFolder, Constants.toolbarIconsFolder);
            EditorApplication.delayCall += () => icon = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(imagesPath, $"{Constants.imagePrefix}Plus.png"));
            tooltip = "Adds a new scene note.";
            clicked += Clicked;
        }

        private void Clicked()
        {
            Scene s = SceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(s.path))
            {
                GUID guid = AssetDatabase.GUIDFromAssetPath(s.path);
                NotesDatabase.instance.CreateNewNote(guid.ToString());
            }
            else
            {
                Debug.LogWarning($"{Constants.packagePrefix} {Constants.creatingNoteInUnsavedSceneError}");
            }
        }
    }
}