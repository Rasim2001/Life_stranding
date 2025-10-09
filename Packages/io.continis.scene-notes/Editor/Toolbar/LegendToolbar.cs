using System.IO;
using SceneNotes.Editor;
using UnityEngine;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine.UIElements;

namespace SceneNotes.Toolbar
{
    [Overlay(typeof(SceneView), "Scene Notes Categories",
        defaultDockPosition = DockPosition.Bottom, defaultDockZone = DockZone.RightColumn, defaultLayout = Layout.HorizontalToolbar)]
    [Icon(Constants.packageAssetsFolder + "/" + Constants.uiImagesFolder + "/" + Constants.toolbarIconsFolder + "/CategoryOff.png")]
    public class LegendToolbar : Overlay, ICreateHorizontalToolbar, ICreateVerticalToolbar
    {
        private VisualElement _contentContainer;
        
        public override void OnCreated()
        {
            Constants.imagePrefix = EditorGUIUtility.isProSkin ? "d_" : "";
            NotesDatabase.instance.CategoriesUpdated += () => CreateContents(_contentContainer);
            
            base.OnCreated();
        }

        private void CreateContents(VisualElement visualElement)
        {
            _contentContainer = visualElement;
            
            if (_contentContainer == null) return;
            
            _contentContainer.Clear();
            
            _contentContainer.RegisterCallback<AttachToPanelEvent>(evt =>
            {
                string stylePath = Path.Combine(Constants.packageAssetsFolder, Constants.uiToolkitTemplatesFolder, "ToolbarStyles.uss");
                StyleSheet styles = AssetDatabase.LoadAssetAtPath<StyleSheet>(stylePath);
                VisualElement overlayContainer = _contentContainer.parent.parent.parent;
                VisualElement panel = _contentContainer.parent.parent;
                
                overlayContainer.styleSheets.Add(styles);
                overlayContainer.style.width = new StyleLength(StyleKeyword.Auto);

                // EditorApplication.delayCall += () =>
                // {
                //     Color currentColor = panel.resolvedStyle.backgroundColor;
                //     Color newColor = new Color(currentColor.r, currentColor.g, currentColor.b, .4f);
                //     panel.style.backgroundColor = newColor;
                // };
            });

            Button categoriesButton = new Button(OnCategoriesButtonClicked);
            categoriesButton.AddToClassList("categories-button");
            categoriesButton.tooltip = "Select the NoteCategories ScriptableObject.";

#if UNITY_6000_0_OR_NEWER
            if(activeLayout != Layout.HorizontalToolbar)
            {
                categoriesButton.style.maxWidth = 38;
            }
#endif

            Image buttonIcon = new Image();
            buttonIcon.AddToClassList("categories-button-icon");
            string iconPath = Path.Combine(Constants.packageAssetsFolder, Constants.uiImagesFolder, Constants.toolbarIconsFolder, $"{Constants.imagePrefix}CategoryOff.png");
            buttonIcon.image = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            categoriesButton.Add(buttonIcon);
            _contentContainer.Add(categoriesButton);
            
            VisualElement spacer = new();
            spacer.AddToClassList("spacer");
            _contentContainer.Add(spacer);

            foreach (NoteCategories.NoteCategory noteCategory in NoteCategories.Instance.Categories)
            {
                VisualElement categoryLine = new();
                categoryLine.AddToClassList("legend-category-line");

                Image image = new Image();
                image.image = noteCategory.icon;
                image.AddToClassList("legend-icon");

                Label label = new Label(noteCategory.name);
                image.AddToClassList("legend-label");

                categoryLine.Add(image);
                categoryLine.Add(label);

                _contentContainer.Add(categoryLine);

                spacer = new();
                spacer.AddToClassList("spacer");
                _contentContainer.Add(spacer);
            }
        }

        private void OnCategoriesButtonClicked()
        {
            EditorGUIUtility.PingObject(NoteCategories.Instance);
            Selection.activeObject = NoteCategories.Instance;
        }

        public override VisualElement CreatePanelContent()
        {
            VisualElement container = new();
            CreateContents(container);
            return container;
        }

        public OverlayToolbar CreateHorizontalToolbarContent()
        {
            OverlayToolbar overlayToolbar = new();
            CreateContents(overlayToolbar);
            return overlayToolbar;
        }

        public OverlayToolbar CreateVerticalToolbarContent()
        {
            OverlayToolbar overlayToolbar = new();
            CreateContents(overlayToolbar);
            return overlayToolbar;
        }
    }
}