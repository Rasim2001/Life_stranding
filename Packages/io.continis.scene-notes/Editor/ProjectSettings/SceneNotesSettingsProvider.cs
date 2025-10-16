using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEngine;

namespace SceneNotes.Editor
{
    static class SceneNotesSettingsProvider
    {
        private static SceneNotesSettings.NoteIconStyle _lastIconStyle;
        private const string SettingsPath = "Project/Scene Notes";

        [SettingsProvider]
        static SettingsProvider CreateSettingsProvider()
        {
            var provider = new UserSettingsProvider(SettingsPath,
                SceneNotesSettingsManager.settings,
                new [] { typeof(SceneNotesSettingsProvider).Assembly }, SettingsScope.Project);

            _lastIconStyle = SceneNotesSettings.noteIconStyle;
            
            SceneNotesSettingsManager.settings.afterSettingsSaved += OnSettingsSaved;
            
            return provider;
        }

        [InitializeOnLoadMethod]
        private static void SaveSettingsRuntimeSide()
        {
            SceneNoteBehaviour.DisplayTitleInScene = SceneNotesSettings.displayTitleInScene;
            SceneNoteBehaviour.TitleColour = SceneNotesSettings.titleColour;
            SceneNoteBehaviour.TitleBackgroundColour = SceneNotesSettings.titleBackgroundColour;
            SceneNoteBehaviour.MaxTitleLength = SceneNotesSettings.maxTitleLength;
        }

        private static void OnSettingsSaved()
        {
            SceneNotesSettings.maxTitleLength.value = Mathf.Clamp(SceneNotesSettings.maxTitleLength, 5, 200);
            
            SaveSettingsRuntimeSide();
            
            // Refresh all icons if icon style changed
            SceneNotesSettings.NoteIconStyle iconStyle = SceneNotesSettings.noteIconStyle;
            
            if(iconStyle != _lastIconStyle)
            { 
                NotesDatabase.instance.RefreshAllNotesIcons();
                SceneNotesSettings.NoteIconStyleChanged?.Invoke(iconStyle);
                _lastIconStyle = iconStyle;
            }
        }
    }
}