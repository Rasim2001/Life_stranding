using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpiderRig.Editor.Shaders
{
    // Валидатор импорта текстур ENV_Lit. Едет тем же куском, что и остальной инспектор —
    // см. спек .scratch/env-lit-shader/spec.md, "Валидатор". Детерминированное (sRGB, тип
    // Normal Map, формат сжатия, Wrap Mode) — с кнопкой «Исправить». DirectX/OpenGL и
    // roughness/smoothness автоматического вердикта не имеют и здесь не проверяются вовсе:
    // конвенция канала гладкости — ответственность художника за файл (тикет 07, грилл
    // 15.09.2026), решение принимается по объекту глазами, а не по цифре в инспекторе.
    internal static class ENV_LitTextureValidator
    {
        // "Standalone" — реальный оверрайд платформы (та же вкладка, что «PC, Mac & Linux
        // Standalone» в инспекторе текстуры), не общая вкладка "Default". Заведено 12.09.2026:
        // форс явного блочного формата (BC4/BC5/BC7) через "Default" писал в консоль
        // "Selected texture format ... is not valid with the current texture type 'Default'"
        // и портил превью текстуры (BC4 виден красным — читается как RGB(r,0,0), это
        // ожидаемо для одноканального формата, но сама ошибка в консоли — нет).
        // Подтверждено: TextureImporter.IsPlatformTextureFormatValid(Default, StandaloneWindows64, BC4)
        // = true, тогда как для generic-вкладки "Default" тот же формат отклоняется.
        private const string TargetPlatform = "Standalone";

        // Рисует строку валидации под слотом текстуры. albedoChannel: true для альбедо
        // (sRGB должен быть включён), false для линейных карт (маска, нормаль, раздельные
        // карты — sRGB должен быть выключен). isNormalMap — дополнительно проверяет
        // textureType == NormalMap.
        //
        // expectedFormat необязателен: у масок и нормали блочный формат однозначен (BC4/BC5/BC7),
        // а у альбедо — нет. BC1 без альфы дешевле, BC7 нужен при cutout, и выбор зависит
        // от материала, а не от слота. Поэтому альбедо проверяется только по sRGB (null),
        // а вердикта по формату, которого у нас нет, валидатор не выдаёт.
        public static void DrawTextureCheck(Texture texture, bool expectSRGB, bool isNormalMap, TextureImporterFormat? expectedFormat)
        {
            if (texture == null) return;

            string path = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(path)) return;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            bool srgbWrong = importer.sRGBTexture != expectSRGB;
            bool typeWrong = isNormalMap && importer.textureType != TextureImporterType.NormalMap;

            // GetAutomaticFormat игнорирует ручной оверрайд платформы и всегда считает,
            // что выбрал бы автоматический режим — поэтому при overridden читаем формат
            // из самих настроек платформы, иначе проверка никогда не увидит уже исправленное.
            bool formatWrong = false;
            if (expectedFormat.HasValue)
            {
                TextureImporterPlatformSettings platformSettings = importer.GetPlatformTextureSettings(TargetPlatform);
                TextureImporterFormat actualFormat = platformSettings.overridden
                    ? platformSettings.format
                    : importer.GetAutomaticFormat(TargetPlatform);
                formatWrong = actualFormat != expectedFormat.Value;
            }

            if (!srgbWrong && !typeWrong && !formatWrong) return;

            EditorGUILayout.BeginHorizontal();
            string message = BuildMessage(srgbWrong, expectSRGB, typeWrong, formatWrong, expectedFormat);
            EditorGUILayout.HelpBox(message, MessageType.Warning);
            if (GUILayout.Button("Исправить", GUILayout.Width(80), GUILayout.Height(38)))
            {
                if (srgbWrong) importer.sRGBTexture = expectSRGB;
                if (typeWrong) importer.textureType = TextureImporterType.NormalMap;
                if (formatWrong && expectedFormat.HasValue)
                {
                    // Автоматический выбор Unity не гарантирует конкретный блочный формат
                    // (BC5 для нормали он сам не предложит) — форсируем явным оверрайдом
                    // платформы Standalone, как это делает ручной выбор формата в инспекторе
                    // на вкладке "PC, Mac & Linux Standalone". Вкладка "Default" такой формат
                    // не принимает — см. комментарий у TargetPlatform.
                    TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(TargetPlatform);
                    settings.overridden = true;
                    settings.format = expectedFormat.Value;
                    importer.SetPlatformTextureSettings(settings);
                }
                importer.SaveAndReimport();
            }
            EditorGUILayout.EndHorizontal();
        }

        private static string BuildMessage(bool srgbWrong, bool expectSRGB, bool typeWrong, bool formatWrong, TextureImporterFormat? expectedFormat)
        {
            if (srgbWrong) return expectSRGB
                ? "sRGB выключен — альбедо должно читаться как цвет."
                : "sRGB включён — это линейные данные (маска/нормаль), не цвет.";
            if (typeWrong) return "Texture Type не Normal Map.";
            if (formatWrong) return $"Формат сжатия не {expectedFormat.Value} — потеря качества.";
            return string.Empty;
        }

        // Wrap Mode карты шума (тикет 06): трипланарная проекция сэмплирует до трёх плоскостей
        // у краёв объекта, и Clamp там растягивает крайний тексель в полосу — артефакт,
        // который на глаз читается как сломанная проекция, а не как настройка импорта.
        // С кнопкой «Исправить», как и у остальных проверок этого файла.
        public static void DrawWrapModeCheck(Texture texture, TextureWrapMode expected, string message)
        {
            if (texture == null) return;

            string path = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(path)) return;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            if (importer.wrapMode == expected) return;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox(message, MessageType.Warning);
            if (GUILayout.Button("Исправить", GUILayout.Width(80), GUILayout.Height(38)))
            {
                importer.wrapMode = expected;
                importer.SaveAndReimport();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
