using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    /// <summary>
    /// Read-only audit of Assets/Art against the actual build dependency graph.
    /// Nothing is moved, renamed or deleted — the tool only reports.
    ///
    /// Build root: enabled scenes from Build Settings + every asset under any Resources
    /// folder + Always Included Shaders + Preloaded Assets. Addressables are not installed,
    /// so this root is complete for the current project (see docs/art-content-organization-assessment.md §2).
    ///
    /// Four buckets:
    ///   A  in Art, reachable from the build root                  — production, do not touch
    ///   B  in Art, reachable only from non-build project scenes   — grey zone, needs a decision
    ///   C  in Art, not reachable at all                           — candidates to move out
    ///   D  outside Art, reachable from the build root             — candidates to move in
    ///
    /// Also collects the actual prefix/suffix vocabulary in use, to compare against
    /// docs/asset-organization-and-naming.md §6.3 and §6.5.
    /// </summary>
    public static class ArtContentAudit
    {
        private const string ArtRoot = "Assets/Art/";
        private const string ReportFolder = ".scratch/art-cleanup";
        private const string GraphicsSettingsPath = "ProjectSettings/GraphicsSettings.asset";

        // Vendor packages that currently live inside Art. Listed so their files can be
        // counted separately: per asset-organization-and-naming.md §8.1 a vendor island
        // is not normalized to project rules, so mixing it into the cleanup list is noise.
        private static readonly string[] VendorIslandsInArt =
        {
            "Assets/Art/Shaders/AllIn13DShader/",
            "Assets/Art/Shaders/AllIn1SpriteShader/",
            "Assets/Art/Shaders/AllIn1VfxToolkit/",
            "Assets/Art/Shaders/Toony Colors Pro/",
            "Assets/Art/Shaders/Toon Shaders/",
            "Assets/Art/Materials/Toon Materials/",
            "Assets/Art/Shaders/VolumetricFog2/",
            "Assets/Art/VFX/HighlightPlusBundle/",
            "Assets/Art/VFX/Piloto Studio/",
            "Assets/Art/VFX/SrRubfish_VFX_03/",
            "Assets/Art/CozyWeatherPF/",
        };

        // Roots outside Art where a build-bound asset is legitimate: engine contract,
        // project config, code or a vendor island. Bucket D ignores them.
        private static readonly string[] LegitimateOutsideArt =
        {
            "Assets/Resources/",
            "Assets/Settings/",
            "Assets/Scenes/",
            "Assets/Scripts/",
            "Assets/Editor/",
            "Assets/Plugins/",
            "Assets/Packages/",
            "Assets/MCP/",
            "Assets/Timeline/",
            "Assets/TextMesh Pro/",
            "Assets/TutorialInfo/",
        };

        private static readonly string[] ArtExtensions =
        {
            ".png", ".tga", ".jpg", ".jpeg", ".tif", ".tiff", ".exr", ".psd", ".hdr", ".cubemap",
            ".mat", ".physicmaterial", ".terrainlayer", ".rendertexture",
            ".fbx", ".obj", ".blend", ".dae",
            ".prefab", ".anim", ".controller", ".mask", ".overridecontroller",
            ".shader", ".shadergraph", ".hlsl", ".cginc", ".shadervariants",
            ".wav", ".mp3", ".ogg", ".aiff",
            ".mp4", ".mov", ".webm",
            ".ttf", ".otf",
        };

        private static readonly string[] TextureMapSuffixes =
        {
            "BC", "N", "AO", "M", "R", "S", "E", "Mask", "H", "O",
        };

        private static readonly string[] PrefabRoles =
        {
            "Actor", "Chunk", "Kit", "Prop", "UI", "FX",
        };

        private class Entry
        {
            public string Path;
            public long Bytes;
            public bool IsVendor;
        }

        [MenuItem("GD Tools/Art Content Audit")]
        public static void Run()
        {
            List<string> allAssets = CollectProjectAssets();

            string[] buildRoots = CollectBuildRoots(allAssets);
            HashSet<string> buildSet = new HashSet<string>(AssetDatabase.GetDependencies(buildRoots, true));
            foreach (string root in buildRoots)
                buildSet.Add(root);

            string[] nonBuildScenes = CollectNonBuildScenes(allAssets, buildSet);
            HashSet<string> projectOnlySet = new HashSet<string>(AssetDatabase.GetDependencies(nonBuildScenes, true));
            foreach (string scene in nonBuildScenes)
                projectOnlySet.Add(scene);
            projectOnlySet.ExceptWith(buildSet);

            List<Entry> inBuild = new List<Entry>();
            List<Entry> projectOnly = new List<Entry>();
            List<Entry> unreferenced = new List<Entry>();
            List<Entry> outsideArtInBuild = new List<Entry>();

            foreach (string path in allAssets)
            {
                Entry entry = MakeEntry(path);

                if (path.StartsWith(ArtRoot, StringComparison.Ordinal))
                {
                    if (buildSet.Contains(path))
                        inBuild.Add(entry);
                    else if (projectOnlySet.Contains(path))
                        projectOnly.Add(entry);
                    else
                        unreferenced.Add(entry);
                }
                else if (buildSet.Contains(path) && IsArtLikeAsset(path) && !IsLegitimateOutsideArt(path))
                {
                    outsideArtInBuild.Add(entry);
                }
            }

            string report = BuildReport(
                allAssets, buildRoots, nonBuildScenes,
                inBuild, projectOnly, unreferenced, outsideArtInBuild);

            WriteReport(report, unreferenced, projectOnly, outsideArtInBuild);
        }

        private static List<string> CollectProjectAssets()
        {
            List<string> result = new List<string>();
            foreach (string path in AssetDatabase.GetAllAssetPaths())
            {
                if (!path.StartsWith("Assets/", StringComparison.Ordinal))
                    continue;
                if (AssetDatabase.IsValidFolder(path))
                    continue;
                result.Add(path);
            }
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        private static string[] CollectBuildRoots(List<string> allAssets)
        {
            HashSet<string> roots = new HashSet<string>(StringComparer.Ordinal);

            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled && !string.IsNullOrEmpty(scene.path))
                    roots.Add(scene.path);
            }

            // Unity puts everything under any Resources folder into the player build,
            // reachable by string path — the dependency graph cannot see those calls.
            foreach (string path in allAssets)
            {
                if (path.Contains("/Resources/"))
                    roots.Add(path);
            }

            foreach (string path in CollectAlwaysIncludedShaders())
                roots.Add(path);

            foreach (UnityEngine.Object preloaded in PlayerSettings.GetPreloadedAssets())
            {
                if (preloaded == null)
                    continue;
                string path = AssetDatabase.GetAssetPath(preloaded);
                if (!string.IsNullOrEmpty(path))
                    roots.Add(path);
            }

            return roots.ToArray();
        }

        private static List<string> CollectAlwaysIncludedShaders()
        {
            List<string> result = new List<string>();
            UnityEngine.Object[] settingsAssets = AssetDatabase.LoadAllAssetsAtPath(GraphicsSettingsPath);
            if (settingsAssets == null || settingsAssets.Length == 0)
                return result;

            SerializedObject settings = new SerializedObject(settingsAssets[0]);
            SerializedProperty shaders = settings.FindProperty("m_AlwaysIncludedShaders");
            if (shaders == null || !shaders.isArray)
                return result;

            for (int i = 0; i < shaders.arraySize; i++)
            {
                UnityEngine.Object shader = shaders.GetArrayElementAtIndex(i).objectReferenceValue;
                if (shader == null)
                    continue;
                string path = AssetDatabase.GetAssetPath(shader);
                if (!string.IsNullOrEmpty(path) && path.StartsWith("Assets/", StringComparison.Ordinal))
                    result.Add(path);
            }
            return result;
        }

        private static string[] CollectNonBuildScenes(List<string> allAssets, HashSet<string> buildSet)
        {
            List<string> result = new List<string>();
            foreach (string path in allAssets)
            {
                if (path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) && !buildSet.Contains(path))
                    result.Add(path);
            }
            return result.ToArray();
        }

        private static Entry MakeEntry(string path)
        {
            long bytes = 0;
            FileInfo info = new FileInfo(path);
            if (info.Exists)
                bytes = info.Length;

            return new Entry
            {
                Path = path,
                Bytes = bytes,
                IsVendor = IsVendorInArt(path),
            };
        }

        private static bool IsVendorInArt(string path)
        {
            foreach (string island in VendorIslandsInArt)
            {
                if (path.StartsWith(island, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static bool IsLegitimateOutsideArt(string path)
        {
            foreach (string root in LegitimateOutsideArt)
            {
                if (path.StartsWith(root, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static bool IsArtLikeAsset(string path)
        {
            string extension = Path.GetExtension(path).ToLowerInvariant();
            return ArtExtensions.Contains(extension);
        }

        private static string BuildReport(
            List<string> allAssets,
            string[] buildRoots,
            string[] nonBuildScenes,
            List<Entry> inBuild,
            List<Entry> projectOnly,
            List<Entry> unreferenced,
            List<Entry> outsideArtInBuild)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("# Аудит Assets/Art против графа зависимостей билда");
            sb.AppendLine();
            sb.AppendLine("**Сгенерировано:** " + DateTime.Now.ToString("dd.MM.yyyy HH:mm") +
                          " · `GD Tools/Art Content Audit`");
            sb.AppendLine("**Режим:** только чтение. Ни один файл не перемещён, не переименован и не удалён.");
            sb.AppendLine();

            sb.AppendLine("## 1. Корень зависимостей");
            sb.AppendLine();
            sb.AppendLine("Всего ассетов в `Assets`: " + allAssets.Count);
            sb.AppendLine("Корневых входов билда: " + buildRoots.Length +
                          " (включённые сцены + всё под `Resources` + Always Included Shaders + Preloaded Assets)");
            sb.AppendLine("Сцен проекта вне билда: " + nonBuildScenes.Length);
            sb.AppendLine();
            sb.AppendLine("Включённые сцены билда:");
            sb.AppendLine();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                    sb.AppendLine("- `" + scene.path + "`");
            }
            sb.AppendLine();
            if (nonBuildScenes.Length > 0)
            {
                sb.AppendLine("Сцены вне билда (корень категории B):");
                sb.AppendLine();
                foreach (string scene in nonBuildScenes)
                    sb.AppendLine("- `" + scene + "`");
                sb.AppendLine();
            }

            sb.AppendLine("## 2. Сводка");
            sb.AppendLine();
            sb.AppendLine("| Категория | Файлов | Размер | Своё | Вендор в Art |");
            sb.AppendLine("|---|---:|---:|---:|---:|");
            AppendSummaryRow(sb, "A · в Art, идёт в билд", inBuild);
            AppendSummaryRow(sb, "B · в Art, только не-билдовые сцены", projectOnly);
            AppendSummaryRow(sb, "C · в Art, ни с чем не связано", unreferenced);
            AppendSummaryRow(sb, "D · вне Art, идёт в билд", outsideArtInBuild);
            sb.AppendLine();
            sb.AppendLine("A — трогать нельзя. B — требует решения по каждой сцене-владельцу.");
            sb.AppendLine("C — кандидаты на вывоз из Art. D — кандидаты на въезд в Art.");
            sb.AppendLine();

            AppendTopList(sb, "## 3. C · крупнейшее несвязанное в Art", unreferenced, 40);
            AppendTopList(sb, "## 4. B · крупнейшее связанное только с не-билдовыми сценами", projectOnly, 25);
            AppendTopList(sb, "## 5. D · крупнейшее вне Art, идущее в билд", outsideArtInBuild, 40);

            AppendFolderBreakdown(sb, "## 6. C · несвязанное по папкам Art", unreferenced);

            AppendNamingInventory(sb, inBuild);

            sb.AppendLine("## 8. Слепые зоны этого аудита");
            sb.AppendLine();
            sb.AppendLine("Граф зависимостей не видит:");
            sb.AppendLine();
            sb.AppendLine("- загрузку по строке, собранной в рантайме (не через `Resources`, которое здесь покрыто целиком);");
            sb.AppendLine("- ссылку шейдера на другой шейдер по имени через `Fallback` или `UsePass`;");
            sb.AppendLine("- ассет, нужный только editor-тулингу: он попадёт в C, хотя нужен;");
            sb.AppendLine("- содержимое, на которое ссылается только выключенная ветка префаба — она всё равно в билде, но это граф видит;");
            sb.AppendLine("- будущий контент, ещё не подключённый к сцене: он неотличим от мусора.");
            sb.AppendLine();
            sb.AppendLine("Поэтому категория C — **список кандидатов, а не приговор**. Каждая партия смотрится глазами.");
            sb.AppendLine();

            return sb.ToString();
        }

        private static void AppendSummaryRow(StringBuilder sb, string label, List<Entry> entries)
        {
            long total = entries.Sum(e => e.Bytes);
            int vendorCount = entries.Count(e => e.IsVendor);
            long vendorBytes = entries.Where(e => e.IsVendor).Sum(e => e.Bytes);
            long ownBytes = total - vendorBytes;

            sb.AppendLine("| " + label + " | " + entries.Count + " | " + Mib(total) +
                          " | " + Mib(ownBytes) + " | " + vendorCount + " шт / " + Mib(vendorBytes) + " |");
        }

        private static void AppendTopList(StringBuilder sb, string header, List<Entry> entries, int take)
        {
            sb.AppendLine(header);
            sb.AppendLine();
            if (entries.Count == 0)
            {
                sb.AppendLine("Пусто.");
                sb.AppendLine();
                return;
            }

            sb.AppendLine("Показано " + Math.Min(take, entries.Count) + " из " + entries.Count +
                          ". Полный список — в `.txt` рядом с этим отчётом.");
            sb.AppendLine();
            sb.AppendLine("| Размер | Путь | Вендор |");
            sb.AppendLine("|---:|---|:-:|");
            foreach (Entry entry in entries.OrderByDescending(e => e.Bytes).Take(take))
            {
                sb.AppendLine("| " + Mib(entry.Bytes) + " | `" + entry.Path + "` | " +
                              (entry.IsVendor ? "да" : "") + " |");
            }
            sb.AppendLine();
        }

        private static void AppendFolderBreakdown(StringBuilder sb, string header, List<Entry> entries)
        {
            sb.AppendLine(header);
            sb.AppendLine();
            if (entries.Count == 0)
            {
                sb.AppendLine("Пусто.");
                sb.AppendLine();
                return;
            }

            Dictionary<string, List<Entry>> byFolder = new Dictionary<string, List<Entry>>(StringComparer.Ordinal);
            foreach (Entry entry in entries)
            {
                string folder = SecondLevelFolder(entry.Path);
                if (!byFolder.TryGetValue(folder, out List<Entry> bucket))
                {
                    bucket = new List<Entry>();
                    byFolder[folder] = bucket;
                }
                bucket.Add(entry);
            }

            sb.AppendLine("| Папка | Файлов | Размер |");
            sb.AppendLine("|---|---:|---:|");
            foreach (KeyValuePair<string, List<Entry>> pair in byFolder.OrderByDescending(p => p.Value.Sum(e => e.Bytes)))
            {
                sb.AppendLine("| `" + pair.Key + "` | " + pair.Value.Count + " | " +
                              Mib(pair.Value.Sum(e => e.Bytes)) + " |");
            }
            sb.AppendLine();
        }

        private static string SecondLevelFolder(string path)
        {
            string tail = path.Substring(ArtRoot.Length);
            int firstSlash = tail.IndexOf('/');
            if (firstSlash < 0)
                return ArtRoot + "<корень>";

            string first = tail.Substring(0, firstSlash);
            string rest = tail.Substring(firstSlash + 1);
            int secondSlash = rest.IndexOf('/');
            if (secondSlash < 0)
                return ArtRoot + first;

            return ArtRoot + first + "/" + rest.Substring(0, secondSlash);
        }

        private static void AppendNamingInventory(StringBuilder sb, List<Entry> inBuild)
        {
            List<Entry> own = inBuild.Where(e => !e.IsVendor).ToList();

            sb.AppendLine("## 7. Фактический словарь имён");
            sb.AppendLine();
            sb.AppendLine("Считано по категории A без вендорских островов — " + own.Count +
                          " файлов. Это то, что действительно идёт в билд, поэтому именно");
            sb.AppendLine("здесь решение по неймингу имеет цену.");
            sb.AppendLine();

            AppendPrefixTable(sb, "### 7.1 Текстуры (`TEX_` по §6.3)", own,
                new[] { ".png", ".tga", ".jpg", ".jpeg", ".tif", ".tiff", ".exr", ".psd", ".hdr" });
            AppendTextureSuffixTable(sb, own);
            AppendPrefixTable(sb, "### 7.3 Материалы (`MAT_` по §6.3)", own, new[] { ".mat" });
            AppendPrefixTable(sb, "### 7.4 Модели (`MSH_` по §6.3)", own, new[] { ".fbx", ".obj", ".blend" });
            AppendPrefabTable(sb, own);
            AppendPrefixTable(sb, "### 7.6 Шейдеры (`SHD_` / `SHG_` по §6.3)", own,
                new[] { ".shader", ".shadergraph" });
            AppendPrefixTable(sb, "### 7.7 Анимации (`ANIM_` / `AC_` по §6.3)", own,
                new[] { ".anim", ".controller" });
        }

        private static void AppendPrefixTable(StringBuilder sb, string header, List<Entry> own, string[] extensions)
        {
            sb.AppendLine(header);
            sb.AppendLine();

            List<string> names = own
                .Where(e => extensions.Contains(Path.GetExtension(e.Path).ToLowerInvariant()))
                .Select(e => Path.GetFileNameWithoutExtension(e.Path))
                .ToList();

            if (names.Count == 0)
            {
                sb.AppendLine("Нет файлов этого типа в категории A.");
                sb.AppendLine();
                return;
            }

            Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (string name in names)
            {
                string prefix = PrefixOf(name);
                counts.TryGetValue(prefix, out int current);
                counts[prefix] = current + 1;
            }

            sb.AppendLine("Всего: " + names.Count + " · различных префиксов: " + counts.Count);
            sb.AppendLine();
            sb.AppendLine("| Префикс | Файлов | Доля |");
            sb.AppendLine("|---|---:|---:|");
            foreach (KeyValuePair<string, int> pair in counts.OrderByDescending(p => p.Value).Take(15))
            {
                sb.AppendLine("| `" + pair.Key + "` | " + pair.Value + " | " +
                              (100f * pair.Value / names.Count).ToString("0.0") + "% |");
            }
            if (counts.Count > 15)
                sb.AppendLine("| … ещё " + (counts.Count - 15) + " | | |");
            sb.AppendLine();
        }

        private static void AppendTextureSuffixTable(StringBuilder sb, List<Entry> own)
        {
            sb.AppendLine("### 7.2 Суффиксы карт текстур (§6.5)");
            sb.AppendLine();

            string[] extensions = { ".png", ".tga", ".jpg", ".jpeg", ".tif", ".tiff", ".exr", ".psd", ".hdr" };
            List<string> names = own
                .Where(e => extensions.Contains(Path.GetExtension(e.Path).ToLowerInvariant()))
                .Select(e => Path.GetFileNameWithoutExtension(e.Path))
                .ToList();

            if (names.Count == 0)
            {
                sb.AppendLine("Нет текстур в категории A.");
                sb.AppendLine();
                return;
            }

            Dictionary<string, int> known = new Dictionary<string, int>(StringComparer.Ordinal);
            Dictionary<string, int> other = new Dictionary<string, int>(StringComparer.Ordinal);
            int noSuffix = 0;

            foreach (string name in names)
            {
                int lastUnderscore = name.LastIndexOf('_');
                if (lastUnderscore < 0 || lastUnderscore == name.Length - 1)
                {
                    noSuffix++;
                    continue;
                }

                string suffix = name.Substring(lastUnderscore + 1);
                Dictionary<string, int> target = TextureMapSuffixes.Contains(suffix) ? known : other;
                target.TryGetValue(suffix, out int current);
                target[suffix] = current + 1;
            }

            int knownTotal = known.Values.Sum();
            sb.AppendLine("Всего текстур: " + names.Count + " · по словарю §6.5: " + knownTotal +
                          " · иной суффикс: " + other.Values.Sum() + " · без суффикса: " + noSuffix);
            sb.AppendLine();
            sb.AppendLine("| Суффикс | Файлов | По словарю §6.5 |");
            sb.AppendLine("|---|---:|:-:|");
            foreach (KeyValuePair<string, int> pair in known.OrderByDescending(p => p.Value))
                sb.AppendLine("| `_" + pair.Key + "` | " + pair.Value + " | да |");
            foreach (KeyValuePair<string, int> pair in other.OrderByDescending(p => p.Value).Take(25))
                sb.AppendLine("| `_" + pair.Key + "` | " + pair.Value + " | |");
            if (other.Count > 25)
                sb.AppendLine("| … ещё " + (other.Count - 25) + " | | |");
            sb.AppendLine();
        }

        private static void AppendPrefabTable(StringBuilder sb, List<Entry> own)
        {
            sb.AppendLine("### 7.5 Префабы: префикс и роль (§6.3, §6.4)");
            sb.AppendLine();

            List<string> names = own
                .Where(e => Path.GetExtension(e.Path).Equals(".prefab", StringComparison.OrdinalIgnoreCase))
                .Select(e => Path.GetFileNameWithoutExtension(e.Path))
                .ToList();

            if (names.Count == 0)
            {
                sb.AppendLine("Нет префабов в категории A.");
                sb.AppendLine();
                return;
            }

            int withPfPrefix = names.Count(n => n.StartsWith("PF_", StringComparison.Ordinal));
            sb.AppendLine("Всего префабов: " + names.Count + " · с префиксом `PF_`: " + withPfPrefix +
                          " · без него: " + (names.Count - withPfPrefix));
            sb.AppendLine();

            sb.AppendLine("| Роль по §6.4 | Файлов |");
            sb.AppendLine("|---|---:|");
            foreach (string role in PrefabRoles)
            {
                int count = names.Count(n => n.StartsWith("PF_" + role + "_", StringComparison.Ordinal));
                sb.AppendLine("| `PF_" + role + "_` | " + count + " |");
            }
            int chunkAlt = names.Count(n => n.StartsWith("CHN_", StringComparison.Ordinal) ||
                                            n.StartsWith("CNK_", StringComparison.Ordinal));
            sb.AppendLine("| `CHN_` / `CNK_` (вне словаря) | " + chunkAlt + " |");
            sb.AppendLine();

            Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (string name in names)
            {
                string prefix = PrefixOf(name);
                counts.TryGetValue(prefix, out int current);
                counts[prefix] = current + 1;
            }

            sb.AppendLine("Фактические префиксы:");
            sb.AppendLine();
            sb.AppendLine("| Префикс | Файлов |");
            sb.AppendLine("|---|---:|");
            foreach (KeyValuePair<string, int> pair in counts.OrderByDescending(p => p.Value).Take(15))
                sb.AppendLine("| `" + pair.Key + "` | " + pair.Value + " |");
            if (counts.Count > 15)
                sb.AppendLine("| … ещё " + (counts.Count - 15) + " | |");
            sb.AppendLine();
        }

        private static string PrefixOf(string nameWithoutExtension)
        {
            int firstUnderscore = nameWithoutExtension.IndexOf('_');
            if (firstUnderscore <= 0)
                return "<без префикса>";
            return nameWithoutExtension.Substring(0, firstUnderscore + 1);
        }

        private static string Mib(long bytes)
        {
            return (bytes / 1048576f).ToString("0.00") + " MiB";
        }

        private static void WriteReport(
            string report,
            List<Entry> unreferenced,
            List<Entry> projectOnly,
            List<Entry> outsideArtInBuild)
        {
            Directory.CreateDirectory(ReportFolder);

            string reportPath = Path.Combine(ReportFolder, "audit-report.md");
            File.WriteAllText(reportPath, report, new UTF8Encoding(false));

            WriteList(Path.Combine(ReportFolder, "C-art-unreferenced.txt"), unreferenced);
            WriteList(Path.Combine(ReportFolder, "B-art-project-only.txt"), projectOnly);
            WriteList(Path.Combine(ReportFolder, "D-outside-art-in-build.txt"), outsideArtInBuild);

            Debug.Log("Art Content Audit: отчёт записан в " + reportPath +
                      "\nC (несвязано в Art): " + unreferenced.Count + " файлов, " + Mib(unreferenced.Sum(e => e.Bytes)) +
                      "\nB (только не-билдовые сцены): " + projectOnly.Count + " файлов, " + Mib(projectOnly.Sum(e => e.Bytes)) +
                      "\nD (вне Art, в билде): " + outsideArtInBuild.Count + " файлов, " + Mib(outsideArtInBuild.Sum(e => e.Bytes)));
        }

        private static void WriteList(string path, List<Entry> entries)
        {
            StringBuilder sb = new StringBuilder();
            foreach (Entry entry in entries.OrderByDescending(e => e.Bytes))
            {
                sb.AppendLine(Mib(entry.Bytes).PadLeft(12) + "  " +
                              (entry.IsVendor ? "[vendor] " : "         ") + entry.Path);
            }
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        }
    }
}
