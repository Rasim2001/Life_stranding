using System.IO;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    /// <summary>
    /// Копирует хуки из версионируемого <c>.githooks/</c> в <c>.git/hooks/</c>.
    /// Не использует <c>core.hooksPath</c> — этот слот занят рабочими LFS-хуками
    /// (pre-push, post-checkout, post-commit, post-merge), и подмена всей папки
    /// их бы отключила. См. .scratch/plans/tingly-drifting-codd.md.
    /// </summary>
    [InitializeOnLoad]
    public static class GitHooksInstaller
    {
        private const string SessionStateKey = "SpiderRig.GitHooksInstaller.Ran";
        private const string HookMarker = "# spiderrig-hook";

        static GitHooksInstaller()
        {
            if (SessionState.GetBool(SessionStateKey, false))
                return;

            SessionState.SetBool(SessionStateKey, true);
            Install(forced: false);
        }

        [MenuItem("GD Tools/Setup Git Hooks")]
        public static void InstallForced()
        {
            Install(forced: true);
        }

        private static void Install(bool forced)
        {
            string repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string dotGit = Path.Combine(repoRoot, ".git");

            if (!Directory.Exists(dotGit))
            {
                Debug.LogWarning("GitHooksInstaller: .git не найден или не является папкой (worktree/submodule?) — пропущено: " + dotGit);
                return;
            }

            string sourceDir = Path.Combine(repoRoot, ".githooks");
            if (!Directory.Exists(sourceDir))
            {
                Debug.LogWarning("GitHooksInstaller: .githooks/ не найден — пропущено.");
                return;
            }

            string targetDir = Path.Combine(dotGit, "hooks");
            Directory.CreateDirectory(targetDir);

            foreach (string sourcePath in Directory.GetFiles(sourceDir))
            {
                InstallOne(sourcePath, targetDir, forced);
            }
        }

        private static void InstallOne(string sourcePath, string targetDir, bool forced)
        {
            string name = Path.GetFileName(sourcePath);
            string targetPath = Path.Combine(targetDir, name);

            string content = File.ReadAllText(sourcePath).Replace("\r\n", "\n");

            if (!File.Exists(targetPath))
            {
                File.WriteAllText(targetPath, content);
                Debug.Log("GitHooksInstaller: установлен хук " + name);
                return;
            }

            string existing = File.ReadAllText(targetPath).Replace("\r\n", "\n");
            if (existing == content)
            {
                if (forced)
                    Debug.Log("GitHooksInstaller: хук " + name + " уже актуален.");
                return;
            }

            if (existing.Contains(HookMarker))
            {
                File.WriteAllText(targetPath, content);
                Debug.Log("GitHooksInstaller: хук " + name + " обновлён.");
                return;
            }

            Debug.LogWarning("GitHooksInstaller: " + targetPath + " существует и не наш (нет маркера " + HookMarker + ") — не трогаем.");
        }
    }
}
