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
    /// folder + Always Included Shaders + Preloaded Assets + assets referenced from player-side
    /// ProjectSettings (pipeline, cursor, icons, splash). Addressables are not installed,
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
    public static class BuildContentAudit
    {
        private const string ArtRoot = "Assets/Art/";
        private const string ModulesRoot = "Assets/Modules/";
        private const string ReportFolder = ".scratch/build-audit";
        private const string GraphicsSettingsPath = "ProjectSettings/GraphicsSettings.asset";

        // Structure rules: docs/asset-organization-and-naming.md §8.3, §8.5: a leading '_' in a root folder of
        // Assets means "not in the build"; ThirdParty is vendor content that does ship.
        private const string ThirdPartyRoot = "Assets/ThirdParty/";
        private const string ExperimentsRoot = "Assets/_Experiments/";

        // Production zone: Art plus Modules. Old and new root names are both accepted while the
        // art-organization migration is in flight (old names go away with ticket 18).
        private static readonly string[] ProductionRoots = { ArtRoot, ModulesRoot };
        private static readonly string[] BlockoutRoots = { "Assets/_PolygonPrototype/", "Assets/_BlockoutKit/" };
        private static readonly string[] ToolsRoots = { "Assets/_TK_Tools/", "Assets/_Local/" };
        private static readonly string[] ChunkPrefabRoots =
        {
            "Assets/Art/Prefabs/Chunks/",
            "Assets/Art/Chunks/",
            "Assets/Art/Worlds/",
        };

        // Vendor content is counted separately; after the 09.2026 cleanup all vendors live in
        // Assets/ThirdParty, the list guards against a relapse.
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
            "Assets/ThirdParty/",
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

        // Old vocabulary plus the new one ("MAHS", "MS" from the 09.2026 dictionary).
        private static readonly string[] TextureMapSuffixes =
        {
            "BC", "N", "AO", "M", "R", "S", "E", "Mask", "H", "O", "MAHS", "MS",
        };

        private static readonly string[] PrefabRoles =
        {
            "Actor", "Chunk", "Kit", "Prop", "UI", "FX",
        };

        private static readonly string[] NewPrefabPrefixes =
        {
            "CHNK_", "CHR_", "ACT_", "KIT_", "PRP_", "FX_",
        };

        private class Entry
        {
            public string Path;
            public long Bytes;
            public bool IsVendor;
        }

        [MenuItem("GD Tools/Build Content Audit")]
        public static void Run()
        {
            List<string> allAssets = CollectProjectAssets();
            Dictionary<string, string[]> directDepsCache = new Dictionary<string, string[]>(StringComparer.Ordinal);

            string[] buildRoots = CollectBuildRoots(allAssets);
            HashSet<string> buildSet = CollectDependenciesStoppingAtScenes(buildRoots, directDepsCache);
            // Editor folders never ship, even when an asset in the build lists them as a dependency.
            buildSet.RemoveWhere(IsEditorOnly);
            // AssetDatabase does not see '#include', so shader includes are resolved from the text.
            List<string> includedByText = AddShaderIncludes(buildSet);

            string[] nonBuildScenes = CollectNonBuildScenes(allAssets, buildSet);
            HashSet<string> projectOnlySet = CollectDependenciesStoppingAtScenes(nonBuildScenes, directDepsCache);
            AddShaderIncludes(projectOnlySet);
            projectOnlySet.ExceptWith(buildSet);

            List<Entry> inBuild = new List<Entry>();
            List<Entry> projectOnly = new List<Entry>();
            List<Entry> unreferenced = new List<Entry>();
            List<Entry> outsideArtInBuild = new List<Entry>();

            foreach (string path in allAssets)
            {
                Entry entry = MakeEntry(path);

                if (IsProduction(path))
                {
                    // Module code compiles into the player whether or not an asset points at it,
                    // so the dependency graph says nothing about it; it is not content.
                    if (IsCodeFile(path) && path.StartsWith(ModulesRoot, StringComparison.Ordinal))
                        continue;
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

            StructureFindings structure = CheckStructure(allAssets, buildSet, directDepsCache);

            Dictionary<string, string> previousSnapshot = ReadSnapshot(SnapshotPath);
            Dictionary<string, string> currentSnapshot = MakeSnapshot(inBuild);

            string report = BuildReport(
                allAssets, buildRoots, nonBuildScenes,
                inBuild, projectOnly, unreferenced, outsideArtInBuild, structure,
                includedByText, previousSnapshot, currentSnapshot);

            WriteReport(report, unreferenced, projectOnly, outsideArtInBuild, structure, currentSnapshot);
        }

        private static bool IsProduction(string path)
        {
            return StartsWithAny(path, ProductionRoots);
        }

        private static bool IsCodeFile(string path)
        {
            string extension = Path.GetExtension(path).ToLowerInvariant();
            return extension == ".cs" || extension == ".asmdef" || extension == ".asmref";
        }

        private static bool StartsWithAny(string path, string[] roots)
        {
            foreach (string root in roots)
            {
                if (path.StartsWith(root, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        // Follows '#include "..."' from every shader/include file in the build set, transitively.
        // Only includes that resolve to a project asset outside Editor folders are added.
        // Returns the files added to the set.
        private static List<string> AddShaderIncludes(HashSet<string> buildSet)
        {
            System.Text.RegularExpressions.Regex includePattern = new System.Text.RegularExpressions.Regex(
                "^\\s*#\\s*include(?:_with_pragmas)?\\s+\"([^\"]+)\"",
                System.Text.RegularExpressions.RegexOptions.Multiline);

            List<string> added = new List<string>();
            Queue<string> queue = new Queue<string>(buildSet.Where(IsShaderText));

            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                if (!File.Exists(current))
                    continue;

                string directory = Path.GetDirectoryName(current);
                foreach (System.Text.RegularExpressions.Match match in includePattern.Matches(File.ReadAllText(current)))
                {
                    string target = ResolveInclude(directory, match.Groups[1].Value);
                    if (target == null || IsEditorOnly(target) || !buildSet.Add(target))
                        continue;

                    added.Add(target);
                    if (IsShaderText(target))
                        queue.Enqueue(target);
                }
            }

            added.Sort(StringComparer.Ordinal);
            return added;
        }

        private static bool IsShaderText(string path)
        {
            string extension = Path.GetExtension(path).ToLowerInvariant();
            return extension == ".shader" || extension == ".hlsl" || extension == ".cginc";
        }

        private static string ResolveInclude(string fromDirectory, string include)
        {
            string candidate = include.StartsWith("Assets/", StringComparison.Ordinal)
                ? include
                : Path.Combine(fromDirectory, include);
            string normalized = Path.GetFullPath(candidate).Replace('\\', '/');
            string projectRoot = Path.GetFullPath(".").Replace('\\', '/').TrimEnd('/') + "/";
            if (!normalized.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
                return null;

            string relative = normalized.Substring(projectRoot.Length);
            return relative.StartsWith("Assets/", StringComparison.Ordinal) && File.Exists(relative) ? relative : null;
        }

        private const string SnapshotPath = ReportFolder + "/A-guid-snapshot.txt";

        // guid -> path for category A. A moved file keeps its guid, so a move is not a difference;
        // a file that dropped out of the build and another that came in are.
        private static Dictionary<string, string> MakeSnapshot(List<Entry> inBuild)
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Entry entry in inBuild)
                result[AssetDatabase.AssetPathToGUID(entry.Path)] = entry.Path;
            return result;
        }

        private static Dictionary<string, string> ReadSnapshot(string path)
        {
            if (!File.Exists(path))
                return null;

            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string line in File.ReadAllLines(path))
            {
                int space = line.IndexOf(' ');
                if (space == 32)
                    result[line.Substring(0, space)] = line.Substring(space + 1).TrimStart();
            }
            return result;
        }

        private class DebtRow
        {
            public string Owner;
            public int Total;
            public int Geometry;
        }

        private class StructureFindings
        {
            // rule 1: build-reachable file under Assets/_* (blockout kit excluded)
            public List<string> UnderscoreInBuild = new List<string>();
            // rule 2: file outside _TK_Tools that depends on it
            public List<string> DependsOnTools = new List<string>();
            // rule 3: file in Art that depends on _Experiments
            public List<string> DependsOnExperiments = new List<string>();
            // blockout debt per scene and per chunk prefab
            public List<DebtRow> BlockoutDebt = new List<DebtRow>();

            public int ErrorCount
            {
                get { return UnderscoreInBuild.Count + DependsOnTools.Count + DependsOnExperiments.Count; }
            }
        }

        private static bool IsUnderscoreRoot(string path)
        {
            const string assets = "Assets/";
            if (!path.StartsWith(assets, StringComparison.Ordinal) || path.Length <= assets.Length)
                return false;
            return path[assets.Length] == '_';
        }

        private static bool IsChunkPrefab(string path)
        {
            return path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) &&
                   StartsWithAny(path, ChunkPrefabRoots);
        }

        private static bool IsGeometrySource(string path)
        {
            string extension = Path.GetExtension(path).ToLowerInvariant();
            return extension == ".prefab" || extension == ".fbx" || extension == ".obj" || extension == ".blend";
        }

        private static StructureFindings CheckStructure(
            List<string> allAssets, HashSet<string> buildSet, Dictionary<string, string[]> directDepsCache)
        {
            StructureFindings result = new StructureFindings();

            foreach (string path in allAssets)
            {
                bool inTools = StartsWithAny(path, ToolsRoots);

                if (buildSet.Contains(path) && IsUnderscoreRoot(path) && !StartsWithAny(path, BlockoutRoots))
                    result.UnderscoreInBuild.Add(path);

                bool inProduction = IsProduction(path);
                if (!inTools || inProduction)
                {
                    // Direct dependencies only: a chain is reported at its first tracked link.
                    string[] direct = GetDirectDependenciesCached(path, directDepsCache);
                    if (!inTools && direct.Any(d => StartsWithAny(d, ToolsRoots)))
                        result.DependsOnTools.Add(path);
                    if (inProduction && direct.Any(d => d.StartsWith(ExperimentsRoot, StringComparison.Ordinal)))
                        result.DependsOnExperiments.Add(path);
                }

                bool isScene = path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) &&
                               path.StartsWith("Assets/Scenes/", StringComparison.Ordinal);
                if (isScene || IsChunkPrefab(path))
                {
                    // Owner is the sole root: the walk stops at any other scene, so a shared
                    // multi-scene bake asset does not smuggle in the neighbour's blockout debt.
                    HashSet<string> reachable = CollectDependenciesStoppingAtScenes(new[] { path }, directDepsCache);
                    List<string> kit = reachable
                        .Where(d => StartsWithAny(d, BlockoutRoots))
                        .ToList();
                    result.BlockoutDebt.Add(new DebtRow
                    {
                        Owner = path,
                        Total = kit.Count,
                        Geometry = kit.Count(IsGeometrySource),
                    });
                }
            }

            result.DependsOnTools.Sort(StringComparer.Ordinal);
            result.DependsOnExperiments.Sort(StringComparer.Ordinal);
            result.BlockoutDebt = result.BlockoutDebt
                .OrderByDescending(r => r.Total).ThenBy(r => r.Owner, StringComparer.Ordinal).ToList();
            return result;
        }

        // BFS that treats a '.unity' scene as a dead end unless it is one of the roots: a scene
        // enters the build only via Build Settings, never because some other asset references it.
        private static HashSet<string> CollectDependenciesStoppingAtScenes(
            string[] roots, Dictionary<string, string[]> directDepsCache)
        {
            HashSet<string> rootSet = new HashSet<string>(roots, StringComparer.Ordinal);
            HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);
            Queue<string> queue = new Queue<string>();

            foreach (string root in roots)
            {
                result.Add(root);
                if (visited.Add(root))
                    queue.Enqueue(root);
            }

            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                string[] direct = GetDirectDependenciesCached(current, directDepsCache);
                foreach (string dep in direct)
                {
                    bool isForeignScene = dep.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) &&
                                          !rootSet.Contains(dep);
                    if (isForeignScene)
                        continue;

                    result.Add(dep);
                    if (visited.Add(dep))
                        queue.Enqueue(dep);
                }
            }

            return result;
        }

        private static string[] GetDirectDependenciesCached(string path, Dictionary<string, string[]> cache)
        {
            string[] deps;
            if (!cache.TryGetValue(path, out deps))
            {
                deps = AssetDatabase.GetDependencies(path, false);
                cache[path] = deps;
            }
            return deps;
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

        private static bool IsEditorOnly(string path)
        {
            return path.Contains("/Editor/");
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
                if (path.Contains("/Resources/") && !IsEditorOnly(path))
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

            foreach (string path in CollectPlayerSettingsReferences())
                roots.Add(path);

            return roots.ToArray();
        }

        // Settings that ship with the player: render pipeline assets (and through them
        // renderer features with their materials), cursor, icons, splash logos, VFX runtime.
        // EditorBuildSettings is excluded — it lists disabled scenes too.
        private static readonly string[] PlayerSettingsFiles =
        {
            "ProjectSettings/GraphicsSettings.asset",
            "ProjectSettings/QualitySettings.asset",
            "ProjectSettings/ProjectSettings.asset",
            "ProjectSettings/VFXManager.asset",
        };

        private static List<string> CollectPlayerSettingsReferences()
        {
            List<string> result = new List<string>();
            System.Text.RegularExpressions.Regex guidPattern =
                new System.Text.RegularExpressions.Regex("guid: ([0-9a-f]{32})");

            foreach (string file in PlayerSettingsFiles)
            {
                if (!File.Exists(file))
                    continue;
                foreach (System.Text.RegularExpressions.Match match in guidPattern.Matches(File.ReadAllText(file)))
                {
                    string path = AssetDatabase.GUIDToAssetPath(match.Groups[1].Value);
                    if (path.StartsWith("Assets/", StringComparison.Ordinal))
                        result.Add(path);
                }
            }

            return result;
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
            if (path.StartsWith(ThirdPartyRoot, StringComparison.Ordinal))
                return true;

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
            List<Entry> outsideArtInBuild,
            StructureFindings structure,
            List<string> includedByText,
            Dictionary<string, string> previousSnapshot,
            Dictionary<string, string> currentSnapshot)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("# Аудит содержимого билда: Assets/Art и структура против графа билда");
            sb.AppendLine();
            sb.AppendLine("**Сгенерировано:** " + DateTime.Now.ToString("dd.MM.yyyy HH:mm") +
                          " · `GD Tools/Build Content Audit`");
            sb.AppendLine("**Режим:** только чтение. Ни один файл не перемещён, не переименован и не удалён.");
            sb.AppendLine();

            sb.AppendLine("## 1. Корень зависимостей");
            sb.AppendLine();
            sb.AppendLine("Всего ассетов в `Assets`: " + allAssets.Count);
            sb.AppendLine("Корневых входов билда: " + buildRoots.Length +
                          " (включённые сцены + всё под `Resources` + Always Included Shaders + Preloaded Assets + ссылки из настроек плеера: пайплайн, курсор, иконки, сплэш)");
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
            sb.AppendLine("Продакшн-зона — `Assets/Art/` и `Assets/Modules/`; категории A–C считаются по обеим.");
            List<Entry> modulesInAudit = inBuild.Concat(projectOnly).Concat(unreferenced)
                .Where(e => e.Path.StartsWith(ModulesRoot, StringComparison.Ordinal)).ToList();
            List<Entry> modulesInBuild = inBuild
                .Where(e => e.Path.StartsWith(ModulesRoot, StringComparison.Ordinal)).ToList();
            sb.AppendLine("Из них `Assets/Modules/`: всего " + modulesInAudit.Count + " файлов / " +
                          Mib(modulesInAudit.Sum(e => e.Bytes)) + " · в A " + modulesInBuild.Count + " файлов / " +
                          Mib(modulesInBuild.Sum(e => e.Bytes)) + ".");
            sb.AppendLine();
            sb.AppendLine("A — трогать нельзя. B — требует решения по каждой сцене-владельцу.");
            sb.AppendLine("C — кандидаты на вывоз из Art. D — кандидаты на въезд в Art.");
            sb.AppendLine();

            AppendTopList(sb, "## 3. C · крупнейшее несвязанное в Art", unreferenced, 40);
            AppendTopList(sb, "## 4. B · крупнейшее связанное только с не-билдовыми сценами", projectOnly, 25);
            AppendTopList(sb, "## 5. D · крупнейшее вне Art, идущее в билд", outsideArtInBuild, 40);

            AppendFolderBreakdown(sb, "## 6. C · несвязанное по папкам Art и Modules", unreferenced);

            AppendNamingInventory(sb, inBuild);

            sb.AppendLine("## 8. Слепые зоны этого аудита");
            sb.AppendLine();
            sb.AppendLine("Граф зависимостей не видит:");
            sb.AppendLine();
            sb.AppendLine("- загрузку по строке, собранной в рантайме (не через `Resources`, которое здесь покрыто целиком);");
            sb.AppendLine("- ссылку шейдера на другой шейдер по имени через `Fallback` или `UsePass`;");
            sb.AppendLine("- `#include`: граф его не видит, поэтому аудит разбирает текст `.shader`/`.hlsl`/`.cginc` из билда сам " +
                          "(только `#include \"…\"` с путём внутри `Assets`; include через макрос и пакетные пути не ловятся). " +
                          "Найдено таким способом: " + includedByText.Count +
                          ", из них в продакшн-зоне (добавлено в A, перечислено ниже): " +
                          includedByText.Count(IsProduction) + ".");
            foreach (string path in includedByText.Where(IsProduction))
                sb.AppendLine("  - `" + path + "`");
            sb.AppendLine("- ассет, нужный только editor-тулингу: он попадёт в C, хотя нужен;");
            sb.AppendLine("- содержимое, на которое ссылается только выключенная ветка префаба — она всё равно в билде, но это граф видит;");
            sb.AppendLine("- будущий контент, ещё не подключённый к сцене: он неотличим от мусора.");
            sb.AppendLine();
            sb.AppendLine("Поэтому категория C — **список кандидатов, а не приговор**. Каждая партия смотрится глазами.");
            sb.AppendLine();

            AppendStructure(sb, structure);
            AppendSnapshotDiff(sb, previousSnapshot, currentSnapshot);

            return sb.ToString();
        }

        private static void AppendSnapshotDiff(
            StringBuilder sb, Dictionary<string, string> previous, Dictionary<string, string> current)
        {
            sb.AppendLine("## 10. Разница снимка категории A по GUID");
            sb.AppendLine();
            sb.AppendLine("Снимок: `" + SnapshotPath + "` (GUID → путь). Перенос файла разницы не даёт — GUID переезжает с ним.");
            sb.AppendLine();
            if (previous == null)
            {
                sb.AppendLine("Предыдущего снимка нет — это первый. Записано GUID: " + current.Count + ".");
                sb.AppendLine();
                return;
            }

            List<KeyValuePair<string, string>> gone = previous.Where(p => !current.ContainsKey(p.Key))
                .OrderBy(p => p.Value, StringComparer.Ordinal).ToList();
            List<KeyValuePair<string, string>> added = current.Where(p => !previous.ContainsKey(p.Key))
                .OrderBy(p => p.Value, StringComparer.Ordinal).ToList();
            sb.AppendLine("Было: " + previous.Count + " · стало: " + current.Count +
                          " · ушло из A: " + gone.Count + " · пришло в A: " + added.Count + ".");
            sb.AppendLine();
            AppendSnapshotList(sb, "Ушло из A (путь из прошлого снимка):", gone);
            AppendSnapshotList(sb, "Пришло в A:", added);
        }

        private static void AppendSnapshotList(StringBuilder sb, string header, List<KeyValuePair<string, string>> items)
        {
            if (items.Count == 0)
                return;
            sb.AppendLine(header);
            sb.AppendLine();
            foreach (KeyValuePair<string, string> item in items.Take(100))
                sb.AppendLine("- `" + item.Value + "` · " + item.Key);
            if (items.Count > 100)
                sb.AppendLine("- … ещё " + (items.Count - 100));
            sb.AppendLine();
        }

        private static void AppendStructure(StringBuilder sb, StructureFindings s)
        {
            sb.AppendLine("## 9. Правила структуры (`_` = не в билде)");
            sb.AppendLine();
            sb.AppendLine("Ошибок: **" + s.ErrorCount + "** (правило 1: " + s.UnderscoreInBuild.Count +
                          " · правило 2: " + s.DependsOnTools.Count + " · правило 3: " + s.DependsOnExperiments.Count + ").");
            sb.AppendLine("Долг блокаута — не ошибка, к релизу уровня должен быть 0.");
            sb.AppendLine();

            AppendViolationList(sb, "### 9.1 Правило 1 · файл под `Assets/_*` достижим из билда (кроме `_PolygonPrototype` / `_BlockoutKit`)",
                s.UnderscoreInBuild);
            AppendViolationList(sb, "### 9.2 Правило 2 · файл под git зависит от `_TK_Tools` / `_Local` (папка в `.gitignore`)",
                s.DependsOnTools);
            AppendViolationList(sb, "### 9.3 Правило 3 · продакшн (Art, Modules) зависит от `_Experiments`",
                s.DependsOnExperiments);

            sb.AppendLine("### 9.4 Долг блокаута (`_PolygonPrototype` / `_BlockoutKit`) по сценам и чанк-префабам");
            sb.AppendLine();
            sb.AppendLine("«Всего» — ассетов кита в рекурсивных зависимостях (префабы, модели, материалы, текстуры);");
            sb.AppendLine("«Геометрия» — из них префабы и модели. Показаны только строки с долгом > 0.");
            sb.AppendLine();
            List<DebtRow> debt = s.BlockoutDebt.Where(r => r.Total > 0).ToList();
            if (debt.Count == 0)
            {
                sb.AppendLine("Пусто.");
                sb.AppendLine();
                return;
            }
            sb.AppendLine("| Владелец | Всего | Геометрия |");
            sb.AppendLine("|---|---:|---:|");
            foreach (DebtRow row in debt)
                sb.AppendLine("| `" + row.Owner + "` | " + row.Total + " | " + row.Geometry + " |");
            sb.AppendLine();
        }

        private static void AppendViolationList(StringBuilder sb, string header, List<string> paths)
        {
            sb.AppendLine(header);
            sb.AppendLine();
            if (paths.Count == 0)
            {
                sb.AppendLine("Нарушений нет.");
                sb.AppendLine();
                return;
            }
            sb.AppendLine("Нарушений: " + paths.Count + ". Полный список — в `structure-violations.txt`.");
            sb.AppendLine();
            foreach (string path in paths.Take(30))
                sb.AppendLine("- `" + path + "`");
            if (paths.Count > 30)
                sb.AppendLine("- … ещё " + (paths.Count - 30));
            sb.AppendLine();
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
            string zone = ProductionRoots.First(r => path.StartsWith(r, StringComparison.Ordinal));
            string tail = path.Substring(zone.Length);
            int firstSlash = tail.IndexOf('/');
            if (firstSlash < 0)
                return zone + "<корень>";

            string first = tail.Substring(0, firstSlash);
            string rest = tail.Substring(firstSlash + 1);
            int secondSlash = rest.IndexOf('/');
            if (secondSlash < 0)
                return zone + first;

            return zone + first + "/" + rest.Substring(0, secondSlash);
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

            string[] textureExtensions = { ".png", ".tga", ".jpg", ".jpeg", ".tif", ".tiff", ".exr", ".psd", ".hdr" };
            AppendPrefixTable(sb, "### 7.1 Текстуры (`TEX_` по §6.3)", own, textureExtensions, "TEX_", "TEX_");
            AppendTextureSuffixTable(sb, own);
            AppendPrefixTable(sb, "### 7.3 Материалы (`MAT_` по §6.3; новый словарь `MT_` / `MTV_`)", own,
                new[] { ".mat" }, "MAT_", "MT_", "MTV_");
            AppendPrefixTable(sb, "### 7.4 Модели (`MSH_` по §6.3; новый словарь `SM_` / `SK_`)", own,
                new[] { ".fbx", ".obj", ".blend" }, "MSH_", "SM_", "SK_");
            AppendPrefabTable(sb, own);
            AppendPrefixTable(sb, "### 7.6 Шейдеры (`SHD_` / `SHG_` по §6.3)", own,
                new[] { ".shader", ".shadergraph" });
            AppendPrefixTable(sb, "### 7.7 Анимации (`ANIM_` / `AC_` по §6.3)", own,
                new[] { ".anim", ".controller" });
        }

        // oldPrefix / newPrefixes: vocabulary of §6.3 and the new dictionary, counted side by side
        // while both are in use. TEX_ is the same in both, so it is passed once as the old one.
        private static void AppendPrefixTable(
            StringBuilder sb, string header, List<Entry> own, string[] extensions,
            string oldPrefix = null, params string[] newPrefixes)
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
            if (oldPrefix != null)
            {
                bool sameVocabulary = newPrefixes.Length == 1 && newPrefixes[0] == oldPrefix;
                string oldLabel = sameVocabulary ? "с `" + oldPrefix + "`" : "старый словарь `" + oldPrefix + "`";
                string line = oldLabel + ": " + names.Count(n => n.StartsWith(oldPrefix, StringComparison.Ordinal));
                if (!sameVocabulary)
                {
                    foreach (string prefix in newPrefixes)
                        line += " · новый `" + prefix + "`: " + names.Count(n => n.StartsWith(prefix, StringComparison.Ordinal));
                }
                sb.AppendLine(line);
            }
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
            foreach (string prefix in NewPrefabPrefixes)
            {
                int count = names.Count(n => n.StartsWith(prefix, StringComparison.Ordinal));
                sb.AppendLine("| `" + prefix + "` (новый словарь) | " + count + " |");
            }
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
            List<Entry> outsideArtInBuild,
            StructureFindings structure,
            Dictionary<string, string> currentSnapshot)
        {
            Directory.CreateDirectory(ReportFolder);

            string reportPath = Path.Combine(ReportFolder, "audit-report.md");
            File.WriteAllText(reportPath, report, new UTF8Encoding(false));

            WriteList(Path.Combine(ReportFolder, "C-art-unreferenced.txt"), unreferenced);
            WriteList(Path.Combine(ReportFolder, "B-art-project-only.txt"), projectOnly);
            WriteList(Path.Combine(ReportFolder, "D-outside-art-in-build.txt"), outsideArtInBuild);
            WriteStructureList(Path.Combine(ReportFolder, "structure-violations.txt"), structure);
            File.WriteAllText(SnapshotPath,
                string.Join("\n", currentSnapshot.OrderBy(p => p.Key, StringComparer.Ordinal)
                    .Select(p => p.Key + " " + p.Value)) + "\n",
                new UTF8Encoding(false));

            Debug.Log("Build Content Audit: отчёт записан в " + reportPath +
                      "\nC (несвязано в Art): " + unreferenced.Count + " файлов, " + Mib(unreferenced.Sum(e => e.Bytes)) +
                      "\nB (только не-билдовые сцены): " + projectOnly.Count + " файлов, " + Mib(projectOnly.Sum(e => e.Bytes)) +
                      "\nD (вне Art, в билде): " + outsideArtInBuild.Count + " файлов, " + Mib(outsideArtInBuild.Sum(e => e.Bytes)) +
                      "\nСтруктура, ошибок: " + structure.ErrorCount + " (правило 1: " + structure.UnderscoreInBuild.Count +
                      ", правило 2: " + structure.DependsOnTools.Count + ", правило 3: " + structure.DependsOnExperiments.Count + ")" +
                      "\nДолг блокаута: " + structure.BlockoutDebt.Count(r => r.Total > 0) + " владельцев из " + structure.BlockoutDebt.Count);
        }

        private static void WriteStructureList(string path, StructureFindings s)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string p in s.UnderscoreInBuild)
                sb.AppendLine("rule1  " + p);
            foreach (string p in s.DependsOnTools)
                sb.AppendLine("rule2  " + p);
            foreach (string p in s.DependsOnExperiments)
                sb.AppendLine("rule3  " + p);
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
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
