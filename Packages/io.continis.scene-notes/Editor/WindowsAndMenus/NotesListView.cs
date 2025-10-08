using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace SceneNotes.Editor
{
#if UNITY_2023_1_OR_NEWER
    [UxmlElement("NotesListView")]
    public partial class NotesListView : ListView
    {
#else
    public class NotesListView : ListView
    {
        public new class UxmlFactory : UxmlFactory<NotesListView> { }
#endif
        
        private readonly string _noteTemplatePath = Path.Combine(Constants.packageAssetsFolder,
            Constants.uiToolkitTemplatesFolder, "NotesBrowser", "NoteEntry.uxml");

        private VisualTreeAsset _noteEntryTemplate;

        public void Init(VisualElement container)
        {
            _noteEntryTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(_noteTemplatePath);

            virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
            showFoldoutHeader = false;
            showBoundCollectionSize = false;

            makeItem += MakeNote;
            bindItem += BindNote;
            unbindItem += UnbindItem;
            
            //container.Add(this);
        }

        private VisualElement MakeNote()
        {
            VisualElement entry = _noteEntryTemplate.Instantiate();
            Button b = new() {name = "Entry"};
            b.ClearClassList();
            b.Add(entry);
            
            return b;
        }
    
        private void BindNote(VisualElement noteEntry, int index)
        {
            SceneNote note = (SceneNote)itemsSource[index];
            if (note == null)
            {
                // Note might have just been deleted
                UnbindItem(noteEntry, index);
                return;
            }

            Button b = (Button)noteEntry;
            b.clicked += () => PingNote(index);

            SerializedObject serializedObject = new(note);
            noteEntry.Bind(serializedObject);
        
            noteEntry.Q<Label>("Number").text = index.ToString();

            // Disconnect author name if empty
            Label label = noteEntry.Q<Label>("Author");
            if (string.IsNullOrEmpty(note.author))
            {
                label.Unbind();
                label.style.display = DisplayStyle.None;
            }
            else label.style.display = DisplayStyle.Flex;

            Label dateLabel = noteEntry.Q<Label>("Date");
            dateLabel.text = Utilities.HumanReadableDate_Short(note.creationDate);
            dateLabel.tooltip = Utilities.HumanReadableDate(note.creationDate);

            VisualElement noteStateBar = noteEntry.Q<VisualElement>("State");
            SetClassFromState(note.state, noteStateBar);
            VisualElement tracker = new(){name = "StateTracker"};
            noteStateBar.Add(tracker);
            tracker.TrackPropertyValue(serializedObject.FindProperty(nameof(SceneNote.state)), prop =>
            {
                SetClassFromState((SceneNote.State)prop.enumValueIndex, noteStateBar);
            });

            VisualElement screenshot = noteEntry.Q<VisualElement>("Screenshot");
            if (note.screenshots != null && note.screenshots.Count > 0)
            {
                screenshot.style.backgroundImage = new StyleBackground(note.screenshots[0]);
                screenshot.style.display = DisplayStyle.Flex;
            }
            else
            {
                screenshot.style.display = DisplayStyle.None;
            }
        }

        private void PingNote(int index)
        {
            SceneNote note =  (SceneNote)itemsSource[index];
            EditorGUIUtility.PingObject(note);
        
            if(note.scene != null && SceneManager.GetSceneByName(note.scene.name).isLoaded)
                SceneView.lastActiveSceneView.Frame(new Bounds(note.worldPosition, Vector3.one * 5f), false);
        }

        private void SetClassFromState(SceneNote.State state, VisualElement noteStateBar)
        {
            noteStateBar.ClearClassList();
            string className = state switch
            {
                SceneNote.State.NotStarted => EditorGUIUtility.isProSkin ? "NotStarted_Dark" : "NotStarted_Light",
                SceneNote.State.Done => "Done",
                SceneNote.State.InProgress => "InProgress",
                _ => ""
            };
            noteStateBar.AddToClassList(className);
        }

        private void UnbindItem(VisualElement noteEntry, int index)
        {
            noteEntry.Unbind();
            noteEntry.Q<VisualElement>("State").ClearClassList();
            VisualElement stateTracker = noteEntry.Q<VisualElement>("StateTracker");
            stateTracker?.RemoveFromHierarchy();
            (noteEntry as Button)!.clickable = null;
        }
    }
}