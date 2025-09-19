using Common.SceneMarkers;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(BatteryPointMarker))]
    public class BatteryPointEditor : UnityEditor.Editor
    {
        [DrawGizmo(GizmoType.Active | GizmoType.Pickable | GizmoType.NonSelected)]
        public static void RenderCustomGizmo(BatteryPointMarker spawner, GizmoType gizmo)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(spawner.transform.position, 0.5f);
        }
    }
}