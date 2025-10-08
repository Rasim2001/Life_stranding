using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SceneNotes.Editor.CustomEditors
{
    public partial class SceneNoteEditor
    {
        private Rect _screenshotRect;
        private bool _isScreenshotModeActive;
        private bool _isResizing;

        // Configurable screenshot settings
        private const float ResizeHandleSize = 26f;
        private const int MinCaptureWidth = 100;
        private const int MinCaptureHeight = 100;

        private GUIStyle _overlayStyle;
        private GUIStyle _resizeHandleStyle;
        private Texture2D _semiBlackTexture;
        private GUIContent _resizeIconContent;

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;

            _semiBlackTexture = CreateColorTexture(new Color(0f, 0f, 0f, 0.5f));
            _resizeIconContent = EditorGUIUtility.IconContent("Grid.MoveTool@2x");
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void ResetScreenshotRect()
        {
            // Get the current scene view
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null) return;

            // Calculate center of the scene view
            Rect viewRect = sceneView.position;
            float centerX = viewRect.width / 2;
            float centerY = viewRect.height / 2;

            // Set a default size
            float defaultWidth = 400f;
            float defaultHeight = 300f;

            _screenshotRect = new Rect(
                centerX - defaultWidth / 2,
                centerY - defaultHeight / 2,
                defaultWidth,
                defaultHeight
            );
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (target == null)
            {
                _isScreenshotModeActive = false;
                return;
            }

            if (!_isScreenshotModeActive || sceneView == null) return;

            _overlayStyle = new GUIStyle
            { normal = { background = _semiBlackTexture, scaledBackgrounds = new []{_semiBlackTexture}} };

            int padding = 5;
            _resizeHandleStyle = new GUIStyle(GUI.skin.button)
            { alignment = TextAnchor.MiddleCenter, padding = new RectOffset(padding,padding,padding,padding), };

            Handles.BeginGUI();
            DrawHoleOverlay(_screenshotRect, sceneView);
            DrawScreenshotRect();
            Handles.EndGUI();

            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.MouseDown:
                    HandleMouseDown(evt);
                    break;
                
                case EventType.MouseDrag:
                    HandleMouseDrag(evt);
                    break;
                
                case EventType.MouseUp:
                    HandleMouseUp(evt);
                    break;
                
                case EventType.KeyDown:
                    if (evt.keyCode == KeyCode.Escape)
                    {
                        _isScreenshotModeActive = false;
                        evt.Use();
                        SceneView.RepaintAll();
                    }

                    break;
            }

            // Check for mouse click outside the screenshot rect
            if (_isScreenshotModeActive && evt.type == EventType.MouseDown)
            {
                Vector2 mousePos = evt.mousePosition;

                Rect buttonRect = new(
                    _screenshotRect.x,
                    _screenshotRect.y + _screenshotRect.height + 10,
                    _screenshotRect.width,
                    30
                );

                // Check if click is outside screenshot rect and button area
                if (!_screenshotRect.Contains(mousePos) &&
                    !buttonRect.Contains(mousePos) &&
                    evt.button == 0) // Left mouse button
                {
                    _isScreenshotModeActive = false;
                    evt.Use();
                    SceneView.RepaintAll();
                }
            }

            // Repaint to ensure continuous updates
            SceneView.RepaintAll();
        }

        private void DrawHoleOverlay(Rect screenshotRect, SceneView sceneView)
        {
            float viewWidth = sceneView.position.width;
            float viewHeight = sceneView.position.height;

            Rect[] overlayRects = new Rect[]
            {
                // Top overlay
                new(0, 0, viewWidth, screenshotRect.y),

                // Bottom overlay
                new(0, screenshotRect.y + screenshotRect.height, viewWidth,
                    viewHeight - (screenshotRect.y + screenshotRect.height)),

                // Left overlay
                new(0, screenshotRect.y, screenshotRect.x, screenshotRect.height),

                // Right overlay
                new(screenshotRect.x + screenshotRect.width, screenshotRect.y,
                    viewWidth - (screenshotRect.x + screenshotRect.width), screenshotRect.height)
            };

            foreach (Rect rect in overlayRects) GUI.Box(rect, GUIContent.none, _overlayStyle);
        }

        private void DrawScreenshotRect()
        {
            Rect buttonRect = new(
                _screenshotRect.x,
                _screenshotRect.y + _screenshotRect.height + 10,
                _screenshotRect.width,
                30
            );

            GUILayout.BeginArea(buttonRect);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Capture", GUILayout.Width(buttonRect.width * .7f - 1f))) CaptureScreenshot();
            if (GUILayout.Button("Cancel", GUILayout.Width(buttonRect.width * .3f - 2f))) _isScreenshotModeActive = false;
            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();

            // Resize handle
            Rect resizeHandleRect = new(
                _screenshotRect.x + _screenshotRect.width - ResizeHandleSize * .75f,
                _screenshotRect.y - ResizeHandleSize * .25f,
                ResizeHandleSize,
                ResizeHandleSize
            );

            GUIContent content = new(_resizeIconContent);
            GUI.Box(resizeHandleRect, content, _resizeHandleStyle);
        }

        private void HandleMouseDown(Event evt)
        {
            Vector2 mousePos = evt.mousePosition;
            
            if(evt.alt ||
               evt.button != 0)
            {
                return;
            }

            // Check if mouse is on resize handle
            Rect resizeHandleRect = new(
                _screenshotRect.x + _screenshotRect.width - ResizeHandleSize * .75f,
                _screenshotRect.y - ResizeHandleSize * .25f,
                ResizeHandleSize,
                ResizeHandleSize
            );

            if (resizeHandleRect.Contains(mousePos))
            {
                _isResizing = true;
                evt.Use();
            }
            else
            {
                _isScreenshotModeActive = false;
            }
        }

        private void HandleMouseDrag(Event evt)
        {
            if (_isResizing)
            {
                // Calculate the change from the original point
                float widthDelta = evt.mousePosition.x - (_screenshotRect.x + _screenshotRect.width);
                float heightDelta = evt.mousePosition.y - _screenshotRect.y;

                // Calculate new width and height
                float newWidth = _screenshotRect.width + widthDelta * 2;
                float newHeight = _screenshotRect.height - heightDelta * 2;

                // Ensure minimum size
                newWidth = Mathf.Max(newWidth, MinCaptureWidth);
                newHeight = Mathf.Max(newHeight, MinCaptureHeight);

                // Adjust the rectangle to keep it centered
                _screenshotRect.x += (_screenshotRect.width - newWidth) / 2;
                _screenshotRect.y += (_screenshotRect.height - newHeight) / 2;
                _screenshotRect.width = newWidth;
                _screenshotRect.height = newHeight;

                evt.Use();
            }
        }


        private void HandleMouseUp(Event evt)
        {
            _isResizing = false;
        }

        private void CaptureScreenshot()
        {
            // Cache Gizmos state and disable them
            bool drawGizmos = SceneView.currentDrawingSceneView.drawGizmos;
            SceneView.currentDrawingSceneView.drawGizmos = false;

            // Ensure screenshots folder exists
            string screenshotFolder = Path.Join("Assets", SceneNotesSettings.unityFolder.value, "Screenshots");
            if (!Directory.Exists(screenshotFolder)) Directory.CreateDirectory(screenshotFolder);

            string filename = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string fullPath = Path.Combine(screenshotFolder, filename);

            // Capture screenshot
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null) return;

            int x = (int)_screenshotRect.x;
            int y = (int)(sceneView.position.height - (_screenshotRect.y + _screenshotRect.height));
            int width = (int)_screenshotRect.width;
            int height = (int)_screenshotRect.height;

            RenderTexture fullViewportRT = new(
                (int)sceneView.position.width,
                (int)sceneView.position.height,
                24
            );
            sceneView.camera.targetTexture = fullViewportRT;
            sceneView.camera.Render();

            Texture2D fullViewportTexture = new(
                (int)sceneView.position.width,
                (int)sceneView.position.height,
                TextureFormat.RGB24,
                false
            );

            RenderTexture.active = fullViewportRT;
            fullViewportTexture.ReadPixels(
                new Rect(0, 0, sceneView.position.width, sceneView.position.height),
                0,
                0
            );
            fullViewportTexture.Apply();

            Texture2D screenShot = new(width, height, TextureFormat.RGB24, false);

            Color[] pixels = fullViewportTexture.GetPixels(
                x,
                y,
                width,
                height
            );
            screenShot.SetPixels(pixels);
            screenShot.Apply();

            // Clean up
            sceneView.camera.targetTexture = null;
            RenderTexture.active = null;
            DestroyImmediate(fullViewportRT);
            DestroyImmediate(fullViewportTexture);

            byte[] bytes = screenShot.EncodeToPNG();
            File.WriteAllBytes(fullPath, bytes);

            AssetDatabase.Refresh();

            // Load the screenshot and add it to the ScriptableObject
            Texture2D savedScreenshot = AssetDatabase.LoadAssetAtPath<Texture2D>(fullPath);

            if (savedScreenshot != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(savedScreenshot);
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

                if (importer != null)
                {
                    importer.npotScale = TextureImporterNPOTScale.None;
                    importer.SaveAndReimport();
                }

                SceneNote sceneNote = (SceneNote)target;
                sceneNote.screenshots.Add(savedScreenshot);
                SaveModifiedDate();
                EditorUtility.SetDirty(target);
            }

            if(drawGizmos) SceneView.currentDrawingSceneView.drawGizmos = true;
            _isScreenshotModeActive = false;
            SceneView.RepaintAll();
        }

        private Texture2D CreateColorTexture(Color color)
        {
            Texture2D texture = new(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void EnterScreenshotMode()
        {
            ResetScreenshotRect();
            _isScreenshotModeActive = true;
            SceneView.RepaintAll();
        }
    }
}