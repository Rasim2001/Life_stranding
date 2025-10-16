using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SceneNotes.Editor.CustomEditors;
using SceneNotes.Toolbar;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace SceneNotes.Editor
{
    public class NotesDatabase : ScriptableSingleton<NotesDatabase>
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private SerializableGODictionary _topLevelNoteGOsByScene;
        
        private readonly string _defaultTitle = "Todo";
        private readonly string _defaultContent = "";
        private readonly HideFlags _goHideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
        
        private Texture2D _noteIconNotStarted;
        private Texture2D _noteIconInProgress;
        private Texture2D _noteIconDone;

        public event UnityAction CategoriesUpdated;

        public void DisplayNotes()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneClosed += OnSceneClosed;
            
            LoadAllNotes();
            
            _root.SetActive(true);
        }

        public void LoadAllNotes()
        {
            RemovePreviousNotesGO();
            // Create root GameObject
            _root = EditorUtility.CreateGameObjectWithHideFlags("SceneNotes", _goHideFlags);
            _topLevelNoteGOsByScene = new SerializableGODictionary();

            // Load note icons
            string imagesPath = Path.Combine(Constants.packageAssetsFolder, Constants.uiImagesFolder, Constants.iconsByStateFolder);
            _noteIconNotStarted = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(imagesPath, "State_NotStarted.png"));
            _noteIconInProgress = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(imagesPath, "State_InProgress.png"));
            _noteIconDone = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(imagesPath, "State_Done.png"));

            // Prepare main SO folder
            string folderPath = Utilities.GetAbsNotesFolderPath();
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
            
            // Create default Categories SO if it doesn't exist
            WarmUpCategories();

            // Take categories from SO
            NotifyCategoriesUpdate();

            // Finally load the notes from disk
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene openScene = SceneManager.GetSceneAt(i);
                if(openScene.isLoaded) LoadNotesFromScene(Utilities.SceneToGuid(openScene));
            }
        }

        public void NotifyCategoriesUpdate() => CategoriesUpdated?.Invoke();

        private void OnSceneOpened(Scene sceneLoaded, OpenSceneMode mode) => LoadNotesFromScene(Utilities.SceneToGuid(sceneLoaded));
        private void OnSceneClosed(Scene sceneGone) => RemoveNotesFromScene(Utilities.SceneToGuid(sceneGone));

        private void LoadNotesFromScene(string sceneGuid)
        {
            // Find notes, but return if there are none
            string notesFolderPath = Utilities.GetAbsNotesFolderPath();
            string subFolderPath = Path.Combine(notesFolderPath, sceneGuid);
            if (!Directory.Exists(subFolderPath)) return;
            
            // Create scene GO
            GetOrCreateSceneGO(sceneGuid);
            
            // Loop through SOs, and create a GO for each
            string[] paths = Utilities.GetAssetPathsInFolder(subFolderPath, "*.asset");
            Utilities.MakePathsProjectRelative(paths);
            foreach (string soPath in paths)
            {
                SceneNote note = (SceneNote)AssetDatabase.LoadAssetAtPath(soPath, typeof(SceneNote));
                GameObject noteGO = CreateNoteGameObject(note);
                ParentNoteGOAccordingly(noteGO, note, sceneGuid);
            }
        }

        private void RemoveNotesFromScene(string sceneGuid)
        {
            if (_topLevelNoteGOsByScene.ContainsKey(sceneGuid))
            {
                DestroyImmediate(_topLevelNoteGOsByScene[sceneGuid]);
                _topLevelNoteGOsByScene.Remove(sceneGuid);
            }
        }
        
        private GameObject CreateNoteGameObject(SceneNote note)
        {
            GameObject noteGO = EditorUtility.CreateGameObjectWithHideFlags(
                "SceneNote", HideFlags.DontSave, typeof(SceneNoteBehaviour));
            noteGO.transform.hideFlags = HideFlags.HideInInspector | HideFlags.DontSave;
            
            SceneNoteBehaviour snb = noteGO.GetComponent<SceneNoteBehaviour>();
            snb.note = note;
            snb.note.referencingNoteBehaviour = snb;

            // Unused for now: Second GO to enable a second icon
            // GameObject child = EditorUtility.CreateGameObjectWithHideFlags("Child", HideFlags.DontSave);
            // SceneVisibilityManager.instance.DisablePicking(child, false);
            // child.transform.SetParent(noteGO.transform);

            SerializedObject serObj = new(note);
            SceneNoteEditor.ResolveReferencedObject(serObj, out GameObject retrievedGO);
            if(retrievedGO != null) SceneNoteEditor.SetupParentConstraint(note);

            SetNoteIcon(note);
            
            return noteGO;
        }

        public void CreateNewNote(string sceneGuid)
        {
            Vector3 notePosition = SceneView.lastActiveSceneView.pivot;

            string newCardId = $"SceneNote-{GUID.Generate()}";
                
            SceneNote newNote = CreateInstance<SceneNote>();
            newNote.author = SceneNotesSettings.authorName;
            newNote.title = _defaultTitle;
            newNote.contents = _defaultContent;
            newNote.worldPosition = notePosition;
            newNote.scene = Utilities.GuidToAsset<SceneAsset>(sceneGuid);
            newNote.state = SceneNote.State.NotStarted;
            newNote.creationDate = Utilities.DateToString(DateTime.Now);
            newNote.modifiedDate = Utilities.DateToString(DateTime.Now);
            newNote.suffix = Utilities.FourNumbers();
            string folderPath = Path.Combine(Utilities.GetRelNotesFolderPath(), sceneGuid);
            string path = Path.Combine(folderPath, $"{newCardId}.asset");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
            AssetDatabase.CreateAsset(newNote, path);
            
            GameObject newNoteGO = CreateNoteGameObject(newNote);
            ParentNoteGOAccordingly(newNoteGO, newNote, sceneGuid);
            Selection.activeGameObject = newNoteGO;
        }

        public void ParentNoteGOAccordingly(GameObject noteGO, SceneNote newNote, string sceneGuid)
        {
            Transform sceneGOTransform = GetOrCreateSceneGO(sceneGuid);
            Transform categoryGOTransform = GetOrCreateCategoryGO(newNote, sceneGOTransform);
            noteGO.transform.SetParent(categoryGOTransform);
        }

        private Transform GetOrCreateSceneGO(string sceneGuid)
        {
            Transform sceneGOTransform = _root.transform.Find(sceneGuid);
            if (sceneGOTransform == null)
            {
                sceneGOTransform = EditorUtility.CreateGameObjectWithHideFlags(sceneGuid, _goHideFlags).transform;
                sceneGOTransform.SetParent(_root.transform);
                _topLevelNoteGOsByScene.TryAdd(sceneGuid, sceneGOTransform.gameObject);
            }

            return sceneGOTransform;
        }

        private Transform GetOrCreateCategoryGO(SceneNote newNote, Transform sceneGOTransform)
        {
            int safeCategoryId = NoteCategories.GetSafeId(newNote.categoryId);
            string categoryName = NoteCategories.Instance.Categories[safeCategoryId].name;
            Transform categoryGOTransform = sceneGOTransform.Find(categoryName);
            if (categoryGOTransform == null)
            {
                categoryGOTransform = EditorUtility.CreateGameObjectWithHideFlags(categoryName, _goHideFlags)
                    .transform;
                categoryGOTransform.SetParent(sceneGOTransform);
                
                // Set category to invisible if it's present and disabled in the dropdown
                if(CategoryDropdown.VisibleCategories.TryGetValue(categoryName, out bool isVisible))
                    if(!isVisible) categoryGOTransform.gameObject.SetActive(false);
            }

            return categoryGOTransform;
        }

        public bool SceneIsCurrentlyLoaded(string sceneGuid) => _topLevelNoteGOsByScene.ContainsKey(sceneGuid);

        public void ShowHideNotesByCategory(int catId, bool show)
        {
            foreach (KeyValuePair<string,GameObject> keyValuePair in _topLevelNoteGOsByScene)
            {
                Transform t = keyValuePair.Value.gameObject.transform.Find(NoteCategories.Instance.Categories[catId].name);
                if(t != null) t.gameObject.SetActive(show);
            }
        }
        
        public void ShowHideNotesByState(string stringState, bool show)
        {
            foreach ((string sceneGuid, GameObject sceneGO) in _topLevelNoteGOsByScene)
            {
                SceneNote.State state = Enum.Parse<SceneNote.State>(stringState);
                
                if (sceneGO.activeSelf)
                {
                    foreach (SceneNoteBehaviour sceneNote
                             in sceneGO.transform.GetComponentsInChildren<SceneNoteBehaviour>(true))
                    {
                        if(sceneNote.note.state == state) sceneNote.gameObject.SetActive(show);
                    }
                }
            }
        }
        
        /// <summary>
        /// Hides or shows the individual SceneNoteBehaviour GOs, in all active Category GOs.
        /// Used to reset all notes to visible/invisible after owner filtering from the toolbar.
        /// </summary>
        public void ShowHideAllNoteGOs(bool show)
        {
            foreach ((string sceneGuid, GameObject go) in _topLevelNoteGOsByScene)
            {
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    Transform categoryTransform = go.transform.GetChild(i);
                    if (!categoryTransform.gameObject.activeSelf) continue;
                    
                    for (int j = 0; j < categoryTransform.childCount; j++)
                    {
                        categoryTransform.GetChild(j).gameObject.SetActive(show);                        
                    }
                }
            }
        }
        
        /// <summary>
        /// Invoked by the toolbar UI button, when deactivating notes.
        /// </summary>
        public void HideNotes()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneClosed -= OnSceneClosed;
            
            if (_root != null) _root.SetActive(false);
        }

        private void RemovePreviousNotesGO()
        {
            if(_root == null) _root = GameObject.Find("SceneNotes");
            if (_root != null) DestroyImmediate(_root);
        }
        
        public void SetNoteIcon(SceneNote note)
        {
            switch (SceneNotesSettings.noteIconStyle.value)
            {
                case SceneNotesSettings.NoteIconStyle.State:
                    EditorGUIUtility.SetIconForObject(note.referencingNoteBehaviour.gameObject,
                        note.state switch
                        {
                            SceneNote.State.Done => _noteIconDone,
                            SceneNote.State.InProgress => _noteIconInProgress,
                            SceneNote.State.NotStarted => _noteIconNotStarted,
                            _ => throw new Exception("State is invalid")
                        });
                    break;
                
                case SceneNotesSettings.NoteIconStyle.Category:
                    int safeId = NoteCategories.GetSafeId(note.categoryId);
                    EditorGUIUtility.SetIconForObject(note.referencingNoteBehaviour.gameObject, NoteCategories.Instance.Categories[safeId].icon);
                    break;
            }
        }

        public void RefreshAllNotesIcons()
        {
            if (!EditingSession.IsShowingNotes()) return;
            
            foreach (GameObject sceneGO in _topLevelNoteGOsByScene.Values)
            {
                foreach (SceneNoteBehaviour sceneNoteBehaviour in sceneGO.transform.GetComponentsInChildren<SceneNoteBehaviour>())
                {
                    SetNoteIcon(sceneNoteBehaviour.note);                    
                }
            }
        }

        public List<string> GetOrWarmupCategories()
        {
            if(NoteCategories.Instance == null) WarmUpCategories();
            return NoteCategories.Instance.Categories.Select(cat => cat.name).ToList();
        }
        
        public static void WarmUpCategories()
        {
            string[] categorySO = AssetDatabase.FindAssets($"t:{typeof(NoteCategories)}");
            if (categorySO.Length < 1)
            {
                NoteCategories categories = CreateInstance<NoteCategories>();
                string notesFolderPath = Utilities.GetRelNotesFolderPath();
                if (!AssetDatabase.IsValidFolder(notesFolderPath))
                {
                    AssetDatabase.CreateFolder("Assets", Utilities.GetSafeUnityFolder());
                }
                AssetDatabase.CreateAsset(categories, Path.Combine(notesFolderPath, "NoteCategories.asset"));
                AssetDatabase.Refresh();
                categorySO = AssetDatabase.FindAssets($"t:{typeof(NoteCategories)}");
            }

            // TODO: Make this more stable in all use-cases (currently it fails when installing the package for the first time)
            if (categorySO.Length > 0)
            {
                NoteCategories noteCategories = AssetDatabase.LoadAssetAtPath<NoteCategories>(AssetDatabase.GUIDToAssetPath(categorySO[0]));
                noteCategories.OnEnable();
            }
        }
    }
}