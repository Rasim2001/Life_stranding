using System.Collections.Generic;
using System.IO;
using System.Linq;
using SceneNotes.Editor;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace SceneNotes.Toolbar
{
    [EditorToolbarElement(ID, typeof(SceneView))]
    public sealed class CategoryDropdown : DropdownToggleBase
    {
        public const string ID = "SceneNotes.CategoryDropdown";
        public static Dictionary<string, bool> VisibleCategories;
        
        private List<string> _listToWatch;
        
        public CategoryDropdown()
        {
            name = ID;
            tooltip = "Filter notes by category.";
            
            _iconName = "Category";
            _isOnSessionKey = EditingSession.FilteringDecks;
            _arraySessionKey = EditingSession.DecksArray;
            
            PrepareItemsDictionary();
            Initialise();

            VisibleCategories = _dropdownValues;
            
            NotesDatabase.instance.CategoriesUpdated += PrepareItemsDictionary;
            RegisterCallback<DetachFromPanelEvent>(evt => NotesDatabase.instance.CategoriesUpdated -= PrepareItemsDictionary);
        }

        protected override void PrepareItemsDictionary()
        {
            _listToWatch = NotesDatabase.instance.GetOrWarmupCategories();
            _dropdownValues = new Dictionary<string, bool>();
            if (_listToWatch == null) return; // Should only happen on first editor launch
            foreach (string categoryName in _listToWatch) 
            {
                _dropdownValues.TryAdd(categoryName, true);
            }
        }
        
        protected override void ApplyAllItems(bool overrideValue = false, bool valueToUse = false)
        {
            int i = 0;
            foreach (string key in _dropdownValues.Keys.ToList())
            {
                NotesDatabase.instance.ShowHideNotesByCategory(i, overrideValue ? valueToUse : _dropdownValues[key]);
                i++;
            }
        }
        
        protected override void AddDropdownItems()
        {
            _dropdown.AddDisabledItem(new GUIContent("Categories"));

            // Add decks
            foreach (string cat in _listToWatch) 
            {
                _dropdown.AddItem(new GUIContent(cat), _dropdownValues[cat], OnElementClicked, cat);
            }
            
            // Add special items
            _dropdown.AddSeparator("");
            _dropdown.AddItem(new GUIContent("Select all"), AreAllItemsOn(), OnElementClicked, "-1");
            _dropdown.AddItem(new GUIContent("Select none"), AreAllItemsOff(), OnElementClicked, "0");
        }
        
        protected override void ChangeFilters(string clickedItemId)
        {
            List<string> categoryNames = _dropdownValues.Keys.ToList();
            if (clickedItemId is "0" or "-1")
            {
                // Change all decks at once
                for (int i = 0; i < categoryNames.Count; i++)
                {
                    string categoryName = categoryNames[i];
                    _dropdownValues[categoryName] = clickedItemId != "0";
                    NotesDatabase.instance.ShowHideNotesByCategory(i, _dropdownValues[categoryName]);
                }
            }
            else
            {
                // Change one deck only
                _dropdownValues[clickedItemId] = !_dropdownValues[clickedItemId];
                int categoryIndex = categoryNames.FindIndex(s => s == clickedItemId);
                NotesDatabase.instance.ShowHideNotesByCategory(categoryIndex, _dropdownValues[clickedItemId]);
            }
        }
    }
}