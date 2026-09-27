using UnityEditor;
using UnityEngine;
using WeatherSystem;
using WeatherSystem.Profiles;

namespace SpiderRig.Editor.Weather
{
    // Единая панель управления погодой — вкладки Lighting/Fog/Clouds/Celestials/Bands,
    // как у Cozy Atmosphere Profile, но окно, а не инспектор. Редактируемый пресет берётся
    // с рига загруженной сцены (WeatherRig.GlobalPreset) — панель обязана править ровно то,
    // что в этой сцене рисуется.
    //
    // Правка через SerializedObject/SerializedProperty — отход от конвенции проекта
    // (обычно прямой доступ к полям + Undo.RecordObject + SetDirty, см. ProjectScenesWindow),
    // но обязателен: у DailyColor/DailyFloat поля приватные, прямым доступом не достать.
    // Заодно Undo и dirty-разметка идут автоматически через ApplyModifiedProperties.
    public class WeatherControlPanel : EditorWindow
    {
        private static readonly string[] TabLabels = { "Lighting", "Fog", "Clouds", "Celestials" };

        [SerializeField] private int _tab;
        [SerializeField] private float _worldY;
        [SerializeField] private bool _heightOverride;
        [SerializeField] private int _selectedRigIndex;
        [SerializeField] private bool _celestialFoldout;

        // Пресет берётся с рига выбранной сцены, а не из фиксированного пути к ассету:
        // после тикета 03 источник погоды — поле WeatherRig.GlobalPreset, и панель обязана
        // редактировать ровно то, что рисуется. Список пресетов с выбором — тикет 04.
        [SerializeField] private WeatherPreset _preset;

        private SerializedObject _presetSO;
        private Vector2 _scroll;

        // Резервная копия выбранного пресета в памяти — основа кнопки «Отклонить».
        // Клон помечен DontSave, поэтому не попадает ни в сцену, ни в ассеты.
        //
        // Клонирование, а не JSON: DailyColor держит Gradient в приватном поле, а JsonUtility
        // градиенты не сериализует — снимок вышел бы неполным и молча.
        private WeatherPreset _backupPreset;
        private bool _hasUnsavedChanges;

        [MenuItem("GD Tools/Weather/Control Panel")]
        public static void Open() => GetWindow<WeatherControlPanel>("Weather Control");

        private void OnEnable()
        {
            titleContent = new GUIContent("Weather Control");

            // При первом открытии показываем пресет нижней полосы рига — самый вероятный
            // предмет правки. Дальше выбор живёт в поле окна.
            if (_preset == null)
            {
                WeatherRig rig = ResolveRig();
                if (rig != null && rig.Bands != null && rig.Bands.Length > 0)
                    _preset = rig.Bands[0].Preset;
            }

            SelectPreset(_preset);
        }

        private void OnDisable()
        {
            if (_hasUnsavedChanges)
                PromptUnsavedOnClose();

            WeatherEditorDriver.PreviewPresetOverride = null;
            WeatherEditorDriver.HeightOverrideEnabled = false;
            WeatherEditorDriver.Invalidate();

            DropBackup();
        }

        // Окно закрывают не только кнопкой: перезагрузка домена после компиляции тоже зовёт
        // OnDisable. Диалог здесь честнее молчаливого сохранения — правки могут быть неудачными.
        private void PromptUnsavedOnClose()
        {
            bool save = EditorUtility.DisplayDialog(
                "Weather Control",
                $"В пресете «{(_preset != null ? _preset.name : "?")}» есть несохранённые изменения.",
                "Сохранить", "Отклонить");

            if (save)
                SaveEdits();
            else
                DiscardEdits();
        }

