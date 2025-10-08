using UnityEditor;
using UnityEngine;

namespace SceneNotes.Editor
{
    public static class MenuItems
    {
        [InitializeOnLoadMethod]
        public static void CheckShowWelcomeScreen()
        {
            if (!SceneNotesSettings.WelcomeWindowSeen.value)
            {
                EditorApplication.delayCall += WelcomeScreen.Init;
                SceneNotesSettings.WelcomeWindowSeen.SetValue(true, true);
            }
        }
        
        [MenuItem(Constants.menuItemBaseName + "Documentation", false, 100)]
        public static void OpenDocumentation() => Application.OpenURL(Constants.documentationUrl);
        
        [MenuItem(Constants.menuItemBaseName + "Email support", false, 101)]
        public static void EmailSupport() => Application.OpenURL(Constants.supportEmail);
        
        [MenuItem(Constants.menuItemBaseName + "Join Discord", false, 102)]
        public static void JoinDiscord() => Application.OpenURL(Constants.reviewUrl);
        
        [MenuItem(Constants.menuItemBaseName + "Write a review", false, 105)]
        public static void LeaveAReview() => Application.OpenURL(Constants.reviewUrl);
    }
}