using System.Collections.Generic;
using Infastructure.StaticData.WeatherSystem;
using UnityEditor;
using UnityEngine;
using WeatherSystem;
using WeatherSystem.Profiles;

namespace SpiderRig.Editor.Weather
{
    // Единая панель управления погодой — вкладки Lighting/Fog/Clouds/Celestials/Bands,
    // как у Cozy Atmosphere Profile, но окно, а не инспектор: профили и WeatherData.asset
    // не привязаны к сцене (в отличие от CozyWeather на сценовом компоненте), риг нужен
    // только для превью.
    //
    // Правка через SerializedObject/SerializedProperty — отход от конвенции проекта
    // (обычно прямой доступ к полям + Undo.RecordObject + SetDirty, см. ProjectScenesWindow),
    // но обязателен: у DailyColor/DailyFloat поля приватные, прямым доступом не достать.
    // Заодно Undo и dirty-разметка идут автоматически через ApplyModifiedProperties.
    public class WeatherControlPanel : EditorWindow
    {
        private const string WeatherDataAssetPath = "Assets/Resources/StaticData/WeatherSystem/WeatherData.asset";
        private static readonly string[] TabLabels = { "Lighting", "Fog", "Clouds", "Celestials", "Bands" };

        [SerializeField] private int _tab;
        [SerializeField] private int _selectedBandIndex;
        [SerializeField] private float _timeOfDay01 = 0.44f;
        [SerializeField] private float _worldY;
        [SerializeField] private bool _previewEnabled;
        [SerializeField] private int _selectedRigIndex;
        [SerializeField] private WeatherStaticData _staticData;

        private SerializedObject _staticDataSO;
        private SerializedObject _profileSO;
        private SkyBandProfile _lastBoundProfile;
        private WeatherPreviewDriver _driver;
        private Vector2 _scroll;

        [MenuItem("SpiderRig/Weather/Control Panel")]
        public static void Open() => GetWindow<WeatherControlPanel>("Weather Control");

        private void OnEnable()
        {
            titleContent = new GUIContent("Weather Control");

            if (_staticData == null)
                _staticData = AssetDatabase.LoadAssetAtPath<WeatherStaticData>(WeatherDataAssetPath);

            _driver = new WeatherPreviewDriver();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            // Домен мог перезагрузиться, пока превью было включено (например, после
            // компиляции скрипта) — тумблер это пережил (SerializeField на EditorWindow),
            // а сам драйвер и его снимок состояния сцены — нет. Возобновляем со свежим
            // снимком: небольшой смысловой компромисс (см. план), но безопасный.
            if (_previewEnabled)
            {
                WeatherRig rig = ResolveRig();
                if (rig != null)
                {
                    _driver.Start(rig);
                    EditorApplication.update += OnEditorUpdate;
                }
                else
                {
                    _previewEnabled = false;
                }
            }
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            if (_driver != null && _driver.IsRunning)
            {
                EditorApplication.update -= OnEditorUpdate;
                _driver.Stop();
            }
        }

