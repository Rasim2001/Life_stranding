#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace SceneNotes
{
    public class SceneNoteBehaviour : MonoBehaviour
    {
        public static bool DisplayTitleInScene;
        public static Color TitleColour;
        public static Color TitleBackgroundColour;
        public static int MaxTitleLength;

        public SceneNote note;

#if UNITY_EDITOR
        [SerializeField] private float _maxWidth = 100f;
        
        private void OnDrawGizmos()
        {
            if (!DisplayTitleInScene) return;
            
            Camera sceneCamera = SceneView.lastActiveSceneView.camera;
            Vector3 viewPos = sceneCamera.WorldToViewportPoint(transform.position);
            
            if (viewPos.z <= 0 || viewPos.x < -0.1f || viewPos.x > 1.1f || viewPos.y < -0.1f || viewPos.y > 1.1f)
                return;
            
            float handleSize = HandleUtility.GetHandleSize(transform.position);
            float iconSize = GizmoUtility.iconSize;
            
            int fontSize = GizmoUtility.use3dIcons ? Mathf.Clamp(Mathf.RoundToInt(iconSize * 900f / handleSize), 1, 14) : 11;
            
            Color titleColour = TitleColour;
            Color titleBackgroundColour = TitleBackgroundColour;
            if (fontSize < 6)
            {
                float fadeFactor = (fontSize-1) * .18f;
                titleColour.a *= fadeFactor;
                titleBackgroundColour.a *= fadeFactor;
            }

            GUIStyle textStyle = new(EditorStyles.whiteLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = fontSize,
                wordWrap = true,
                normal = new GUIStyleState { textColor = titleColour }
            };

            Vector3 offset = SceneView.lastActiveSceneView.camera.transform.up * (GizmoUtility.use3dIcons ? iconSize * 18f : handleSize / 4.4f);
            Vector3 labelPosition = transform.position - offset;

            string noteTitle = note.title;
            if (noteTitle.Length > MaxTitleLength)
            {
                noteTitle = noteTitle.Remove(MaxTitleLength, noteTitle.Length - MaxTitleLength);
                noteTitle += "...";
            }
            GUIContent content = new GUIContent(noteTitle);
            
            Handles.BeginGUI();
            
            float scaledMaxWidth = _maxWidth * fontSize / 11f;
            
            Vector2 textSize = textStyle.CalcSize(content);
            if (textSize.x > scaledMaxWidth)
            {
                float height = textStyle.CalcHeight(content, scaledMaxWidth);
                textSize = new Vector2(scaledMaxWidth, height);
            }
            
            float padding = fontSize * 0.05f;
            Vector2 boxSize = new Vector2(textSize.x + padding * 2f, textSize.y + padding);
            
            Vector2 guiPosition = HandleUtility.WorldToGUIPoint(labelPosition);
            Rect boxRect = new Rect(guiPosition.x - boxSize.x * 0.5f, guiPosition.y, boxSize.x, boxSize.y);
            
            GUIStyle roundedStyle = new GUIStyle();
            roundedStyle.normal.background = EditorGUIUtility.whiteTexture;
            
            Color originalColor = GUI.color;
            GUI.color = titleBackgroundColour;
            
            GUI.Box(boxRect, GUIContent.none, roundedStyle);
            GUI.color = originalColor;
            
            GUI.Label(boxRect, content, textStyle);
            
            Handles.EndGUI();
        }
#endif
    }
}