        private void DrawPresetBar()
        {
            WeatherPreset[] presets = FindAllPresets();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Пресет", GUILayout.Width(48));

            if (presets.Length > 0)
            {
                var labels = new string[presets.Length];
                int current = 0;
                for (int i = 0; i < presets.Length; i++)
                {
                    labels[i] = presets[i] != null ? presets[i].name : "(пусто)";
                    if (presets[i] == _preset)
                        current = i;
                }

                int picked = EditorGUILayout.Popup(current, labels, EditorStyles.toolbarPopup, GUILayout.Width(220));
                if (picked != current)
                    SelectPreset(presets[picked]);
            }
            else
            {
                GUILayout.Label("(нет пресетов)", GUILayout.Width(220));
            }

            using (new EditorGUI.DisabledScope(_preset == null))
                if (GUILayout.Button("Дублировать", EditorStyles.toolbarButton, GUILayout.Width(90)))
                    DuplicateSelected();

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(!_hasUnsavedChanges))
            {
                if (GUILayout.Button("Сохранить", EditorStyles.toolbarButton, GUILayout.Width(80)))
                    SaveEdits();

                if (GUILayout.Button("Отклонить", EditorStyles.toolbarButton, GUILayout.Width(80)))
                    DiscardEdits();
            }

            EditorGUILayout.EndHorizontal();

            if (_hasUnsavedChanges)
                EditorGUILayout.HelpBox("Есть несохранённые изменения.", MessageType.Info);
        }

        // === рабочая копия ===

        private void SelectPreset(WeatherPreset preset)
        {
            if (_hasUnsavedChanges && _preset != null && _preset != preset)
                PromptUnsavedOnClose();

            DropBackup();

            _preset = preset;
            _presetSO = null;
            _presetSO = null;
            _hasUnsavedChanges = false;

            if (_preset == null)
                return;

            TakeBackup();
            WeatherEditorDriver.PreviewPresetOverride = _preset;
            WeatherEditorDriver.Invalidate();
        }

        private void TakeBackup()
        {
            _backupPreset = Instantiate(_preset);
            _backupPreset.hideFlags = HideFlags.HideAndDontSave;
        }

        private void DropBackup()
        {
            if (_backupPreset != null)
                DestroyImmediate(_backupPreset);
            _backupPreset = null;
        }

        private void SaveEdits()
        {
            if (_preset == null)
                return;

            EditorUtility.SetDirty(_preset);
            AssetDatabase.SaveAssets();

            DropBackup();
            TakeBackup();
            _hasUnsavedChanges = false;
        }

        private void DiscardEdits()
        {
            if (_preset == null || _backupPreset == null)
                return;

            EditorUtility.CopySerialized(_backupPreset, _preset);
            AssetDatabase.SaveAssets();

            _presetSO = null;
            _hasUnsavedChanges = false;
            WeatherEditorDriver.Invalidate();
        }

        // Штатный CopyAsset: пресет теперь один самостоятельный объект без под-ассетов,
        // копировать нечего кроме него самого. Ctrl+D в Project-окне даёт тот же результат.
        private void DuplicateSelected()
        {
            if (_preset == null)
                return;

            if (_hasUnsavedChanges)
                PromptUnsavedOnClose();

            string sourcePath = AssetDatabase.GetAssetPath(_preset);
            string copyPath = AssetDatabase.GenerateUniqueAssetPath(sourcePath);

            if (!AssetDatabase.CopyAsset(sourcePath, copyPath))
            {
                EditorUtility.DisplayDialog("Weather Control", "Не удалось создать копию пресета.", "OK");
                return;
            }

            AssetDatabase.Refresh();
            SelectPreset(AssetDatabase.LoadAssetAtPath<WeatherPreset>(copyPath));
        }

