using System;
using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEngine;

namespace SceneNotes.Editor
{
    public class SceneNotesSettings
    {
        internal static Action<NoteIconStyle> NoteIconStyleChanged;
        
        private const string CatVisualisation = "Visualisation Preferences";
        private const string CatPersonal = "Personal";
        private const string CatProject = "Project";

        public enum NoteIconStyle
        {
            [Tooltip("The note's icon display its state: Not Started, In Progress, Done.")] State,
            [Tooltip("The note's icon displays its category. Assign a category icon in the NoteCategories ScriptableObject.")] Category,
        }
        
        public static PackageSetting<bool> WelcomeWindowSeen = new("general.welcomeWindowSeen", false);

        [UserSetting(CatPersonal, "Author Name", "This is the name used to sign notes and comments.")]
        public static UserPref<string> authorName = new("preferences.authorName", CloudProjectSettings.userName);
        
        [UserSetting(CatVisualisation, "Display Title In Scene", "Shows the title of each note in Scene View, under the note's icon.")]
        public static UserPref<bool> displayTitleInScene = new("preferences.displayTitleInScene", true);
        
        [UserSetting(CatVisualisation, "Maximum Title Length", "If titles are displayed in the Scene View, only displays up to this length.")]
        public static UserPref<int> maxTitleLength = new("preferences.maxTitleLength", 30);
        
        [UserSetting(CatVisualisation, "Title Colour", "The colour to use for the note's title. Only used if the title is displayed.")]
        public static UserPref<Color> titleColour = new("preferences.titleColour", Color.white);
        
        [UserSetting(CatVisualisation, "Title Background Colour", "The colour to use for the note's title background. Only used if the title displayed.")]
        public static UserPref<Color> titleBackgroundColour = new("preferences.titleBackgroundColour", new Color(0f,0f,0f, .1f));
        
        [UserSetting(CatVisualisation, "Icon Displays", "Whether note icons in the Scene View display the note's state, or its category.")]
        public static UserPref<NoteIconStyle> noteIconStyle = new("preferences.noteIconStyle", NoteIconStyle.State);
        
        [UserSetting(CatProject, "Notes Folder", "This is the folder from which Scene Notes loads data, and in which it saves new notes.")]
        public static PackageSetting<string> unityFolder = new("general.unityFolder", "SceneNotes");
    }
}