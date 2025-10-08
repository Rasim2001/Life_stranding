using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace SceneNotes.Editor
{
    public static class Utilities
    {
        public const string DateFormat = "yyyyMMddHHmmzzz";
        
        public static string AssetToGuid<T>(T asset) where T : Object
        {
            return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetOrScenePath(asset));
        }
        
        public static string SceneToGuid(Scene scene)
        {
            return AssetDatabase.AssetPathToGUID(scene.path);
        }

        public static T GuidToAsset<T>(string guid) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
        }
        
        public static string[] GetAssetPathsInFolder(string folderAbsPath, string searchPattern)
        {
            string[] paths = Directory.GetFiles(folderAbsPath, searchPattern, SearchOption.TopDirectoryOnly);
            return paths;
        }
        
        public static void MakePathsProjectRelative(string[] absPaths)
        {
            if (absPaths.Length > 0)
            {
                int assetPathIndex = absPaths[0].IndexOf("Assets", StringComparison.Ordinal);
                // Make them project-relative
                for (int i = 0; i < absPaths.Length; i++)
                {
                    absPaths[i] = absPaths[i].Substring(assetPathIndex);
                }
            }
        }

        public static string MakePathAbsolute(string relativePath)
        {
            int assetPathIndex = Application.dataPath.LastIndexOf("Assets", StringComparison.Ordinal);
            string dataPath = Application.dataPath.Remove(assetPathIndex);
            return $"{dataPath}{relativePath}";
        }
        
        public static void DestroyAllFoldersInFolder(string folderAbsPath)
        {
            string[] paths = Directory.GetDirectories(folderAbsPath);
            MakePathsProjectRelative(paths);
            AssetDatabase.DeleteAssets(paths, new List<string>());
        }
        
        public static void DestroyAllAssetsInFolder(string folderAbsPath, string assetPattern)
        {
            string[] paths = GetAssetPathsInFolder(folderAbsPath, assetPattern);
            MakePathsProjectRelative(paths);
            foreach (string soPath in paths)
            {
                AssetDatabase.DeleteAsset(soPath);
            }
        }
        
        public static string GetAbsNotesFolderPath() => Path.Combine(Application.dataPath, GetSafeUnityFolder());
        public static string GetRelNotesFolderPath() => Path.Combine("Assets", GetSafeUnityFolder());
        
        /// <summary>Ensures that the Unity folder hasn't been set to empty by setting it to the default value.</summary>
        internal static string GetSafeUnityFolder()
        {
            if (string.IsNullOrEmpty(SceneNotesSettings.unityFolder.value)) SceneNotesSettings.unityFolder.Reset();
            return SceneNotesSettings.unityFolder.value;
        }
        
        public static string DateToString(DateTime date) => date.ToString("yyyyMMddHHmmzzz");

        private static DateTime StringToDate(string compactString)
        {
            try
            {
                return DateTime.ParseExact(compactString, DateFormat, null);
            }
            catch (Exception)
            {
                return DateTime.Now;
            }
        }

        public static string HumanReadableDate_Short(string compactString)
        {
            string year = compactString.StartsWith(DateTime.Now.Year.ToString()) ? "" :  " yyyy";
            return StringToDate(compactString).ToString($"dd MMM{year}, HH:mm");
        }

        public static string HumanReadableDate(string compactString) => StringToDate(compactString).ToString("dd MMM yyyy - HH:mm (zzz)");

        public static string Hyphenate(string source)
        {
            string pattern = @"(^(PRN|AUX|NUL|CON|COM[1-9]|LPT[1-9]|(\.+)$)(\..*)?$)|(([\x00-\x1f\\?*:"";‌​|/<>])+)|([\. ]+)";
            return Regex.Replace(source, pattern, "_");
        }

        public static string FourNumbers()
        {
            return $"{Random.Range(0, 10)}{Random.Range(0, 10)}{Random.Range(0, 10)}{Random.Range(0, 10)}";
        }
    }
}