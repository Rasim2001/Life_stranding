using System.Text;
using UnityEditor;
using UnityEngine;
using WeatherSystem.Profiles;

namespace SpiderRig.Editor.Weather
{
    // Одноразовая миграция перед удалением SkyFogColor (тикет 02, .scratch/
    // fog-generation-and-layers/spec.md, решения #13, #17). Поле удаляется — купол
    // берёт цвет дымки из FogFarColor вместо собственного. На DATA_Weather_Preset_Ground
    // лежит единственное авторское значение SkyFogColor в проекте: суточный 7-ключевой
    // градиент (ночной синий → бирюза → дневной голубой → закатный красный). На
    // остальных трёх полосах поле в режиме Constant/белый — переносить нечего.
    //
    // Копируются только ЦВЕТОВЫЕ ключи. Альфа-ключи исходного градиента (0→1 по суткам)
    // не переносятся: в SkyFogColor альфа не читалась шейдером вообще (купол брал только
    // .rgb), а в FogFarColor альфа — маска плотности дальностного тумана. Перенос альфы
    // означал бы незапрошенное включение/выключение тумана по времени суток. Целевая
    // альфа остаётся как есть на момент миграции (см. отчёт).
    public static class WeatherSkyFogColorMigration
    {
        private const string GroundProfileName = "DATA_Weather_Preset_Ground";

        [MenuItem("GD Tools/Weather/Migrate SkyFogColor to FogFarColor (one-shot)")]
        private static void Run()
        {
            if (!EditorUtility.DisplayDialog("Migrate SkyFogColor → FogFarColor",
                    $"Копирует ЦВЕТОВЫЕ ключи градиента SkyFogColor на {GroundProfileName} " +
                    "в FogFarColor (переключает FogFarColor в режим Gradient). Альфа-ключи " +
                    "не переносятся — это разные величины (прозрачность цвета vs плотность " +
                    "тумана). Остальные профили не тронуты — там поле не авторено.\n\n" +
                    "Запускать один раз, до удаления поля SkyFogColor из кода. Продолжить?",
                    "Мигрировать", "Отмена"))
                return;

            string result = RunCore();
            EditorUtility.DisplayDialog("Migrate SkyFogColor → FogFarColor", result, "OK");
        }

        // Без диалога — для вызова из execute_code.
        public static string RunCore()
        {
            var report = new StringBuilder();

            foreach (string guid in AssetDatabase.FindAssets($"t:WeatherPreset {GroundProfileName}"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<WeatherPreset>(path);
                if (profile == null || profile.name != GroundProfileName)
                    continue;

                MigrateProfile(profile, report);
            }

            AssetDatabase.SaveAssets();

            string full = report.Length > 0 ? report.ToString() : $"{GroundProfileName} не найден.";
            Debug.Log("[WeatherSkyFogColorMigration]\n" + full);
            return full;
        }

        private static void MigrateProfile(WeatherPreset profile, StringBuilder report)
        {
            var so = new SerializedObject(profile);
            SerializedProperty source = so.FindProperty("SkyFogColor");
            SerializedProperty target = so.FindProperty("FogFarColor");
            if (source == null || target == null)
            {
                report.AppendLine($"{profile.name}: поле SkyFogColor или FogFarColor не найдено — пропущено.");
                return;
            }

            SerializedProperty sourceMode = source.FindPropertyRelative("_mode");
            if (sourceMode.enumValueIndex != 1) // DailyColor.Mode.Gradient
            {
                report.AppendLine($"{profile.name}: SkyFogColor в режиме Constant, переносить нечего — пропущено.");
                return;
            }

            Gradient sourceGradient = source.FindPropertyRelative("_gradient").gradientValue;
            SerializedProperty targetGradientProp = target.FindPropertyRelative("_gradient");
            Gradient targetGradient = targetGradientProp.gradientValue;

            // Переносим только цвет: берём ЦВЕТОВЫЕ ключи источника, АЛЬФА-ключи —
            // оставляем целевые как есть (см. комментарий класса).
            var mixed = new Gradient { mode = sourceGradient.mode };
            mixed.SetKeys(sourceGradient.colorKeys, targetGradient.alphaKeys);
            targetGradientProp.gradientValue = mixed;

            SerializedProperty targetMode = target.FindPropertyRelative("_mode");
            targetMode.enumValueIndex = 1; // DailyColor.Mode.Gradient

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(profile);

            report.AppendLine($"{profile.name}.FogFarColor: {sourceGradient.colorKeys.Length} цветовых " +
                $"ключей перенесено из SkyFogColor; альфа-ключи ({targetGradient.alphaKeys.Length} шт.) " +
                "оставлены как были; режим переключён в Gradient.");
        }
    }
}
