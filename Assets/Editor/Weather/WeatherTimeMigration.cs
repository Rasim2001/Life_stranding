using System;
using System.Text;
using UnityEditor;
using UnityEngine;
using WeatherSystem;
using WeatherSystem.Profiles;

namespace SpiderRig.Editor.Weather
{
    // Одноразовая миграция данных под смену шкалы времени (см. WeatherTime.cs):
    // старое 0 = восход становится 0.25, т.е. каждый ключ градиента/кривой сдвигается
    // на +0.25 с заворотом в [0,1). Плюс: AmbientMultiplier — новое поле профиля
    // (дефолт DailyFloat._constant = 0), на существующих профилях без миграции ambient
    // погас бы целиком; плюс WeatherRig._startingTimeOfDay01 в префабе — та же ось,
    // тот же сдвиг.
    //
    // Самопроверка — главная страховка: до сдвига каждое поле сэмплится в 32 точках,
    // после сдвига сэмплится в соответствующих сдвинутых точках, инвариант
    // new.Evaluate(t + 0.25) == old.Evaluate(t). Ненулевая дельта в отчёте — миграция
    // сломана, откатываться (Edit > Undo History или git checkout ассетов).
    public static class WeatherTimeMigration
    {
        private const float Shift = 0.25f;
        private const int SampleCount = 32;
        private const string RigPrefabPath = "Assets/Resources/Prefabs/WeatherSystem/PF_Actor_Weather_Rig.prefab";

        // [MenuItem] с диалогом — только для интерактивного запуска человеком. Диалог
        // модальный и блокирует поток, поэтому вызывать RunCore() напрямую (например,
        // из execute_code) нужно в обход этого метода — иначе таймаут на стороне
        // вызывающего кода и последующий повторный вызов способны запустить миграцию
        // повторно поверх уже сдвинутых данных.
        [MenuItem("SpiderRig/Weather/Migrate Time Convention (one-shot)")]
        private static void Run()
        {
            if (!EditorUtility.DisplayDialog("Migrate Time Convention",
                    "Сдвигает все суточные градиенты/кривые в SkyBandProfile на +0.25 " +
                    "(0=восход → 0=полночь), проставляет AmbientMultiplier=1 на существующих " +
                    "профилях и правит _startingTimeOfDay01 в PF_Actor_Weather_Rig.prefab.\n\n" +
                    "Запускать один раз. Продолжить?",
                    "Мигрировать", "Отмена"))
                return;

            string result = RunCore();
            EditorUtility.DisplayDialog("Migrate Time Convention", result, "OK");
        }

