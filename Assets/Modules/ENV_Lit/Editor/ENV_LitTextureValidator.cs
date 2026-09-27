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
        private const float WarningIconSize = 16f;
        private const float FixButtonWidth = 44f;
        private const float ConvertButtonWidth = 60f;
        private static Texture warningIcon;
        private static GUIStyle warningTextStyle;

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

            bool importerChanged = false;

            if (srgbWrong)
            {
                GUIContent message = expectSRGB
                    ? new GUIContent("sRGB must be enabled for color textures.",
                        "sRGB выключен — альбедо должно читаться как цвет.")
                    : new GUIContent("sRGB must be disabled for data textures.",
                        "sRGB включён — это линейные данные (маска/нормаль), не цвет.");
                if (DrawActionWarning(message,
                    new GUIContent("Fix", "Исправить только настройку sRGB этой текстуры."),
                    FixButtonWidth))
                {
                    importer.sRGBTexture = expectSRGB;
                    importerChanged = true;
                }
            }

            if (typeWrong)
            {
                GUIContent message = new GUIContent("Texture Type must be Normal Map.",
                    "Texture Type должен быть установлен в Normal Map.");
                if (DrawActionWarning(message,
                    new GUIContent("Fix", "Исправить только Texture Type этой текстуры."),
                    FixButtonWidth))
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    importerChanged = true;
                }
            }

            if (formatWrong)
            {
                GUIContent message = new GUIContent($"Compression format must be {expectedFormat.Value}.",
                    $"Формат сжатия должен быть {expectedFormat.Value}, иначе возможна потеря качества.");
                if (DrawActionWarning(message,
                    new GUIContent("Convert", "Преобразовать только формат сжатия этой текстуры."),
                    ConvertButtonWidth))
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
                    importerChanged = true;
                }
            }

            if (importerChanged)
                importer.SaveAndReimport();
        }

        // Wrap Mode карты шума (тикет 06): трипланарная проекция сэмплирует до трёх плоскостей
        // у краёв объекта, и Clamp там растягивает крайний тексель в полосу — артефакт,
        // который на глаз читается как сломанная проекция, а не как настройка импорта.
        // С кнопкой Fix, как и у остальных проверок этого файла.
        public static void DrawWrapModeCheck(Texture texture, TextureWrapMode expected, GUIContent message)
        {
            if (texture == null) return;

            string path = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(path)) return;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            if (importer.wrapMode == expected) return;

            if (DrawActionWarning(message,
                new GUIContent("Fix", "Исправить только Wrap Mode этой текстуры."),
                FixButtonWidth))
            {
                importer.wrapMode = expected;
                importer.SaveAndReimport();
            }
        }

        // Stateless: строка рисуется, пока вызывающий код видит реальную проблему импорта.
        // Малый штатный warning icon сохраняет семантику HelpBox, но не раздувает строку.
        private static bool DrawActionWarning(GUIContent message, GUIContent action,
            float actionWidth)
        {
            if (warningIcon == null)
                warningIcon = EditorGUIUtility.IconContent("console.warnicon.sml").image;

            if (warningTextStyle == null)
            {
                warningTextStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = true
                };
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label(warningIcon, GUIStyle.none,
                GUILayout.Width(WarningIconSize), GUILayout.Height(WarningIconSize));
            GUILayout.Label(message, warningTextStyle, GUILayout.ExpandWidth(true));
            bool actionRequested = GUILayout.Button(action, EditorStyles.miniButton,
                GUILayout.Width(actionWidth),
                GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.EndHorizontal();

            return actionRequested;
        }
    }
}
