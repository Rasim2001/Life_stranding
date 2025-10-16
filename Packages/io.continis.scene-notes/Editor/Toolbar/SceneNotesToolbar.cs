using System.IO;
using SceneNotes.Editor;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;

namespace SceneNotes.Toolbar
{
    /// <summary>
    /// Utility class to store a global state representing the current state of the toolbar.
    /// </summary>
    public static class EditingSession
    {
        private const string SessionStateKey = "showingNotes";
        private const string FocusKey = "focusOnCards";
        public const string DecksArray = "decksArray";
        public const string FilteringDecks = "filteringDecks";
        public const string FilteringState = "filteringState";
        public const string StateArray = "stateArray";

        public static void SetShowingNotesState(bool newState) => SessionState.SetBool(SessionStateKey, newState);
        public static bool IsShowingNotes() => SessionState.GetBool(SessionStateKey, false);
        
        public static void SetIsFocusing(bool newState) => SessionState.SetBool(FocusKey, newState);
        public static bool IsFocusOn() => SessionState.GetBool(FocusKey, false);

        // Generic functions (need a valid key to be passed)
        public static void SetBool(string boolKey, bool newValue) => SessionState.SetBool(boolKey, newValue);
        public static bool GetBool(string boolKey) => SessionState.GetBool(boolKey, false);
        public static void SetArray(string boolKey, int[] newArray) => SessionState.SetIntArray(boolKey, newArray);
        public static int[] GetArray(string boolKey) => SessionState.GetIntArray(boolKey, default);
    }

    [Overlay(typeof(SceneView), "Scene Notes",
        defaultDockPosition = DockPosition.Top, defaultDockZone = DockZone.LeftColumn, defaultLayout = Layout.VerticalToolbar)]
    [Icon(Constants.packageAssetsFolder + "/" + Constants.uiImagesFolder + "/" + Constants.toolbarIconsFolder + "/ToolbarIconOff.png")]
    public class SceneNotesToolbar : ToolbarOverlay
    {
        SceneNotesToolbar() : base(OnOffButton.ID, ReloadButton.ID, FocusButton.ID,
                                        StateDropdown.ID, CategoryDropdown.ID, AddNoteButton.ID) { }
        
        public override void OnCreated()
        {
            Constants.imagePrefix = EditorGUIUtility.isProSkin ? "d_" : "";
            
            base.OnCreated();
        }
    }
}