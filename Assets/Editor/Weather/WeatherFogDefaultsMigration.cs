using System.Text;
using UnityEditor;
using UnityEngine;
using WeatherSystem.Profiles;

namespace SpiderRig.Editor.Weather
{
    // Одноразовая миграция: FogVisibilityDistance — новое поле профиля, дефолт
    // DailyFloat._constant = 0 (Unity не гарантирует прогон C#-инициализатора при
    // десериализации уже существующего .asset, тот же случай, что и у AmbientMultiplier
    // в WeatherTimeMigration.cs). σ = 3/0 = бесконечность — сплошной туман на весь мир
    // в момент регистрации fog-пасса, поэтому это не косметика, а регрессия по умолчанию.
    //
    // Отдельный пункт меню, не расширение WeatherTimeMigration: тот скрипт сдвигает
    // суточные градиенты на +0.25 безусловно и не идемпотентен — повторный запуск
    // сдвинул бы уже сдвинутые ключи ещё раз. Смешивать с ним новую миграцию опасно.
    public static class WeatherFogDefaultsMigration
    {
        private const float DefaultVisibilityMeters = 600f; // спек: референс "ясный день".

        [MenuItem("GD Tools/Weather/Migrate Fog Defaults (one-shot)")]
        private static void Run()
        {
            if (!EditorUtility.DisplayDialog("Migrate Fog Defaults",
                    $"Проставляет FogVisibilityDistance={DefaultVisibilityMeters:0} м на профилях " +
                    "WeatherPreset, где поле ещё нулевое (новое поле, дефолт тумана без миграции — " +
                    "сплошная пелена на весь мир). Не трогает профили, где поле уже настроено.\n\n" +
                    "Продолжить?",
                    "Мигрировать", "Отмена"))
                return;

            string result = RunCore();
            EditorUtility.DisplayDialog("Migrate Fog Defaults", result, "OK");
        }

        // Без диалога — для вызова из execute_code.
        public static string RunCore()
        {
            var report = new StringBuilder();
            int migrated = 0;

            foreach (string guid in AssetDatabase.FindAssets("t:WeatherPreset"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<WeatherPreset>(path);
                if (MigrateProfile(profile, report))
                    migrated++;
            }

            AssetDatabase.SaveAssets();

            string header = $"Мигрировано профилей: {migrated}\n\n";
            string full = header + report;
            Debug.Log("[WeatherFogDefaultsMigration]\n" + full);
            return full;
        }

        private static bool MigrateProfile(WeatherPreset profile, StringBuilder report)
        {
            var so = new SerializedObject(profile);
            SerializedProperty visibility = so.FindProperty("FogVisibilityDistance");
            if (visibility == null)
                return false;

            SerializedProperty mode = visibility.FindPropertyRelative("_mode");
            SerializedProperty constant = visibility.FindPropertyRelative("_constant");
            if (mode == null || constant == null || mode.enumValueIndex != 0 || constant.floatValue != 0f)
                return false;

            constant.floatValue = DefaultVisibilityMeters;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(profile);

            report.AppendLine($"{profile.name}.FogVisibilityDistance: 0 → {DefaultVisibilityMeters:0} " +
                "(новое поле, дефолт \"ясный день\")");
            return true;
        }
    }
}
