using Infastructure.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Editor.World
{
    [CustomEditor(typeof(SegmentBounds))]
    public class SegmentBoundsEditor : UnityEditor.Editor
    {
        [DrawGizmo(GizmoType.Active | GizmoType.Pickable | GizmoType.NonSelected)]
        public static void RenderCustomGizmo(SegmentBounds bounds, GizmoType gizmoType)
        {
            Bounds xzBounds = GetSceneXZBounds(bounds.gameObject.scene);
            if (xzBounds.size.x <= 0f && xzBounds.size.z <= 0f)
                return;

            Gizmos.color = Color.cyan;
            DrawFrame(xzBounds, bounds.BottomY);
            Handles.Label(new Vector3(xzBounds.center.x, bounds.BottomY, xzBounds.min.z),
                $"BottomY {bounds.BottomY:0.##}");

            Gizmos.color = bounds.IsValid ? Color.green : Color.red;
            DrawFrame(xzBounds, bounds.TopY);
            Handles.Label(new Vector3(xzBounds.center.x, bounds.TopY, xzBounds.min.z),
                $"TopY {bounds.TopY:0.##}");
        }

        private static void DrawFrame(Bounds xzBounds, float y)
        {
            var a = new Vector3(xzBounds.min.x, y, xzBounds.min.z);
            var b = new Vector3(xzBounds.max.x, y, xzBounds.min.z);
            var c = new Vector3(xzBounds.max.x, y, xzBounds.max.z);
            var d = new Vector3(xzBounds.min.x, y, xzBounds.max.z);

            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, c);
            Gizmos.DrawLine(c, d);
            Gizmos.DrawLine(d, a);
        }

        private static Bounds GetSceneXZBounds(Scene scene)
        {
            bool hasBounds = false;
            var bounds = new Bounds();

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (!hasBounds)
                    {
                        bounds = renderer.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }
            }

            return bounds;
        }
    }
}
