using System.Collections.Generic;
using System.Linq;
using SceneNotes.Toolbar;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SceneNotes.Editor.CustomEditors
{
    [CustomEditor(typeof(NoteCategories))]
    public class NoteCategoriesEditor : UnityEditor.Editor
    {
        public VisualTreeAsset template;
        private ListView _listView;
        private HelpBox _iconStyleWarningBox;

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement inspector = new VisualElement();
            template.CloneTree(inspector);

            Button button = inspector.Q<Button>("OpenLegendOverlayBtn");
            
#if UNITY_6000_0_OR_NEWER
            button.clicked += OnLegendOverlayButtonClicked;
#else
            button.RemoveFromHierarchy();            
#endif

            _listView = inspector.Q<ListView>("CategoriesList");
            _listView.TrackPropertyValue(serializedObject.FindProperty("_categories"), _ => OnListChanged());

            _iconStyleWarningBox = inspector.Q<HelpBox>("IconStyleWarningBox");
            OnNoteIconStyleChanged(SceneNotesSettings.noteIconStyle);
            
            SceneNotesSettings.NoteIconStyleChanged += OnNoteIconStyleChanged;

            return inspector;
        }

#if UNITY_6000_0_OR_NEWER
      private void OnLegendOverlayButtonClicked()
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null) return;
            
            Overlay overlay = sceneView.overlayCanvas.overlays.FirstOrDefault(o => o is LegendToolbar);
            if (overlay == null)
            {
                overlay = new LegendToolbar();
                SceneView.AddOverlayToActiveView(overlay);
            }
            overlay.displayed = true;
        }  
#endif

        private void OnNoteIconStyleChanged(SceneNotesSettings.NoteIconStyle newStyle)
        {
            _iconStyleWarningBox.style.display = newStyle == SceneNotesSettings.NoteIconStyle.State
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        // Refresh notes if the categories have changed
        private void OnListChanged()
        {
            NotesDatabase.instance.NotifyCategoriesUpdate();
            if (EditingSession.IsShowingNotes()) NotesDatabase.instance.LoadAllNotes();
            if (NotesBrowser.IsOpen) NotesBrowser.RefreshCategories();
        }
    }
}