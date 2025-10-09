using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Search;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace SceneNotes.Editor
{
    public class NotesBrowser : EditorWindow
    {
        
        [SerializeField] private VisualTreeAsset template = default;
    
        // UI elements
        private NotesListView _notesListView;
        private DropdownField _stateFilter;
        private DropdownField _categoryFilter;
    
        private List<SceneNote> _notes;
        private bool _isReady;
    
        // Search parameters and filters
        private string _searchTerm;
        private int _filterByState;
        private int _filterByCategory;
        private bool _filterByOpenScenes = true;
        
        private static NotesBrowser Wnd => Resources.FindObjectsOfTypeAll<NotesBrowser>()[0];
        public static bool IsOpen => Resources.FindObjectsOfTypeAll<NotesBrowser>().Length > 0;

        [MenuItem("Window/Scene Notes/Notes Browser")]
        [MenuItem(Constants.menuItemBaseName + "Notes Browser window", false, 1)]
        public static void OpenWindow()
        {
            NotesBrowser wnd = GetWindow<NotesBrowser>();
            wnd.titleContent = new GUIContent("Notes Browser");
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.Add(template.Instantiate());
            
            _notesListView = root.Q<NotesListView>();
            _notesListView.Init(root);

            HelpBox suggestionBox = root.Q<HelpBox>("SuggestionBox");
            _notesListView.hierarchy.Add(suggestionBox);

            ToolbarSearchField tsf = root.Q<ToolbarSearchField>();
            tsf.RegisterValueChangedCallback(OnSearch);

            _stateFilter = root.Q<DropdownField>("StateFilter");
            _stateFilter.choices = new List<string>(){"–", nameof(SceneNote.State.NotStarted), nameof(SceneNote.State.InProgress), nameof(SceneNote.State.Done)};
            _stateFilter.SetValueWithoutNotify("–");
            _stateFilter.RegisterValueChangedCallback(OnStateFilterChanged);

            if(NoteCategories.Instance == null) NotesDatabase.WarmUpCategories();
            _categoryFilter = root.Q<DropdownField>("CategoryFilter");
            SetupCategoriesDropdown();

            Toggle openScenesToggle = root.Q<Toggle>("OpenScenesFilter");
            openScenesToggle.RegisterValueChangedCallback(OnOpenScenesFilterChanged);

            _searchTerm = "";
            _filterByState = -1;
            _filterByCategory = -1;
            _isReady = true;
            
            FindNotes();
        }

        private void SetupCategoriesDropdown()
        {
            if (_categoryFilter == null) return; // Prevents errors if the window is open but in the background (i.e. no CreateGUI ever called)

            _categoryFilter.choices = NoteCategories.Instance.Categories.Select(cat => cat.name).ToList();
            _categoryFilter.choices.Insert(0, "–");
            _categoryFilter.SetValueWithoutNotify("–");
            _categoryFilter.RegisterValueChangedCallback(OnCategoryFilterChanged);
            _filterByCategory = -1;
        }

        private void OnCategoryFilterChanged(ChangeEvent<string> evt)
        {
            _filterByCategory = _categoryFilter.index - 1;
            FindNotes();
        }

        private void OnStateFilterChanged(ChangeEvent<string> evt)
        {
            _filterByState = _stateFilter.index - 1;
            FindNotes();
        }

        private void OnOpenScenesFilterChanged(ChangeEvent<bool> evt)
        {
            _filterByOpenScenes = evt.newValue;
            FindNotes();
        }

        private void FindNotes()
        {
            if (!_isReady) return;
            
            string searchQuery = String.IsNullOrEmpty(_searchTerm)
                ? "p: t:SceneNotes.SceneNote"
                : $"p: t:SceneNotes.SceneNote (name:\"{_searchTerm}\" or title:\"{_searchTerm}\" or author:\"{_searchTerm}\" or contents:\"{_searchTerm}\")";

            string stateFilterQuery = string.Empty;
            if (_filterByState != -1)
            {
                stateFilterQuery = $" and state=<$enum:{((SceneNote.State)_filterByState).ToString()},SceneNotes.SceneNote+State$>";
            }

            string catFilterQuery = string.Empty;
            if (_filterByCategory != -1)
            {
                catFilterQuery = $" and categoryId={_filterByCategory}";
            }

            string sceneFilterQuery = string.Empty;
            if (_filterByOpenScenes)
            {
                sceneFilterQuery = " and (";
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene openScene = SceneManager.GetSceneAt(i);
                    if (i != 0) sceneFilterQuery += " or ";
                    sceneFilterQuery += $"scene=\"{openScene.path}\"";
                }

                sceneFilterQuery += ")";
            }

            searchQuery = $"{searchQuery}{stateFilterQuery}{catFilterQuery}{sceneFilterQuery}";

            _notes = new List<SceneNote>();
        
            SearchService.Request(searchQuery, 
                (context, items) =>
                {
                    foreach (SearchItem searchItem in items)
                    {
                        SceneNote foundNote = searchItem.ToObject<SceneNote>();
                        _notes.Add(foundNote);
                    }

                    if(_notesListView != null) _notesListView.itemsSource = _notes;
                    context.Dispose();
                });
        }

        private void OnSearch(ChangeEvent<string> evt)
        {
            _searchTerm = evt.newValue;
            FindNotes();
        }
        
        private void OnDisable()
        {
            if(_notesListView != null) _notesListView.itemsSource = null;
            _notes = null;
        }
        
        // Static methods
        public static void RefreshNotes()
        {
            Wnd?.FindNotes();
        }
        
        public static void RefreshCategories()
        {
            Wnd?.SetupCategoriesDropdown();
            Wnd?.FindNotes();
        }
    }
}
