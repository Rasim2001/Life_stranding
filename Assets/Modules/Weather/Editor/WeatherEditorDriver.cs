using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WeatherSystem;
using WeatherSystem.Profiles;

namespace SpiderRig.Editor.Weather
{
    // Погода в редакторе включена всегда — режима превью больше нет. Открыл сцену, и небо,
    // свет и туман сразу такие, какими их увидит игрок. Так устроен Cozy, и ровно поэтому там
    // ничего не «сбрасывается»: жалоба пользователя («закрываю окно — всё сбрасывается») была
    // не про потерю данных, а про то, что вне превью погоду не вычислял никто.
    //
    // Драйвер статический и не привязан к окну намеренно. Раньше подписка на EditorApplication.update
    // жила на WeatherControlPanel, поэтому закрытие окна убивало картинку. Теперь окно — только
    // редактор данных, а рисует всегда этот класс.
    //
    // Источник правды — риг сцены: пресет берётся из WeatherRig.GlobalPreset, время суток из
    // его же сериализованного поля. Ничего из этого не живёт в окне, поэтому переживает и
    // закрытие панели, и перезапуск редактора.
    [InitializeOnLoad]
    public static class WeatherEditorDriver
    {
        private static readonly WeatherPreviewDriver Driver = new WeatherPreviewDriver();

        private static WeatherRig _boundRig;
        private static SkyState _lastSky;
        private static float _lastTimeOfDay01 = float.NaN;
        private static bool _cacheInvalid = true;

        // Канал от панели: по умолчанию высота берётся от камеры Scene view, но художнику нужно
        // уметь посмотреть дальнюю полосу не улетая туда (спек, решение 14).
        public static bool HeightOverrideEnabled { get; set; }
        public static float HeightOverride { get; set; }

        // Пока окно погоды открыто, сцена рисует тот пресет, который в нём правят, а не тот,
        // что назначен на риг. Иначе можно вслепую настраивать пресет, нигде не назначенный.
        // Окно снимает оверрайд при закрытии, и сцена возвращается к пресету рига. Сам риг
        // при этом не трогается — сцена не грязнится.
        public static WeatherPreset PreviewPresetOverride { get; set; }

        static WeatherEditorDriver()
        {
            EditorApplication.update += OnUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneSaving += OnSceneSaving;
            EditorSceneManager.sceneSaved += OnSceneSaved;
        }

        // Новая сцена загружена — привязку снимаем БЕЗ восстановления. Снимок относится
        // к прежней сцене, а RenderSettings.ambient* глобальны: «восстановив» им, мы записали
        // бы чужие значения в свежезагруженную сцену, и ближайший Start() снял бы снимок уже
        // с этой порчи. Измерено: ровно так авторский ambient подменялся ведомым.
        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            Driver.Abandon();
            ResetCache();
            _boundRig = null;
        }

        public static WeatherRig BoundRig => _boundRig;

        // Панель зовёт это после правки пресета: значения в ассете поменялись, а входы драйвера
        // (риг, время, высота) — нет, и без принудительного сброса кэша картинка не обновилась бы.
        public static void Invalidate() => _cacheInvalid = true;

        private static void OnUpdate()
        {
            // В Play Mode рисует WeatherService. Двое пишущих в один купол — это мерцание,
            // поэтому редакторный драйвер уходит с дороги заранее, ещё на переходе.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Unbind();
                return;
            }

            WeatherRig rig = FindRig();
            if (rig == null)
            {
                Unbind();
                return;
            }

            if (_boundRig != rig)
            {
                Unbind();
                Driver.Start(rig);
                _boundRig = rig;
            }

            float worldY = ResolveWorldY(rig);
            float timeOfDay01 = rig.StartingTimeOfDay01;

            // Открытое окно погоды показывает правимый пресет плоско — минуя и полосы,
            // и зоны: иначе автор правит пресет, а видит его вперемешку со случайной зоной
            // под камерой. Закрыто — сцена собирается обеими осями, как в игре.
            SkyState sky = PreviewPresetOverride != null
                ? SkyBandBlender.EvaluateSingle(PreviewPresetOverride, timeOfDay01)
                : WeatherComposer.Compose(rig.Bands, worldY, timeOfDay01, ObserverPosition(), EditorDeltaTime());

