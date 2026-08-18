using UnityEditor;
using UnityEngine;

namespace SpiderRig.Editor.Weather
{
    // Параметрическая UV-полусфера/чаша для купольного неба и облаков — свой меш вместо
    // авторского FBX у Cozy (их контент лицензионный, брать нельзя, приём — да).
    //
    // Один полюс наверху (зенит), кольца вниз до heightFraction*180° от зенита. При
    // heightFraction=1 (полная сфера, нужна для неба — у кози их Skydome тоже сфера,
    // не купол) достраивается нижний полюс — без него последнее кольцо на theta=π
    // схлопывается в вырожденное кольцо треугольников нулевой площади вместо честного
    // полюса. Шов по долготе продублирован отдельным столбцом вершин (u=0 и u=1
    // в одной точке) — стандартный приём UV-сферы, без него на будущих текстурных
    // слоях был бы разрыв UV на шве.
    //
    // yFlatten сплющивает высоту уже ПОСЛЕ построения сферы — тем самым разница
    // "небо = полная сфера, облака = пологая чаша" достигается формой геометрии,
    // а не проекцией в шейдере (см. docs в SHD_Weather_DomeClouds.shader).
    public class WeatherDomeMeshGenerator : EditorWindow
    {
        private float _radius = 1f;
        private int _longitudeSegments = 32;
        private int _latitudeSegments = 16;
        [Range(0.05f, 1f)] private float _heightFraction = 0.58f;
        [Range(0.05f, 1f)] private float _yFlatten = 1f;
        private string _assetName = "MSH_Weather_DomeSky";

        [MenuItem("SpiderRig/Weather/Generate Dome Mesh...")]
        private static void Open() => GetWindow<WeatherDomeMeshGenerator>("Weather Dome Mesh");

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Купольный меш неба/облаков", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Небо: heightFraction ~0.58 (чуть за горизонт), yFlatten 1.0.\n" +
                "Облака: heightFraction ~0.35, yFlatten ~0.3-0.4 — пологая чаша.",
                MessageType.None);

            _radius = EditorGUILayout.FloatField("Radius", _radius);
            _longitudeSegments = EditorGUILayout.IntSlider("Longitude Segments", _longitudeSegments, 8, 96);
            _latitudeSegments = EditorGUILayout.IntSlider("Latitude Segments", _latitudeSegments, 4, 48);
            _heightFraction = EditorGUILayout.Slider("Height Fraction (доля сферы)", _heightFraction, 0.05f, 1f);
            _yFlatten = EditorGUILayout.Slider("Y Flatten", _yFlatten, 0.05f, 1f);
            _assetName = EditorGUILayout.TextField("Asset Name", _assetName);

