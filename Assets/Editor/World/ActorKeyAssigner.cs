using System;
using System.Collections.Generic;
using Common;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Editor.World
{
    /// <summary>
    /// Автоматически выдаёт ключ памяти (<see cref="MarkerUniqueId.UniqueId"/>) экземплярам
    /// акторов в момент постановки в сцену — перетаскивание префаба, Ctrl+D, вставка.
    /// Правка исходного префаба и перенос существующего инстанса такого события не рождают,
    /// поэтому ключ, однажды выданный, стабилен. См. .scratch/plans/ancient-wibbling-sprout.md.
    /// </summary>
    [InitializeOnLoad]
    public static class ActorKeyAssigner
    {
        static ActorKeyAssigner()
        {
            ObjectChangeEvents.changesPublished += OnChangesPublished;
        }

        private static void OnChangesPublished(ref ObjectChangeEventStream stream)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            var newHierarchies = new List<MarkerUniqueId[]>();
            HashSet<MarkerUniqueId> newlyCreatedMarkers = null;

            for (int i = 0; i < stream.length; i++)
            {
                if (stream.GetEventType(i) != ObjectChangeKind.CreateGameObjectHierarchy)
                    continue;

                stream.GetCreateGameObjectHierarchyEvent(i, out CreateGameObjectHierarchyEventArgs args);

                var go = EditorUtility.InstanceIDToObject(args.instanceId) as GameObject;
                if (go == null)
                    continue;

                if (PrefabStageUtility.GetCurrentPrefabStage() != null || EditorUtility.IsPersistent(go))
                    continue;

                MarkerUniqueId[] markers = go.GetComponentsInChildren<MarkerUniqueId>(includeInactive: true);
                if (markers.Length == 0)
                    continue;

                newHierarchies.Add(markers);

                newlyCreatedMarkers ??= new HashSet<MarkerUniqueId>();
                foreach (MarkerUniqueId marker in markers)
                    newlyCreatedMarkers.Add(marker);
            }

            if (newHierarchies.Count == 0)
                return;

            HashSet<string> occupiedKeys = CollectOccupiedKeys(newlyCreatedMarkers);

            foreach (MarkerUniqueId[] markers in newHierarchies)
                foreach (MarkerUniqueId marker in markers)
                    AssignIfNeeded(marker, occupiedKeys);
        }

        private static HashSet<string> CollectOccupiedKeys(HashSet<MarkerUniqueId> excluded)
        {
            var keys = new HashSet<string>();
            foreach (MarkerUniqueId marker in UnityEngine.Object.FindObjectsByType<MarkerUniqueId>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (excluded.Contains(marker))
                    continue;

                if (!string.IsNullOrEmpty(marker.UniqueId))
                    keys.Add(marker.UniqueId);
            }

            return keys;
        }

        private static void AssignIfNeeded(MarkerUniqueId marker, HashSet<string> occupiedKeys)
        {
            bool needsNewKey = string.IsNullOrEmpty(marker.UniqueId) || occupiedKeys.Contains(marker.UniqueId);
            if (!needsNewKey)
            {
                occupiedKeys.Add(marker.UniqueId);
                return;
            }

            Undo.RecordObject(marker, "Assign actor key");
            marker.UniqueId = Guid.NewGuid().ToString();
            PrefabUtility.RecordPrefabInstancePropertyModifications(marker);
            EditorUtility.SetDirty(marker);
            EditorSceneManager.MarkSceneDirty(marker.gameObject.scene);

            occupiedKeys.Add(marker.UniqueId);
        }
    }
}