            // Сравниваем итоговое состояние, а не входы. Входов мало не бывает: правка границ
            // полос или значений внутри пресета меняет картинку, не трогая ни высоту, ни время,
            // и на сравнении входов такая правка осталась бы невидимой до следующего Invalidate.
            //
            // Смысл сравнения — не экономия на записи свойств (она дешёвая), а SceneView.RepaintAll():
            // без него редактор крутился бы на полной скорости без всякой причины.
            if (!_cacheInvalid && SameSky(sky, _lastSky) && Mathf.Approximately(timeOfDay01, _lastTimeOfDay01))
                return;

            Driver.Apply(sky, timeOfDay01);

            _lastSky = sky;
            _lastTimeOfDay01 = timeOfDay01;
            _cacheInvalid = false;

            SceneView.RepaintAll();
        }

        // Представительная выборка, а не все ~40 полей: этих пяти достаточно, чтобы поймать
        // любую осмысленную правку неба, облаков, света или тумана.
        private static bool SameSky(SkyState a, SkyState b) =>
            a.ZenithColor == b.ZenithColor
            && a.HorizonColor == b.HorizonColor
            && a.CloudColor == b.CloudColor
            && Mathf.Approximately(a.FogVisibilityDistance, b.FogVisibilityDistance)
            && Mathf.Approximately(a.SunIntensity, b.SunIntensity)
            && Mathf.Approximately(a.CloudCoverage, b.CloudCoverage);

        // Высота от камеры Scene view — то же правило, что в рантайме после тикета 01: погоду
        // видит камера. Пока ни одного Scene view не открыто, падаем на низ мира — начало
        // первой полосы, тем же способом, что WeatherService до появления камеры.
        private static float ResolveWorldY(WeatherRig rig)
        {
            if (HeightOverrideEnabled)
                return HeightOverride;

            SceneView view = SceneView.lastActiveSceneView;
            if (view != null && view.camera != null)
                return view.camera.transform.position.y;

            return rig.Bands != null && rig.Bands.Length > 0 ? rig.Bands[0].StartY : 0f;
        }

        // Наблюдатель для зон — та же камера Scene view, что даёт высоту. Ручной оверрайд
        // высоты её не подменяет: он про «покажи полосу на отметке N», а не про «перенеси
        // наблюдателя», и зоны от него ехать не должны.
        private static Vector3? ObserverPosition()
        {
            SceneView view = SceneView.lastActiveSceneView;
            return view != null && view.camera != null
                ? view.camera.transform.position
                : (Vector3?)null;
        }

        // В Edit Mode Time.deltaTime не существует, а временнóй режим зоны без дельты стоял бы
        // намертво. Считаем сами по часам редактора.
        private static double _lastEditorTime;

        private static float EditorDeltaTime()
        {
            double now = EditorApplication.timeSinceStartup;
            double delta = now - _lastEditorTime;
            _lastEditorTime = now;

            // Первый вызов и возврат из Play Mode дают огромную дельту — она бы мгновенно
            // догнала вес зоны до предела и съела весь переход.
            return delta > 0d && delta < 0.5d ? (float)delta : 0f;
        }

        private static WeatherRig FindRig()
        {
            WeatherRig[] rigs = Object.FindObjectsByType<WeatherRig>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            return rigs.Length > 0 ? rigs[0] : null;
        }

        private static void Unbind()
        {
            if (_boundRig == null && !Driver.IsRunning)
                return;

            Driver.Stop();
            _boundRig = null;
            ResetCache();
        }

        private static void ResetCache()
        {
            _lastSky = default;
            _lastTimeOfDay01 = float.NaN;
            _cacheInvalid = true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            // На выходе из Play Mode домен перезагружается и статика обнуляется сама, но
            // на входе — нет: без явного отката авторские значения света остались бы
            // перезаписанными погодой на момент входа.
            if (change == PlayModeStateChange.ExitingEditMode)
                Unbind();
        }

        private static void OnSceneSaving(Scene scene, string path) => Driver.RestoreSceneValues();

        // После записи файла картинку возвращаем: кэш входов сброшен, значит ближайший
        // OnUpdate применит погоду заново. Мигания не будет — между сохранением и следующим
        // апдейтом кадр не рисуется.
        private static void OnSceneSaved(Scene scene) => Invalidate();
    }
}
