using Common.SceneMarkers;
using Infastructure.CutScene;
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

    [CustomEditor(typeof(CutSceneTargetMarker))]
    public class CutScenePointEditor : UnityEditor.Editor
    {
        [DrawGizmo(GizmoType.Active | GizmoType.Pickable | GizmoType.NonSelected)]
        public static void RenderCustomGizmo(CutSceneTargetMarker spawner, GizmoType gizmo)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawCube(spawner.transform.position, new Vector3(1, 1, 1));
        }
    }


    [CustomEditor(typeof(FlowerPointMarker))]
    public class FlowerPointEditor : UnityEditor.Editor
    {
        [DrawGizmo(GizmoType.Active | GizmoType.Pickable | GizmoType.NonSelected)]
        public static void RenderCustomGizmo(FlowerPointMarker spawner, GizmoType gizmo)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(spawner.transform.position, 0.3f);
        }
    }

    [CustomEditor(typeof(ProductSkillPointMarker))]
    public class ProductSkillsEditor : UnityEditor.Editor
    {
        [DrawGizmo(GizmoType.Active | GizmoType.Pickable | GizmoType.NonSelected)]
        public static void RenderCustomGizmo(ProductSkillPointMarker spawner, GizmoType gizmo)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(spawner.transform.position, 0.3f);
        }
    }

    [CustomEditor(typeof(CheckPointMarker))]
    public class CheckPointEditor : UnityEditor.Editor
    {
        [DrawGizmo(GizmoType.Active | GizmoType.Pickable | GizmoType.NonSelected)]
        public static void RenderCustomGizmo(CheckPointMarker spawner, GizmoType gizmo)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(spawner.transform.position, 0.3f);
        }
    }
}