            EditorGUILayout.Space();
            if (GUILayout.Button("Generate & Save"))
                GenerateAndSave();
        }

        private void GenerateAndSave()
        {
            Mesh mesh = Generate(_radius, _longitudeSegments, _latitudeSegments, _heightFraction, _yFlatten);
            mesh.name = _assetName;

            const string folder = "Assets/Art/Models/Weather";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Art/Models"))
                    AssetDatabase.CreateFolder("Assets/Art", "Models");
                AssetDatabase.CreateFolder("Assets/Art/Models", "Weather");
            }

            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{_assetName}.asset");
            AssetDatabase.CreateAsset(mesh, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(mesh);
            Debug.Log($"WeatherDomeMeshGenerator: сохранён {path} (verts={mesh.vertexCount}, tris={mesh.triangles.Length / 3})");
        }

        public static Mesh Generate(float radius, int longitudeSegments, int latitudeSegments, float heightFraction, float yFlatten)
        {
            float thetaMax = heightFraction * Mathf.PI;

            // Полная сфера: последнее кольцо при theta=π вырождается (sin(π)≈0, все его
            // вершины сходятся в одну точку) — строим настоящий южный полюс вместо него,
            // а не тратим треугольники на кольцо нулевой площади.
            bool hasBottomPole = heightFraction >= 1f;
            int realRingCount = hasBottomPole ? latitudeSegments - 1 : latitudeSegments;

            // Столбцов по долготе на кольцо на один больше сегментов — последний
            // дублирует первый по позиции, но с u=1 вместо u=0 (шов).
            int columns = longitudeSegments + 1;
            int ringVertexCount = columns * realRingCount;
            int vertexCount = 1 + ringVertexCount + (hasBottomPole ? 1 : 0); // +1 северный полюс [+1 южный]

            var vertices = new Vector3[vertexCount];
            var uvs = new Vector2[vertexCount];

            vertices[0] = new Vector3(0f, radius * yFlatten, 0f);
            uvs[0] = new Vector2(0.5f, 0f);

            for (int lat = 1; lat <= realRingCount; lat++)
            {
                // Знаменатель — исходный latitudeSegments, не realRingCount: угловой шаг
                // между кольцами не меняется от того, что последнее кольцо заменено полюсом.
                float theta = thetaMax * lat / latitudeSegments;
                float y = Mathf.Cos(theta) * yFlatten;
                float ringRadius = Mathf.Sin(theta);
                float v = (float)lat / latitudeSegments;

                for (int lon = 0; lon < columns; lon++)
                {
                    float phi = 2f * Mathf.PI * lon / longitudeSegments;
                    float x = ringRadius * Mathf.Cos(phi);
                    float z = ringRadius * Mathf.Sin(phi);

                    int index = 1 + (lat - 1) * columns + lon;
                    vertices[index] = new Vector3(x, y, z) * radius;
                    uvs[index] = new Vector2((float)lon / longitudeSegments, v);
                }
            }

            int bottomPoleIndex = vertexCount - 1;
            if (hasBottomPole)
            {
                vertices[bottomPoleIndex] = new Vector3(0f, -radius * yFlatten, 0f);
                uvs[bottomPoleIndex] = new Vector2(0.5f, 1f);
            }

            // Треугольники смотрят наружу (обычный порядок обхода) — шейдеры рисуют
            // Cull Front, камера физически внутри купола, видит изнанку.
            int fanTriCount = columns - 1;
            int quadRingCount = realRingCount - 1;
            int bottomFanTriCount = hasBottomPole ? columns - 1 : 0;
            int triangleCount = fanTriCount + quadRingCount * (columns - 1) * 2 + bottomFanTriCount;
            var triangles = new int[triangleCount * 3];
            int t = 0;

            // Порядок обхода подобран замером: с (0, 1+lon, 1+lon+1) нормаль треугольника
            // (по левосторонней конвенции Unity) смотрела внутрь купола, и камера в центре
            // видела ЛИЦЕВУЮ сторону — Cull Front (расчёт на обратное, см. комментарий выше)
            // вырезал всё целиком, меш был невидим при формально верной сборке. Порядок ниже
            // даёт нормаль наружу, как и предполагает Cull Front.
            for (int lon = 0; lon < columns - 1; lon++)
            {
                triangles[t++] = 0;
                triangles[t++] = 1 + lon + 1;
                triangles[t++] = 1 + lon;
            }

            for (int lat = 0; lat < realRingCount - 1; lat++)
            {
                int ringStart = 1 + lat * columns;
                int nextRingStart = ringStart + columns;

                for (int lon = 0; lon < columns - 1; lon++)
                {
                    int a = ringStart + lon;
                    int b = ringStart + lon + 1;
                    int c = nextRingStart + lon;
                    int d = nextRingStart + lon + 1;

                    triangles[t++] = a;
                    triangles[t++] = b;
                    triangles[t++] = c;

                    triangles[t++] = b;
                    triangles[t++] = d;
                    triangles[t++] = c;
                }
            }

            if (hasBottomPole)
            {
                // Зеркало верхнего веера относительно экватора: у отражения меняется
                // хиральность, поэтому порядок обхода здесь обратный северному —
                // иначе южная шапка получит нормаль внутрь и исчезнет при Cull Front
                // ровно как исчезал весь купол до починки winding в этом же файле.
                int lastRingStart = 1 + (realRingCount - 1) * columns;
                for (int lon = 0; lon < columns - 1; lon++)
                {
                    triangles[t++] = bottomPoleIndex;
                    triangles[t++] = lastRingStart + lon;
                    triangles[t++] = lastRingStart + lon + 1;
                }
            }

            var mesh = new Mesh();
            mesh.indexFormat = vertexCount > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            // Нормали шейдерам не нужны (направление берётся из object-space позиции
            // вершины), но пересчитываем для корректного отображения в инспекторе/Gizmos.
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
