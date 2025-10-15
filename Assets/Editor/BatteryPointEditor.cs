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

    [CustomEditor(typeof(EnergyPointMarker))]
    public class EnergyPointEditor : UnityEditor.Editor
    {
        [DrawGizmo(GizmoType.Active | GizmoType.Pickable | GizmoType.NonSelected)]
        public static void RenderCustomGizmo(EnergyPointMarker spawner, GizmoType gizmo)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(spawner.transform.position, 0.5f);
        }
    }


    [CustomEditor(typeof(ElephantPointMarker))]
    public class ElephantPointEditor : UnityEditor.Editor
    {
        [DrawGizmo(GizmoType.Active | GizmoType.Pickable | GizmoType.NonSelected)]
        public static void RenderCustomGizmo(ElephantPointMarker spawner, GizmoType gizmo)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(spawner.transform.position, 0.5f);
        }
    }
}