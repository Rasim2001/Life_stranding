using System.Text;
using UnityEditor;
using UnityEngine;
using WeatherSystem.Profiles;

namespace SpiderRig.Editor.Weather
{
    // Одноразовая миграция: SunShadowStrength/MoonShadowStrength — новые поля профиля,
    // дефолт DailyFloat._constant = 0 (Unity не гарантирует прогон C#-инициализатора при
    // десериализации уже существующего .asset, тот же случай, что и у AmbientMultiplier
    // в WeatherTimeMigration.cs и у FogVisibilityDistance в WeatherFogDefaultsMigration.cs).
    // Без миграции сила тени солнца и луны молча гаснет на всех существующих пресетах —
    // регрессия хуже той, что чинил срез 1 плана celestial-handover-and-arc-controls.
    //
    // 1f — нейтральный дефолт: совпадает с прежним авторским значением на риге
    // (WeatherRig._sunShadowStrength/_moonShadowStrength были Range(0,1) с дефолтом 1),
    // визуально ничего не меняется.
    public static class WeatherShadowStrengthDefaultsMigration
    {
        private const float DefaultShadowStrength = 1f;

        [MenuItem("GD Tools/Weather/Migrate Shadow Strength Defaults (one-shot)")]
        private static void Run()
        {
            if (!EditorUtility.DisplayDialog("Migrate Shadow Strength Defaults",
                    $"Проставляет SunShadowStrength/MoonShadowStrength={DefaultShadowStrength:0} на профилях " +
                    "WeatherPreset, где поле ещё нулевое (новые поля, дефолт без миграции — тень солнца и " +
                    "луны молча гаснет). Не трогает профили, где поле уже настроено.\n\n" +
                    "Продолжить?",
                    "Мигрировать", "Отмена"))
                return;

            string result = RunCore();
            EditorUtility.DisplayDialog("Migrate Shadow Strength Defaults", result, "OK");
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
            Debug.Log("[WeatherShadowStrengthDefaultsMigration]\n" + full);
            return full;
        }

        private static bool MigrateProfile(WeatherPreset profile, StringBuilder report)
        {
            var so = new SerializedObject(profile);
            bool changed = false;

            changed |= MigrateField(so, "SunShadowStrength", profile.name, report);
            changed |= MigrateField(so, "MoonShadowStrength", profile.name, report);

            if (!changed)
                return false;

            EditorUtility.SetDirty(profile);
            return true;
        }

        private static bool MigrateField(SerializedObject so, string fieldName, string profileName, StringBuilder report)
        {
            SerializedProperty field = so.FindProperty(fieldName);
            if (field == null)
                return false;

            SerializedProperty mode = field.FindPropertyRelative("_mode");
            SerializedProperty constant = field.FindPropertyRelative("_constant");
            if (mode == null || constant == null || mode.enumValueIndex != 0 || constant.floatValue != 0f)
                return false;

            constant.floatValue = DefaultShadowStrength;
            so.ApplyModifiedProperties();

            report.AppendLine($"{profileName}.{fieldName}: 0 → {DefaultShadowStrength:0} " +
                "(новое поле, нейтральный дефолт)");
            return true;
        }
    }
}