        // Превью и WeatherService не должны писать в один купол одновременно —
        // выключаем превью перед входом в Play Mode, дальше управление у сервиса.
        private void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode && _previewEnabled)
                SetPreviewEnabled(false);
        }

        private void OnEditorUpdate()
        {
            if (_driver == null || !_driver.IsRunning)
                return;

            _driver.Apply(_staticData, _worldY, _timeOfDay01);
            SceneView.RepaintAll();
        }

        private void SetPreviewEnabled(bool enabled)
        {
            _previewEnabled = enabled;

            if (enabled)
            {
                WeatherRig rig = ResolveRig();
                if (rig == null)
                {
                    EditorUtility.DisplayDialog("Weather Control",
                        "В загруженных сценах не найден WeatherRig — превью недоступно.", "OK");
                    _previewEnabled = false;
                    return;
                }

                _driver.Start(rig);
                EditorApplication.update += OnEditorUpdate;
            }
            else
            {
                EditorApplication.update -= OnEditorUpdate;
                _driver.Stop();
            }
        }

        private static WeatherRig[] FindRigs() =>
            Object.FindObjectsByType<WeatherRig>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        private WeatherRig ResolveRig()
        {
            WeatherRig[] rigs = FindRigs();
            if (rigs.Length == 0)
                return null;

            _selectedRigIndex = Mathf.Clamp(_selectedRigIndex, 0, rigs.Length - 1);
            return rigs[_selectedRigIndex];
        }

        private void OnGUI()
        {
            if (_staticData == null)
            {
                EditorGUILayout.HelpBox($"WeatherStaticData не найден: {WeatherDataAssetPath}", MessageType.Warning);
                if (GUILayout.Button("Повторить", GUILayout.Width(100)))
                    _staticData = AssetDatabase.LoadAssetAtPath<WeatherStaticData>(WeatherDataAssetPath);
                return;
            }

            if (_staticDataSO == null)
                _staticDataSO = new SerializedObject(_staticData);

            DrawHeader();
            EditorGUILayout.Space(4);

            _tab = GUILayout.Toolbar(_tab, TabLabels);
            EditorGUILayout.Space(4);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            if (_tab == 4)
            {
                DrawBandsTab();
            }
            else
            {
                SkyBandProfile profile = GetSelectedProfile();
                if (profile == null)
                {
                    EditorGUILayout.HelpBox("У выбранной полосы не назначен Profile.", MessageType.Warning);
                }
                else
                {
                    BindProfile(profile);

                    switch (_tab)
                    {
                        case 0: DrawLightingTab(); break;
                        case 1: DrawFogTab(); break;
                        case 2: DrawCloudsTab(); break;
                        case 3: DrawCelestialsTab(); break;
                    }

                    // SaveAssets на каждое применённое изменение, а не по таймеру/кнопке:
                    // без этого правки живут только в памяти сессии редактора и не
                    // переживают перезапуск Unity, пока их не сохранят чем-то другим.
                    if (_profileSO.ApplyModifiedProperties())
                        AssetDatabase.SaveAssets();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            SkyBand[] bands = _staticData.Bands;

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Профиль", GUILayout.Width(60));

            if (bands != null && bands.Length > 0)
            {
                var labels = new string[bands.Length];
                for (int i = 0; i < bands.Length; i++)
                    labels[i] = $"{NameOf(bands[i])} [{bands[i].StartY:0}..{bands[i].EndY:0}]";

                _selectedBandIndex = Mathf.Clamp(_selectedBandIndex, 0, bands.Length - 1);
                _selectedBandIndex = EditorGUILayout.Popup(_selectedBandIndex, labels, GUILayout.Width(260));
            }
            else
            {
                GUILayout.Label("(нет полос)", GUILayout.Width(260));
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Время суток", GUILayout.Width(90));
            _timeOfDay01 = EditorGUILayout.Slider(_timeOfDay01, 0f, 1f);
            EditorGUILayout.EndHorizontal();
            GUILayout.Label($"0 полночь · 0.25 восход · 0.5 полдень · 0.75 закат   ({FormatClock(_timeOfDay01)})", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Высота Y", GUILayout.Width(90));
            _worldY = EditorGUILayout.FloatField(_worldY);
            EditorGUILayout.EndHorizontal();
            GUILayout.Label(DescribeActiveBand(bands, _worldY), EditorStyles.miniLabel);

            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();

            WeatherRig[] rigs = FindRigs();
            bool canPreview = rigs.Length > 0;
            using (new EditorGUI.DisabledScope(!canPreview))
            {
                bool newPreview = EditorGUILayout.ToggleLeft("Preview", _previewEnabled, GUILayout.Width(70));
                if (newPreview != _previewEnabled)
                    SetPreviewEnabled(newPreview);
            }

            if (!canPreview)
            {
                GUILayout.Label("нет WeatherRig в загруженных сценах", EditorStyles.miniLabel);
            }
            else if (rigs.Length > 1)
            {
                var rigLabels = new string[rigs.Length];
                for (int i = 0; i < rigs.Length; i++)
                    rigLabels[i] = $"{rigs[i].name} ({rigs[i].gameObject.scene.name})";

                _selectedRigIndex = Mathf.Clamp(_selectedRigIndex, 0, rigs.Length - 1);
                int newRigIndex = EditorGUILayout.Popup(_selectedRigIndex, rigLabels, GUILayout.Width(240));
                if (newRigIndex != _selectedRigIndex)
                {
                    bool wasRunning = _previewEnabled;
                    if (wasRunning)
                        SetPreviewEnabled(false);

                    _selectedRigIndex = newRigIndex;

                    if (wasRunning)
                        SetPreviewEnabled(true);
                }
            }
            else if (_previewEnabled)
            {
                GUILayout.Label($"риг: {rigs[0].name} ({rigs[0].gameObject.scene.name})", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndHorizontal();
        }

        private SkyBandProfile GetSelectedProfile()
        {
            SkyBand[] bands = _staticData.Bands;
            if (bands == null || bands.Length == 0)
                return null;

            _selectedBandIndex = Mathf.Clamp(_selectedBandIndex, 0, bands.Length - 1);
            return bands[_selectedBandIndex].Profile;
        }

        private void BindProfile(SkyBandProfile profile)
        {
            if (_profileSO == null || _lastBoundProfile != profile)
            {
                _profileSO = new SerializedObject(profile);
                _lastBoundProfile = profile;
            }

            _profileSO.Update();
        }

        private void DrawFields(params string[] fieldNames)
        {
            foreach (string name in fieldNames)
            {
                SerializedProperty prop = _profileSO.FindProperty(name);
                if (prop != null)
                    EditorGUILayout.PropertyField(prop, true);
            }
        }

        private void DrawLightingTab()
        {
            EditorGUILayout.LabelField("Skydome", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Что видно на небе.", MessageType.None);
            DrawFields("SkyZenithColor", "SkyHorizonColor", "GradientExponent");

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Ambient", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Чем освещается сцена.", MessageType.None);
            DrawFields("AmbientSkyColor", "AmbientEquatorColor", "AmbientGroundColor", "AmbientMultiplier");

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Sun", EditorStyles.boldLabel);
            DrawFields("SunIntensity", "SunColor");
            EditorGUILayout.HelpBox("SunColor красит и Sun Light, и диск/гало солнца в шейдере — общая ручка.", MessageType.None);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Moon", EditorStyles.boldLabel);
            DrawFields("MoonIntensity", "MoonColor");
            EditorGUILayout.HelpBox("MoonColor красит и Moon Light, и диск/гало луны в шейдере — общая ручка.", MessageType.None);
        }

        private void DrawFogTab()
        {
            EditorGUILayout.LabelField("Horizon fog", EditorStyles.boldLabel);
            DrawFields("SkyFogColor", "SkyFogAmount", "SkyFogHeight", "SkyFogGlowSquish");

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Weather filter", EditorStyles.boldLabel);
            DrawFields("FilterColor", "FilterSaturation", "FilterValue");
        }

        private void DrawCloudsTab()
        {
            // Разделено по тому же принципу, что у секций шейдера: свет/цвет отдельно
            // от формы шума. Раньше 16 полей Cumulus шли одним нечитаемым списком.
            EditorGUILayout.LabelField("Cumulus — color & light", EditorStyles.boldLabel);
            DrawFields("CloudColor", "CloudShadowColor", "CloudHighlightColor", "CloudHighlightFalloff",
                "ShadowSampleDistance", "ShadowDensity", "CloudThickness", "BorderEffect", "BorderHeight");

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Cumulus — shape & generation", EditorStyles.boldLabel);
            DrawFields("CloudCoverage", "CloudScale", "CloudSoftness", "WindSpeed", "CloudRollBias",
                "CloudDetailScale", "CloudDetailAmount");

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Storm", EditorStyles.boldLabel);
            DrawFields("StormColor", "StormShadowColor", "StormScale", "StormThreshold",
                "StormDirection", "StormFrontFalloff");

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Cirrus", EditorStyles.boldLabel);
            DrawFields("CirrusColor", "CirrusCoverage", "CirrusOpacity", "CirrusScale", "CirrusSpeed");
        }

        private void DrawCelestialsTab()
        {
            EditorGUILayout.LabelField("Sun disk / halo", EditorStyles.boldLabel);
            DrawFields("SunSize", "SunHaloColor", "SunHaloFalloff", "SunHaloIntensity");

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Moon flare", EditorStyles.boldLabel);
            DrawFields("MoonFlareFalloff", "MoonFlareIntensity");

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Night", EditorStyles.boldLabel);
            DrawFields("StarDensity", "NightTint");
        }

        private void DrawBandsTab()
        {
            _staticDataSO.Update();
            SerializedProperty bandsProp = _staticDataSO.FindProperty("Bands");

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("StartY", EditorStyles.boldLabel, GUILayout.Width(70));
            GUILayout.Label("EndY", EditorStyles.boldLabel, GUILayout.Width(70));
            GUILayout.Label("BlendUpwards", EditorStyles.boldLabel, GUILayout.Width(90));
            GUILayout.Label("Profile", EditorStyles.boldLabel, GUILayout.Width(220));
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < bandsProp.arraySize; i++)
            {
                SerializedProperty element = bandsProp.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(element.FindPropertyRelative("StartY"), GUIContent.none, GUILayout.Width(70));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("EndY"), GUIContent.none, GUILayout.Width(70));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("BlendUpwards"), GUIContent.none, GUILayout.Width(90));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("Profile"), GUIContent.none, GUILayout.Width(220));
                EditorGUILayout.EndHorizontal();
            }

            if (_staticDataSO.ApplyModifiedProperties())
                AssetDatabase.SaveAssets();

            EditorGUILayout.Space(6);
            if (GUILayout.Button("Validate", GUILayout.Width(120)))
                ValidateBands(_staticData.Bands);
        }

        // Read-only проверка контракта из SkyBand.cs. SkyBandBlender.Evaluate никогда
        // не читает StartY (только EndY/BlendUpwards) — StartY чисто описательный
        // и может молча разойтись с фактическим поведением блендера. Не чинит, только
        // показывает расхождение — тот же house style, что у ProjectScenesWindow.ValidateConfiguration.
        private static void ValidateBands(SkyBand[] bands)
        {
            if (bands == null || bands.Length == 0)
            {
                EditorUtility.DisplayDialog("Validate Bands", "Bands пуст.", "OK");
                return;
            }

            var checks = new List<(string Label, bool Passed)>();

            bool sorted = true;
            for (int i = 1; i < bands.Length; i++)
                if (bands[i].StartY < bands[i - 1].StartY)
                    sorted = false;
            checks.Add(("Массив отсортирован по возрастанию StartY", sorted));

            bool endsAboveStarts = true;
            foreach (SkyBand b in bands)
                if (b.EndY < b.StartY)
                    endsAboveStarts = false;
            checks.Add(("EndY >= StartY у каждой полосы", endsAboveStarts));

            bool allProfiles = true;
            foreach (SkyBand b in bands)
                if (b.Profile == null)
                    allProfiles = false;
            checks.Add(("У каждой полосы назначен Profile", allProfiles));

            bool contract = true;
            for (int i = 1; i < bands.Length; i++)
            {
                float expected = bands[i - 1].EndY + bands[i - 1].BlendUpwards;
                if (!Mathf.Approximately(bands[i].StartY, expected))
                    contract = false;
            }
            checks.Add(("StartY[i] == EndY[i-1] + BlendUpwards[i-1] (SkyBandBlender это не проверяет — только диагностика)", contract));

            foreach ((string label, bool passed) in checks)
            {
                if (!passed)
                    Debug.LogWarning($"[WeatherControlPanel] Validate Bands: {label}");
            }

            string report = string.Join("\n", checks.ConvertAll(c => (c.Passed ? "✓ " : "✗ ") + c.Label));
            EditorUtility.DisplayDialog("Validate Bands", report, "OK");
        }

        // Мирроит ветвление SkyBandBlender.Evaluate (см. Assets/Scripts/WeatherSystem/Profiles/SkyBandBlender.cs)
        // только ради диагностической строки — не пересчитывает SkyState, поэтому не может
        // разойтись с реальным блендом по цвету/значениям, только по формулировке "какая полоса".
        private static string DescribeActiveBand(SkyBand[] bands, float worldY)
        {
            if (bands == null || bands.Length == 0)
                return "нет полос";
            if (bands.Length == 1)
                return $"{NameOf(bands[0])} (единственная)";
            if (worldY <= bands[0].EndY)
                return $"{NameOf(bands[0])} (плато)";

            for (int i = 0; i < bands.Length - 1; i++)
            {
                SkyBand current = bands[i];
                SkyBand next = bands[i + 1];
                float blendEnd = current.EndY + current.BlendUpwards;

                if (worldY <= blendEnd)
                {
                    if (current.BlendUpwards <= 0f)
                        return $"{NameOf(current)} → {NameOf(next)} (мгновенный переход)";

                    float t = Mathf.InverseLerp(current.EndY, blendEnd, worldY);
                    return $"{NameOf(current)} → {NameOf(next)}, {t:P0}";
                }

                if (worldY <= next.EndY)
                    return $"{NameOf(next)} (плато)";
            }

            return $"{NameOf(bands[bands.Length - 1])} (плато, верх)";
        }

        private static string NameOf(SkyBand band) =>
            band.Profile != null ? band.Profile.name : "(no profile)";

        // t=0 полночь — та же шкала, что WeatherTime.PivotEuler. Только для читаемости
        // подписи под слайдером, на сам блендинг не влияет.
        private static string FormatClock(float timeOfDay01)
        {
            float totalMinutes = Mathf.Repeat(timeOfDay01, 1f) * 24f * 60f;
            int hours = (int)(totalMinutes / 60f);
            int minutes = (int)(totalMinutes % 60f);
            return $"{hours:00}:{minutes:00}";
        }
    }
}
