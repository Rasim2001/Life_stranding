using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SceneNotes.Editor
{
    public class WelcomeScreen : EditorWindow
    {
        [SerializeField] private VisualTreeAsset _visualTreeAsset;
        private static WelcomeScreen _window;

        [MenuItem(Constants.menuItemBaseName + "Welcome screen", false, 0)]
        public static void Init()
        {
            _window = GetWindow<WelcomeScreen>(true);
            _window.titleContent = new GUIContent("Scene Notes");
            _window.minSize = new Vector2(300f, 400f);
            _window.maxSize = new Vector2(300f, 400f);
        }

        private void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            VisualElement templateWindow = _visualTreeAsset.Instantiate();
            templateWindow.style.flexGrow = 1f;
            
            templateWindow.Q<Button>("DocsBtn").clicked += () => Application.OpenURL(Constants.documentationUrl);
            templateWindow.Q<Button>("EmailBtn").clicked += () => Application.OpenURL(Constants.supportEmail);
            templateWindow.Q<Button>("DiscordBtn").clicked += () => Application.OpenURL(Constants.discordUrl);
            templateWindow.Q<Button>("OtherToolsBtn").clicked += () => Application.OpenURL(Constants.otherToolsUrl);
            templateWindow.Q<Button>("ReviewBtn").clicked += () => Application.OpenURL(Constants.reviewUrl);
            
            root.Add(templateWindow);
        }
    }
}