        private static WeatherPreset[] FindAllPresets()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(WeatherPreset));
            var result = new WeatherPreset[guids.Length];
            for (int i = 0; i < guids.Length; i++)
                result[i] = AssetDatabase.LoadAssetAtPath<WeatherPreset>(AssetDatabase.GUIDToAssetPath(guids[i]));
            return result;
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
            DrawPresetBar();

            if (_preset == null)
            {
                EditorGUILayout.HelpBox(
                    "В проекте нет ни одного пресета погоды. Создай его через "
                    + "Create → StaticData → Weather → Weather Preset и назначь в поле "
                    + "Global Preset на WeatherRig.",
                    MessageType.Warning);
                return;
            }

            // Оверрайд мог слететь при перезагрузке домена — окно открыто, а сцена рисует
            // пресет рига. Восстанавливаем на каждой отрисовке, это дёшево.
            if (WeatherEditorDriver.PreviewPresetOverride != _preset)
            {
                WeatherEditorDriver.PreviewPresetOverride = _preset;
                WeatherEditorDriver.Invalidate();
            }

            if (_presetSO == null || _presetSO.targetObject != _preset)
                _presetSO = new SerializedObject(_preset);

            DrawHeader();
            EditorGUILayout.Space(4);

            _tab = GUILayout.Toolbar(_tab, TabLabels);
            EditorGUILayout.Space(4);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            _presetSO.Update();

            switch (_tab)
            {
                case 0: DrawLightingTab(); break;
                case 1: DrawFogTab(); break;
                case 2: DrawCloudsTab(); break;
                case 3: DrawCelestialsTab(); break;
            }

            // Правка уходит в ассет в памяти, но НЕ на диск: сохранение по явной кнопке.
            // Автосейв на каждое движение слайдера затирал бы рабочий пресет неудачным
            // экспериментом молча — ровно то, от чего избавился тикет 04.
            if (_presetSO.ApplyModifiedProperties())
            {
                _hasUnsavedChanges = true;
                WeatherEditorDriver.Invalidate();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {

            DrawTimeOfDay();

            // Высота по умолчанию приезжает от камеры Scene view — то же правило, что
            // в рантайме. Ручной оверрайд нужен, чтобы посмотреть дальнюю полосу не улетая
            // туда камерой (спек, решение 14).
            EditorGUILayout.BeginHorizontal();
            _heightOverride = EditorGUILayout.ToggleLeft("Высота Y", _heightOverride, GUILayout.Width(90));
            using (new EditorGUI.DisabledScope(!_heightOverride))
                _worldY = EditorGUILayout.FloatField(_worldY);
            EditorGUILayout.EndHorizontal();

            WeatherEditorDriver.HeightOverrideEnabled = _heightOverride;
            WeatherEditorDriver.HeightOverride = _worldY;

            // Высота на превью правимого пресета не влияет — он показывается плоско. Она нужна
            // только чтобы понимать, какая полоса рига звучала бы на этой отметке, когда окно
            // закроют. Полосы живут в инспекторе рига (спек, решение 6).
            WeatherRig rigForBands = ResolveRig();
            SkyBand[] bands = rigForBands != null ? rigForBands.Bands : null;
            float shownY = _heightOverride ? _worldY : CurrentSceneViewY();
            GUILayout.Label(
                _heightOverride
                    ? $"полоса рига на {shownY:0.0}: {DescribeActiveBand(bands, shownY)}"
                    : $"камера Scene view на {shownY:0.0} · полоса рига: {DescribeActiveBand(bands, shownY)}",
                EditorStyles.miniLabel);

            EditorGUILayout.Space(2);

            WeatherRig[] rigs = FindRigs();
            if (rigs.Length == 0)
            {
                GUILayout.Label("нет WeatherRig в загруженных сценах", EditorStyles.miniLabel);
            }
            else if (rigs.Length > 1)
            {
                var rigLabels = new string[rigs.Length];
                for (int i = 0; i < rigs.Length; i++)
                    rigLabels[i] = $"{rigs[i].name} ({rigs[i].gameObject.scene.name})";

                _selectedRigIndex = Mathf.Clamp(_selectedRigIndex, 0, rigs.Length - 1);
                _selectedRigIndex = EditorGUILayout.Popup(_selectedRigIndex, rigLabels, GUILayout.Width(240));
            }
            else
            {
                GUILayout.Label($"риг: {rigs[0].name} ({rigs[0].gameObject.scene.name})", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(2);
            DrawCelestialMechanicsSection();
        }

        // Правит сцену (риг), а не пресет — сознательно не пятая вкладка: вкладки
        // Lighting/Fog/Clouds/Celestials подчиняются кнопкам «Сохранить / Отклонить» пресета,
        // а эта секция их не касается. Механизм — тот же, что в DrawTimeOfDay(): свой
        // SerializedObject(rig), ApplyModifiedProperties() → WeatherEditorDriver.Invalidate(),
        // Undo и dirty-разметка сцены получаются автоматически.
        private void DrawCelestialMechanicsSection()
        {
            WeatherRig rig = ResolveRig();
            if (rig == null)
                return;

            _celestialFoldout = EditorGUILayout.Foldout(_celestialFoldout, "Небесная механика", true);
            if (!_celestialFoldout)
                return;

            EditorGUI.indentLevel++;

            var rigSO = new SerializedObject(rig);
            DrawRigField(rigSO, "_arcAzimuth", "Azimuth");
            DrawRigField(rigSO, "_arcTilt", "Tilt");
            DrawRigField(rigSO, "_moonOffsetDegrees", "Moon Offset");
            DrawRigField(rigSO, "_horizonFadeBand", "Horizon Fade Band");
            DrawRigField(rigSO, "_sunShadowType", "Sun Shadow Type");
            DrawRigField(rigSO, "_moonShadowType", "Moon Shadow Type");

            if (rigSO.ApplyModifiedProperties())
                WeatherEditorDriver.Invalidate();

            // Справочная строка — чтобы последствия ручек читались без отдельных замеров.
            // Восход = 180 + Azimuth, заход = Azimuth (нормализован в 0..360), макс. высота
            // = 90 - Tilt — формулы из спека (.scratch/celestial-handover-and-arc-controls).
            float sunrise = Mathf.Repeat(180f + rig.ArcAzimuth, 360f);
            float sunset = Mathf.Repeat(rig.ArcAzimuth, 360f);
            float maxHeight = 90f - rig.ArcTilt;
            int overlapMinutes = EstimateOverlapGameMinutes(rig);

            GUILayout.Label(
                $"восход {sunrise:0}° · заход {sunset:0}° · макс. высота {maxHeight:0}° · "
                + $"восход луны {sunrise:0}° · перекрытие ≈ {overlapMinutes} игр. мин.",
                EditorStyles.miniLabel);

            // Звёздный купол со светилами не связан (решение зафиксировано в спеке) — звёзды
            // крутятся вокруг мировой вертикали по своей Latitude, светила — по дуге вокруг
            // горизонтальной оси. Модели разные, подсказка — единственное, что их сближает.
            EditorGUILayout.HelpBox(
                "Latitude звёздного купола (вкладка Celestials пресета) со Tilt не связан "
                + "автоматически — для согласованного наклона выставьте его вручную близко к Tilt.",
                MessageType.None);

            EditorGUI.indentLevel--;
        }

        private static void DrawRigField(SerializedObject rigSO, string propertyName, string label)
        {
            SerializedProperty prop = rigSO.FindProperty(propertyName);
            if (prop != null)
                EditorGUILayout.PropertyField(prop, new GUIContent(label));
        }

        // Численная оценка окна, где обе лампы одновременно выше порога включения —
        // та же формула, что в WeatherCelestialApplier, но без записи в реальные трансформы
        // (композиция кватернионов пивота и лампы, см. WeatherService.RotateSunMoonPivot).
        // Приближение: не учитывает возможный поворот самого рига в мире — для справочной
        // строки этого достаточно, замер точных чисел делается через MCP (план, п. 2.7).
        private static int EstimateOverlapGameMinutes(WeatherRig rig)
        {
            Quaternion pivotRotation = Quaternion.Euler(WeatherTime.ArcPivotEuler(rig.ArcAzimuth, rig.ArcTilt));
            float band = rig.HorizonFadeBand;
            const int steps = 1440;
            int count = 0;

            for (int i = 0; i < steps; i++)
            {
                float t = i / (float)steps;
                float sunElevation = Elevation(pivotRotation, t, 0f);
                float moonElevation = Elevation(pivotRotation, t, rig.MoonOffsetDegrees);

                bool sunOn = WeatherTime.HorizonWeight(sunElevation, band) > 0.001f;
                bool moonOn = WeatherTime.HorizonWeight(moonElevation, band) > 0.001f;
                if (sunOn && moonOn)
                    count++;
            }

            return count;
        }

        private static float Elevation(Quaternion pivotRotation, float timeOfDay01, float offsetDegrees)
        {
            Quaternion local = Quaternion.Euler(WeatherTime.CelestialEuler(timeOfDay01, offsetDegrees));
            Vector3 forward = pivotRotation * local * Vector3.forward;
            return -forward.y;
        }

        // Время суток живёт на риге, а не в окне: иначе оно теряется при закрытии панели —
        // ровно то, на что жаловался пользователь. Правка идёт через SerializedObject, поэтому
        // получает Undo и честно метит сцену грязной: это авторские данные, а не производные.
        private void DrawTimeOfDay()
        {
            WeatherRig rig = ResolveRig();
            if (rig == null)
                return;

            var rigSO = new SerializedObject(rig);
            SerializedProperty timeProp = rigSO.FindProperty("_startingTimeOfDay01");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Время суток", GUILayout.Width(90));
            EditorGUILayout.PropertyField(timeProp, GUIContent.none);
            EditorGUILayout.EndHorizontal();

            if (rigSO.ApplyModifiedProperties())
                WeatherEditorDriver.Invalidate();

            GUILayout.Label(
                $"0 полночь · 0.25 восход · 0.5 полдень · 0.75 закат   ({FormatClock(timeProp.floatValue)})",
                EditorStyles.miniLabel);
        }

        // Время суток теперь на риге, поэтому подписи вроде «≈ N м при текущей плотности»
        // читают его оттуда же, что и драйвер. Без рига — полдень как нейтральная точка:
        // подпись справочная, врать ей нечем.
        private float CurrentTimeOfDay01()
        {
            WeatherRig rig = ResolveRig();
            return rig != null ? rig.StartingTimeOfDay01 : 0.5f;
        }

        private static float CurrentSceneViewY()
        {
            SceneView view = SceneView.lastActiveSceneView;
            return view != null && view.camera != null ? view.camera.transform.position.y : 0f;
        }

        private void DrawFields(params string[] fieldNames)
        {
            foreach (string name in fieldNames)
            {
                SerializedProperty prop = _presetSO.FindProperty(name);
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
            DrawFields("AmbientSkyColor", "AmbientEquatorColor", "AmbientGroundReflectance", "AmbientMultiplier");

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Sun", EditorStyles.boldLabel);
            DrawFields("SunIntensity", "SunColor", "SunShadowStrength");
            EditorGUILayout.HelpBox("SunColor красит и Sun Light, и диск/гало солнца в шейдере — общая ручка.", MessageType.None);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Moon", EditorStyles.boldLabel);
            DrawFields("MoonIntensity", "MoonColor", "MoonShadowStrength");
            EditorGUILayout.HelpBox("MoonColor красит и Moon Light, и диск/гало луны в шейдере — общая ручка.", MessageType.None);
        }

        private void DrawFogTab()
        {
            // Цвет дымки больше не своя ручка (SkyFogColor удалён) — дальний стоп ниже
            // (FogFarColor) её и кормит, и кормит подмес в облака (CloudsFogAmount).
            EditorGUILayout.LabelField("Horizon fog", EditorStyles.boldLabel);
            DrawFields("SkyFogAmount", "SkyFogHeight", "SkyFogGlowSquish");

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Distance fog", EditorStyles.boldLabel);
            DrawFields("FogVisibilityDistance");
            DrawFogStopField("FogNearColor", null);
            DrawFogStopField("FogMidColor", "FogMidPosition");
            DrawFogStopField("FogFarColor", "FogFarPosition");
            DrawFields("CloudsFogAmount");

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Weather filter", EditorStyles.boldLabel);
            DrawFields("FilterColor", "FilterSaturation", "FilterValue");
        }

        // Позиция стопа — доля дальности видимости (спек, решение #3), не метры. Подпись
        // "≈ X м" — единственное место, где эта доля переводится в читаемое расстояние;
        // PropertyDrawer этого сделать не может, у него нет доступа к соседнему полю.
        private void DrawFogStopField(string colorField, string positionField)
        {
            DrawFields(colorField);
            if (positionField == null)
                return;

            DrawFields(positionField);

            SerializedProperty visibility = _presetSO.FindProperty("FogVisibilityDistance");
            SerializedProperty position = _presetSO.FindProperty(positionField);
            if (visibility == null || position == null)
                return;

            float meters = position.floatValue * EvaluateDailyFloatConstantOrCurve(visibility, CurrentTimeOfDay01());
            GUILayout.Label($"≈ {meters:0.#} м при текущей плотности", EditorStyles.miniLabel);
        }

        // DailyFloat не читается напрямую как float из SerializedProperty — режим решает,
        // откуда брать значение: _constant или _curve.Evaluate(t). В режиме Curve подпись
        // без этой ветки молча показала бы 0.
        private static float EvaluateDailyFloatConstantOrCurve(SerializedProperty dailyFloatProp, float timeOfDay01)
        {
            SerializedProperty mode = dailyFloatProp.FindPropertyRelative("_mode");
            if (mode == null)
                return 0f;

            if (mode.enumValueIndex == 0)
            {
                SerializedProperty constant = dailyFloatProp.FindPropertyRelative("_constant");
                return constant != null ? constant.floatValue : 0f;
            }

            SerializedProperty curve = dailyFloatProp.FindPropertyRelative("_curve");
            return curve != null ? curve.animationCurveValue.Evaluate(timeOfDay01) : 0f;
        }

        private void DrawCloudsTab()
        {
            // Разделено по тому же принципу, что у секций шейдера: свет/цвет отдельно
            // от формы шума. Раньше 16 полей Cumulus шли одним нечитаемым списком.
            // Общая палитра на все ярусы разом — см. .scratch/cloud-color-architecture/spec.md.
            EditorGUILayout.LabelField("Clouds — shared color & light", EditorStyles.boldLabel);
            DrawFields("CloudColor", "CloudSkyLitColor", "SkyLitSpread", "SkyLitSoftness",
                "CloudShadowColor", "CloudHighlightColor", "CloudHighlightFalloff",
                "CloudMoonColor", "CloudMoonHighlightFalloff",
                "ShadowSampleDistance", "ShadowDensity", "CloudThickness", "BorderEffect", "BorderHeight",
                "CloudBorderColor");

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Cumulus — shape & generation", EditorStyles.boldLabel);
            DrawFields("CloudCoverage", "CloudScale", "CloudSoftness", "WindSpeed", "CloudRollBias",
                "CloudDetailScale", "CloudDetailAmount", "CloudCohesion");

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Storm", EditorStyles.boldLabel);
            DrawFields("StormTint", "StormCoverage", "StormScale", "StormThreshold",
                "StormDirection", "StormFrontFalloff");

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Cirrus", EditorStyles.boldLabel);
            DrawFields("CirrusTint", "CirrusCoverage", "CirrusOpacity", "CirrusScale", "CirrusSpeed");
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
            DrawFields("StarColor", "Latitude");
        }

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
            band.Preset != null ? band.Preset.name : "(пресет не назначен)";

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
