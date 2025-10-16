namespace SceneNotes.Editor
{
    public static class Constants
    {
        public static string imagePrefix = "";
        
        public const string menuItemBaseName = "Tools/Scene Notes/";
        
        // URLs
        public const string reviewUrl = "https://assetstore.unity.com/packages/tools/level-design/scene-notes-tasks-issues-annotations-275627#reviews";
        public const string discordUrl = "https://discord.com/invite/rCRug7Szr8";
        public const string documentationUrl = "https://tools.continis.io/v/scene-notes";
        public const string otherToolsUrl = "https://assetstore.unity.com/publishers/87819";
        public const string supportEmail = "mailto:buoybase@gmail.com";

        // Package folder path
        public const string packageAssetsFolder = "Packages/io.continis.scene-notes";
        public const string uiToolkitTemplatesFolder = "UI/UIToolkit";
        public const string uiImagesFolder = "UI/Images";
        public const string iconsByStateFolder = "NoteIcons_State";
        public const string toolbarIconsFolder = "ToolbarIcons"; 
        
        // Log messages
        public const string packagePrefix = "(Scene Notes)";
        public const string noteNotFound = "Impossible to find the note in the scene. Please press the Reload button on the notes toolbar.;";
        public const string errorRenaming = "Error renaming asset. There was already another note with the same name, or the name contained illegal characters for a file name.";
        public const string errorMovingFile = "File couldn't be moved:";
        public const string sceneNotSavedError = "The current scene hasn't been saved yet. Please save the scene, reload Scene Notes, and try again.";
        public const string creatingNoteInUnsavedSceneError = "You are trying to create a note in a scene that hasn't been saved yet. Please save the scene, reload Scene Notes, and try again.";
    }
}