using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace SceneNotes
{
    public class NoteCategories : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField, HideInInspector, FormerlySerializedAs("_categories")] private List<string> _oldCategories;
        [SerializeField, HideInInspector] private bool _migrated;
        [SerializeField] private List<NoteCategory> _categories;

        public List<NoteCategory> Categories => _categories;

        [Serializable]
        public struct NoteCategory
        {
            public NoteCategory(string name, Texture2D icon)
            {
                this.name = name;
                this.icon = icon;
            }

            public string name;
            public Texture2D icon;
        }

        public void OnBeforeSerialize()
        {
            if (_migrated && _oldCategories is { Count: 0 }) _oldCategories = null;
        }

        public void OnAfterDeserialize()
        {
            if (!_migrated && _oldCategories != null && _oldCategories.Count > 0)
            {
                if (_categories == null) _categories = new List<NoteCategory>();
                
                _categories.Clear();
                foreach (string categoryName in _oldCategories)
                    _categories.Add(new NoteCategory(categoryName, null));

                _migrated = true;
                _oldCategories.Clear();
            }
        }

        private static NoteCategories _instance;
        public static NoteCategories Instance
        {
            get
            {
#if UNITY_EDITOR
                // TODO: Fix this for eventual runtime behaviour. For now we're always assuming it's found in the editor.
                if (_instance == null)
                {
                    string[] assetsFound = AssetDatabase.FindAssets($"t:{nameof(NoteCategories)}");
                    if (assetsFound.Length == 1)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(assetsFound[0]);
                        _instance = AssetDatabase.LoadAssetAtPath<NoteCategories>(path);
                    }
                }
#endif
                return _instance;
            }
        }

        public void OnEnable()
        {
            _instance = this;

#if UNITY_EDITOR
            // TODO: Make all these filenames more secure
            if (_categories == null)
            {
                _categories = new List<NoteCategory>
                {
                    new("Todo", GetIcon("Category_Bright1")),
                    new("Bugs", GetIcon("Category_Bright2"))
                };
            }
            else
            {
                // Force icon when it's null
                for (int i = 0; i < _categories.Count; i++)
                {
                    if (_categories[i].icon != null) continue;
                    
                    NoteCategory noteCategory = _categories[i];
                    Texture2D newIcon = GetIcon($"Category_Bright{i+1}");
                    if (newIcon == null) GetIcon("Category_Default");
                    noteCategory.icon = newIcon;
                    _categories[i] = noteCategory;
                }
            }
#endif
        }

#if UNITY_EDITOR
        private static Texture2D GetIcon(string iconName)
        {
            string iconPath = $"Packages/io.continis.scene-notes/UI/Images/NoteIcons_Category/{iconName}.png";
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            return icon;
        }
#endif

        public static int GetSafeId(int originalId)
        {
            if (Instance == null) return originalId;

            return originalId <= Instance.Categories.Count - 1 ? originalId : 0;
        }
    }
}