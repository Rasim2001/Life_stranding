using System.Collections.Generic;
using System.Linq;
using SceneNotes.Editor;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace SceneNotes.Toolbar
{
    [EditorToolbarElement(ID, typeof(SceneView))]
    public sealed class StateDropdown : DropdownToggleBase
    {
        public const string ID = "SceneNotes.StateDropdown";

        private Dictionary<string, string> _dictionaryToWatch;
        
        public StateDropdown()
        {
            name = ID;
            tooltip = "Filter notes by their State.";
            
            _iconName = "State";
            _isOnSessionKey = EditingSession.FilteringState;
            _arraySessionKey = EditingSession.StateArray;
            
            PrepareItemsDictionary();
            Initialise();
        }

        protected override void PrepareItemsDictionary()
        {
            _dictionaryToWatch = new Dictionary<string, string>();
            _dictionaryToWatch.Add("NotStarted", "Not Started");
            _dictionaryToWatch.Add("InProgress", "In Progress");
            _dictionaryToWatch.Add("Done", "Done");
            
            _dropdownValues = new Dictionary<string, bool>();
            foreach (KeyValuePair<string,string> valuePair in _dictionaryToWatch)
            {
                _dropdownValues.Add(valuePair.Key, true);
            }
        }

        protected override void ApplyAllItems(bool overrideValue = false, bool valueToUse = false)
        {
            foreach (string key in _dropdownValues.Keys.ToList())
            {
                NotesDatabase.instance.ShowHideNotesByState(key, overrideValue ? valueToUse : _dropdownValues[key]);
            }
        }

        protected override void AddDropdownItems()
        {
            _dropdown.AddDisabledItem(new GUIContent("State"));
            
            // Add real owners
            foreach (KeyValuePair<string, string> entry in _dictionaryToWatch)
            {
                _dropdown.AddItem(new GUIContent(entry.Value), _dropdownValues[entry.Key], OnElementClicked, entry.Key);
            }
            
            // Add special items
            _dropdown.AddSeparator("");
            _dropdown.AddItem(new GUIContent("Any state"), AreAllItemsOn(), OnElementClicked, "-1");
            _dropdown.AddItem(new GUIContent("None"), AreAllItemsOff(), OnElementClicked, "0");
        }

        protected override void ChangeFilters(string clickedItemId)
        {
            if (clickedItemId is "0" or "-1")
            {
                // Change all owners at once
                foreach (string key in _dropdownValues.Keys.ToList())
                {
                    _dropdownValues[key] = clickedItemId != "0";
                    NotesDatabase.instance.ShowHideAllNoteGOs(_dropdownValues[key]);
                }
            }
            else
            {
                // Change one state only
                _dropdownValues[clickedItemId] = !_dropdownValues[clickedItemId];
                NotesDatabase.instance.ShowHideNotesByState(clickedItemId, _dropdownValues[clickedItemId]);
            }
        }
    }
}