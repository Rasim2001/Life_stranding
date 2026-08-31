using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Editor.World
{
    /// <summary>
    /// Read-only помощник для выбора шва этажа (тикет 08, план этап B): по каждому
    /// ребёнку корневых групп открытой сцены показывает Y-габариты рендереров и сторону
    /// относительно Split Y. Сцены не открывает и не закрывает — работает с уже открытой.
    /// </summary>
    public class SegmentSplitReportWindow : EditorWindow
    {
        private static readonly string[] RootGroupNames =
        {
            "-----TERRAINCUT-----",
            "------GEOMETRY-----!",
            "-------GROUND------"
        };

        private class Row
        {
            public string GroupName;
            public string ChildName;
            public bool HasRenderers;
            public float MinY;
            public float MaxY;
        }

        private float _splitY;
        private readonly List<Row> _rows = new List<Row>();
        private Vector2 _scroll;
        private string _statusMessage = "";
        private int _belowCount;
        private int _aboveCount;
        private int _straddlesCount;

        [MenuItem("GD Tools/Segments/Split Report")]
        public static void Open() => GetWindow<SegmentSplitReportWindow>("Split Report");

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            _splitY = EditorGUILayout.FloatField(new GUIContent("Split Y",
                "Высота шва. Таблица не живая — пересчитывается по кнопке."), _splitY, GUILayout.Width(220));

            if (GUILayout.Button("Scan open scene", GUILayout.Width(140)))
                Scan();

            GUILayout.Label($"below: {_belowCount} · above: {_aboveCount} · STRADDLES: {_straddlesCount}",
                EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_statusMessage))
                EditorGUILayout.HelpBox(_statusMessage, MessageType.Warning);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Group", EditorStyles.boldLabel, GUILayout.Width(200));
            GUILayout.Label("Child", EditorStyles.boldLabel, GUILayout.Width(260));
            GUILayout.Label("Y min .. max", EditorStyles.boldLabel, GUILayout.Width(160));
            GUILayout.Label("Side", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            foreach (Row row in _rows)
                DrawRow(row);

            EditorGUILayout.EndScrollView();
        }

        private void DrawRow(Row row)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(row.GroupName, GUILayout.Width(200));
            GUILayout.Label(row.ChildName, GUILayout.Width(260));

            if (row.HasRenderers)
            {
                GUILayout.Label($"{row.MinY:0.##} .. {row.MaxY:0.##}", GUILayout.Width(160));
                GUILayout.Label(DescribeSide(row), GUILayout.Width(100));
            }
            else
            {
                GUILayout.Label("no renderers", GUILayout.Width(160));
                GUILayout.Label("—", GUILayout.Width(100));
            }

            EditorGUILayout.EndHorizontal();
        }

        private string DescribeSide(Row row)
        {
            if (row.MaxY <= _splitY)
                return "below";
            if (row.MinY >= _splitY)
                return "above";
            return "STRADDLES";
        }

        private void Scan()
        {
            _rows.Clear();
            _statusMessage = "";
            _belowCount = 0;
            _aboveCount = 0;
            _straddlesCount = 0;

            Scene scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                _statusMessage = "Нет открытой сцены.";
                return;
            }

            var foundGroups = new HashSet<string>();

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (System.Array.IndexOf(RootGroupNames, root.name) < 0)
                    continue;

                foundGroups.Add(root.name);

                foreach (Transform child in root.transform)
                    _rows.Add(BuildRow(root.name, child));
            }

            foreach (string groupName in RootGroupNames)
            {
                if (!foundGroups.Contains(groupName))
                    _statusMessage += $"Группа «{groupName}» не найдена в открытой сцене. ";
            }

            foreach (Row row in _rows)
            {
                if (!row.HasRenderers)
                    continue;

                string side = DescribeSide(row);
                if (side == "below") _belowCount++;
                else if (side == "above") _aboveCount++;
                else _straddlesCount++;
            }
        }

        private static Row BuildRow(string groupName, Transform child)
        {
            var row = new Row { GroupName = groupName, ChildName = child.name };

            bool hasBounds = false;
            var bounds = new Bounds();

            foreach (Renderer renderer in child.GetComponentsInChildren<Renderer>(true))
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

            row.HasRenderers = hasBounds;
            row.MinY = bounds.min.y;
            row.MaxY = bounds.max.y;
            return row;
        }
    }
}
