using System.Collections;
using System.Collections.Generic;
using System.IO;
using SceneNotes.Editor;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace SceneNotes.Toolbar
{
    public abstract class DropdownToggleBase : EditorToolbarDropdownToggle
    {
        protected GenericMenu _dropdown;
        protected Dictionary<string, bool> _dropdownValues;

        protected string _iconName;
        protected string _isOnSessionKey;
        protected string _arraySessionKey;

        protected void Initialise()
        { 
            RetrieveSessionData();
            
            EditorApplication.delayCall += () =>
            {
                if(!AreAllItemsOn())
                {
                    SetValueWithoutNotify(value);
                    LoadIcon(value);
                }
                else LoadIcon(false);
            };
            
            dropdownClicked += OnDropdownClicked;
            this.RegisterValueChangedCallback(OnValueChanged);
        }
        
        protected bool AreAllItemsOn() => !_dropdownValues.ContainsValue(false);
        protected bool AreAllItemsOff() => !_dropdownValues.ContainsValue(true);

        protected abstract void ApplyAllItems(bool overrideValue = false, bool valueToUse = false);
        protected abstract void AddDropdownItems();
        protected abstract void ChangeFilters(string clickedItemId);
        protected abstract void PrepareItemsDictionary();

        private void OnValueChanged(ChangeEvent<bool> evt)
        {
            bool valueToSet = evt.newValue;
            if(valueToSet)
            {
                if (AreAllItemsOn())
                {
                    // Allow the user to turn off some
                    OnDropdownClicked();

                    valueToSet = !AreAllItemsOn();
                }
            }
            
            LoadIcon(valueToSet);
            SetValueWithoutNotify(valueToSet);
            
            // If button is off, force all GOs to visible
            if (valueToSet == false) ApplyAllItems(true, true);
            else ApplyAllItems();
            
            EditingSession.SetBool(_isOnSessionKey, value);
            ClearSelection();
        }

        private void LoadIcon(bool on)
        {
            string iconEnding = on ? "On" :"Off";
            string iconPath = Path.Combine(Constants.packageAssetsFolder, Constants.uiImagesFolder, Constants.toolbarIconsFolder,
                                            $"{Constants.imagePrefix}{_iconName}{iconEnding}.png");
            icon = (Texture2D)AssetDatabase.LoadAssetAtPath(iconPath, typeof(Texture2D));
        }

        public void Reset()
        {
            if(_dropdownValues != null) OnElementClicked("-1");
        }

        private void OnDropdownClicked()
        {
            _dropdown = new GenericMenu();
            AddDropdownItems();
            _dropdown.DropDown(worldBound);
        }

        protected void OnElementClicked(object id)
        {
            string clickedItemId = id.ToString();
            ChangeFilters(clickedItemId);
            bool usingFilters = clickedItemId == "0" || !AreAllItemsOn();
            
            LoadIcon(usingFilters);
            SetValueWithoutNotify(usingFilters);
            
            SaveSession();
            ClearSelection();
        }

        private void ClearSelection()
        {
            // Clear selection if object disappeared
            if (Selection.activeGameObject != null && !Selection.activeGameObject.activeInHierarchy)
                Selection.activeGameObject = null;
        }

        private void SaveSession()
        {
            EditingSession.SetBool(_isOnSessionKey, value);
            
            int[] arrayToStore = new int[_dropdownValues.Count];
            List<string> keys = new(_dropdownValues.Keys);
            int i = 0;
            foreach (string key in keys)
            {
                arrayToStore[i] = _dropdownValues[key] ? 1 : 0;
                i++;
            }

            EditingSession.SetArray(_arraySessionKey, arrayToStore);
        }

        private void RetrieveSessionData()
        {
            SetValueWithoutNotify(EditingSession.GetBool(_isOnSessionKey));
            
            int[] storedArray = EditingSession.GetArray(_arraySessionKey);
            if (storedArray == null || storedArray.Length != _dropdownValues.Count) return; // No valid data
            
            List<string> itemKeys = new(_dropdownValues.Keys);
            int i = 0;
            foreach (string key in itemKeys)
            {
                _dropdownValues[key] = storedArray[i] == 1;
                i++;
            }
        }
    }
}