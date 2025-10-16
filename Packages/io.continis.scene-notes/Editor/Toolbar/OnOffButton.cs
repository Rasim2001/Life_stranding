using System.IO;
using SceneNotes.Editor;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace SceneNotes.Toolbar
{
    [EditorToolbarElement(ID, typeof(SceneView))]
    class OnOffButton : EditorToolbarToggle
    {
        public const string ID = "SceneNotes.OnOffButton";

        private ReloadButton _reloadButton;
        private FocusButton _focusButton;
        private AddNoteButton _addNoteButton;
        private CategoryDropdown _categoryDropdown;
        private StateDropdown _stateDropdown;

        public OnOffButton()
        {
            name = ID;
            tooltip = "Display/hide scene notes.";
            string imagesPath = Path.Combine(Constants.packageAssetsFolder, Constants.uiImagesFolder, Constants.toolbarIconsFolder);
            EditorApplication.delayCall += () =>
            {
                onIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(imagesPath, $"{Constants.imagePrefix}ToolbarIconOn.png"));
                offIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(imagesPath, $"{Constants.imagePrefix}ToolbarIconOff.png"));
            };

            // Needed to turn the button blue if the toolbar was open
            if(EditingSession.IsShowingNotes()) SetValueWithoutNotify(true);
            
            this.RegisterValueChangedCallback(OnButtonClicked);
            RegisterCallback<AttachToPanelEvent>(OnAttach);
        }

        public sealed override void SetValueWithoutNotify(bool newValue) => base.SetValueWithoutNotify(newValue);

        /// <summary>
        /// Happens when the Overlay is visualised by the user
        /// </summary>
        private void OnAttach(AttachToPanelEvent evt)
        {
            _reloadButton = parent.Q<ReloadButton>();
            _focusButton = parent.Q<FocusButton>();
            _addNoteButton = parent.Q<AddNoteButton>();
            _categoryDropdown = parent.Q<CategoryDropdown>();
            _stateDropdown = parent.Q<StateDropdown>();
            
            // Hide buttons immediately if there was no session already started
            if (!EditingSession.IsShowingNotes()) ChangeButtonsVisibility(false);
        }
        
        /// <summary>
        /// Invoked when the button is pressed, the toolbar is turned on/off 
        /// </summary>
        private void OnButtonClicked(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
            {
                Scene s = SceneManager.GetActiveScene();
                if (string.IsNullOrEmpty(s.path))
                {
                    Debug.LogWarning($"{Constants.packagePrefix} {Constants.sceneNotSavedError}");
                    SetValueWithoutNotify(false);
                    return;
                }

                NotesDatabase.instance.DisplayNotes();
                ChangeButtonsVisibility(true);
                _categoryDropdown.Reset();
                _stateDropdown.Reset();
                EditingSession.SetShowingNotesState(true);
            }
            else
            {
                // Close the toolbar
                NotesDatabase.instance.HideNotes();
                ChangeButtonsVisibility(false);
                EditingSession.SetShowingNotesState(false);
            }
        }

        private void ChangeButtonsVisibility(bool show)
        {
            _reloadButton.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            _focusButton.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            _addNoteButton.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            _stateDropdown.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            _categoryDropdown.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}