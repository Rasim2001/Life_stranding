using Common.SceneMarkers;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(CheckPointMarker))]
    public class CheckPointMarkerEditor : UnityEditor.Editor
    {
        [DrawGizmo(GizmoType.Active | GizmoType.Pickable | GizmoType.NonSelected)]
        public static void RenderCustomGizmo(CheckPointMarker spawner, GizmoType gizmo)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(spawner.transform.position, 0.5f);
        }
    }
}