        // Без диалога — детерминированный одноразовый запуск, безопасный для вызова
        // из execute_code. Возвращает отчёт той же строкой, что уходит в Debug.Log.
        public static string RunCore()
        {
            var report = new StringBuilder();
            float maxDelta = 0f;

            foreach (string guid in AssetDatabase.FindAssets("t:SkyBandProfile"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<SkyBandProfile>(path);
                maxDelta = Mathf.Max(maxDelta, MigrateProfile(profile, report));
            }

            MigratePrefab(report);

            AssetDatabase.SaveAssets();

            string header = $"Максимальная дельта самопроверки по всем полям: {maxDelta:e3}\n\n";
            string full = header + report;
            Debug.Log("[WeatherTimeMigration]\n" + full);
            return full;
        }

        private static float MigrateProfile(SkyBandProfile profile, StringBuilder report)
        {
            var so = new SerializedObject(profile);
            SerializedProperty it = so.GetIterator();
            bool enterChildren = true;
            float maxDelta = 0f;

            while (it.NextVisible(enterChildren))
            {
                enterChildren = false;

                SerializedProperty mode = it.FindPropertyRelative("_mode");
                if (mode == null || mode.propertyType != SerializedPropertyType.Enum)
                    continue;

                // Constant — суточной оси нет, сдвигать нечего.
                if (mode.enumValueIndex == 0)
                    continue;

                SerializedProperty gradientProp = it.FindPropertyRelative("_gradient");
                if (gradientProp != null)
                    maxDelta = Mathf.Max(maxDelta, MigrateGradient(profile.name, it.name, gradientProp, report));

                SerializedProperty curveProp = it.FindPropertyRelative("_curve");
                if (curveProp != null)
                    maxDelta = Mathf.Max(maxDelta, MigrateCurve(profile.name, it.name, curveProp, report));
            }

            // Новое поле: если ещё не тронуто (Constant, 0 — дефолт DailyFloat), ставим
            // нейтральный множитель 1. Не затираем, если кто-то уже успел настроить.
            SerializedProperty ambientMultiplier = so.FindProperty("AmbientMultiplier");
            if (ambientMultiplier != null)
            {
                SerializedProperty m = ambientMultiplier.FindPropertyRelative("_mode");
                SerializedProperty c = ambientMultiplier.FindPropertyRelative("_constant");
                if (m != null && c != null && m.enumValueIndex == 0 && c.floatValue == 0f)
                {
                    c.floatValue = 1f;
                    report.AppendLine($"{profile.name}.AmbientMultiplier: 0 → 1 (новое поле, нейтральный дефолт)");
                }
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(profile);
            return maxDelta;
        }

        // Насколько раньше точки шва берём вторую синтетическую точку — landing чуть
        // ниже new_t=1 после сдвига (Mathf.Repeat(seamOldT+Shift,1) сам по себе всегда
        // даёт ровно 0, никогда 1 — 1.0 у Repeat заворачивается в 0.0, поэтому для
        // "правого" края нужна отдельная точка чуть раньше шва, а не тот же seamOldT).
        private const float SeamEpsilon = 1e-4f;

        // Gradient/AnimationCurve не поддерживают циклическую интерполяцию — Evaluate()
        // вне диапазона ключей клампится к ближайшему краю, а не заворачивает на другой
        // конец. Ровно один шов (переход последний ключ → первый по кругу) существует
        // ВСЕГДА и просто переезжает вместе со сдвигом, а не устраняется им. Если пара
        // ключей, раньше плавно переходивших друг в друга ВНУТРИ диапазона, после сдвига
        // попадёт на новую границу [0,1] — их интерполяция обрывается в жёсткий кламп
        // с ОБЕИХ сторон шва (проверено эмпирически: одной синтетической точки хватает
        // только когда усечённый участок узкий; на широком — кламп с "дальней" стороны
        // остаётся заметным).
        //
        // Чиним двумя синтетическими точками ровно у будущего шва (в старых координатах:
        // 1-Shift и 1-Shift-SeamEpsilon) — обе с одним и тем же досэмплированным значением
        // ДО сдвига. После сдвига одна ложится на new_t=0, другая — вплотную перед new_t=1,
        // и обрыв интерполяции устранён с обеих сторон.
        private static float MigrateGradient(string ownerName, string fieldName, SerializedProperty gradientProp, StringBuilder report)
        {
            Gradient original = gradientProp.gradientValue;

            var oldSamples = new Color[SampleCount];
            for (int i = 0; i < SampleCount; i++)
                oldSamples[i] = original.Evaluate((float)i / SampleCount);

            float seamOldT = Mathf.Repeat(1f - Shift, 1f);
            Color seamColor = original.Evaluate(seamOldT);

            GradientColorKey[] colorKeys = InsertSeamKeys(original.colorKeys, seamOldT, seamColor,
                (t, c) => new GradientColorKey(c, t), out int colorSeamsAdded);
            GradientAlphaKey[] alphaKeys = InsertSeamKeys(original.alphaKeys, seamOldT, seamColor.a,
                (t, a) => new GradientAlphaKey(a, t), out int alphaSeamsAdded);

            for (int i = 0; i < colorKeys.Length; i++)
                colorKeys[i].time = Mathf.Repeat(colorKeys[i].time + Shift, 1f);
            for (int i = 0; i < alphaKeys.Length; i++)
                alphaKeys[i].time = Mathf.Repeat(alphaKeys[i].time + Shift, 1f);

            Array.Sort(colorKeys, (a, b) => a.time.CompareTo(b.time));
            Array.Sort(alphaKeys, (a, b) => a.time.CompareTo(b.time));

            var shifted = new Gradient { mode = original.mode };
            shifted.SetKeys(colorKeys, alphaKeys);
            gradientProp.gradientValue = shifted;

            if (colorSeamsAdded < 2 || alphaSeamsAdded < 2)
                report.AppendLine($"  ! {ownerName}.{fieldName}: на пределе 8 ключей, синтетический " +
                    $"шов вставлен не полностью (color +{colorSeamsAdded}, alpha +{alphaSeamsAdded}) — " +
                    "возможен резкий переход у новой границы t=0/1, см. дельту ниже");

            float maxDelta = 0f;
            for (int i = 0; i < SampleCount; i++)
            {
                float t = (float)i / SampleCount;
                Color newValue = shifted.Evaluate(Mathf.Repeat(t + Shift, 1f));
                maxDelta = Mathf.Max(maxDelta, ColorDelta(oldSamples[i], newValue));
            }

            report.AppendLine($"{ownerName}.{fieldName}: {colorKeys.Length} color key(s), " +
                $"{alphaKeys.Length} alpha key(s), макс. дельта={maxDelta:e3}");
            return maxDelta;
        }

        // Та же схема, что MigrateGradient. AnimationCurve не ограничен 8 ключами,
        // как Gradient, но для единообразия и на случай будущего лимита проверка та же.
        private static float MigrateCurve(string ownerName, string fieldName, SerializedProperty curveProp, StringBuilder report)
        {
            AnimationCurve original = curveProp.animationCurveValue;

            var oldSamples = new float[SampleCount];
            for (int i = 0; i < SampleCount; i++)
                oldSamples[i] = original.Evaluate((float)i / SampleCount);

            float seamOldT = Mathf.Repeat(1f - Shift, 1f);
            float seamValue = original.Evaluate(seamOldT);

            Keyframe[] keys = InsertSeamKeys(original.keys, seamOldT, seamValue,
                (t, v) => new Keyframe(t, v), out _);

            for (int i = 0; i < keys.Length; i++)
                keys[i].time = Mathf.Repeat(keys[i].time + Shift, 1f);
            Array.Sort(keys, (a, b) => a.time.CompareTo(b.time));

            var shifted = new AnimationCurve(keys);
            curveProp.animationCurveValue = shifted;

            float maxDelta = 0f;
            for (int i = 0; i < SampleCount; i++)
            {
                float t = (float)i / SampleCount;
                float newValue = shifted.Evaluate(Mathf.Repeat(t + Shift, 1f));
                maxDelta = Mathf.Max(maxDelta, Mathf.Abs(oldSamples[i] - newValue));
            }

            report.AppendLine($"{ownerName}.{fieldName}: {keys.Length} key(s), макс. дельта={maxDelta:e3}");
            return maxDelta;
        }

        // До 2 синтетических ключей (seamOldT и seamOldT-SeamEpsilon, то же значение) —
        // после сдвига один ложится ровно на new_t=0, другой вплотную перед new_t=1.
        // Gradient молча схлопывается в дефолт при превышении 8 ключей (проверено
        // эмпирически, исключения нет) — поэтому вставляем не больше, чем есть места,
        // и возвращаем фактическое количество добавленного для отчёта.
        private static T[] InsertSeamKeys<T, TValue>(T[] source, float seamOldT, TValue seamValue,
            Func<float, TValue, T> makeKey, out int added)
        {
            const int MaxKeys = 8;
            int room = Mathf.Max(0, MaxKeys - source.Length);
            added = Mathf.Min(2, room);

            if (added == 0)
                return source;

            var result = new T[source.Length + added];
            Array.Copy(source, result, source.Length);
            result[source.Length] = makeKey(seamOldT, seamValue);
            if (added == 2)
                result[source.Length + 1] = makeKey(Mathf.Max(0f, seamOldT - SeamEpsilon), seamValue);

            return result;
        }

        private static void MigratePrefab(StringBuilder report)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(RigPrefabPath);
            try
            {
                var rig = root.GetComponent<WeatherRig>();
                if (rig == null)
                {
                    report.AppendLine($"{RigPrefabPath}: компонент WeatherRig не найден — пропущено");
                    return;
                }

                var so = new SerializedObject(rig);
                SerializedProperty startTime = so.FindProperty("_startingTimeOfDay01");
                float oldValue = startTime.floatValue;
                float newValue = Mathf.Repeat(oldValue + Shift, 1f);
                startTime.floatValue = newValue;
                so.ApplyModifiedProperties();

                PrefabUtility.SaveAsPrefabAsset(root, RigPrefabPath);
                report.AppendLine($"{RigPrefabPath}: _startingTimeOfDay01 {oldValue:0.###} → {newValue:0.###}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static float ColorDelta(Color a, Color b) =>
            Mathf.Max(Mathf.Abs(a.r - b.r), Mathf.Max(Mathf.Abs(a.g - b.g),
                Mathf.Max(Mathf.Abs(a.b - b.b), Mathf.Abs(a.a - b.a))));
    }
}
