using System.Threading.Tasks;
using GoogleImporter;
using GoogleImporter.Parsers;
using UnityEditor;
using UnityEngine;

namespace Editor.GoogleImporter
{
    public static class ConfigImportsMenu
    {
        private const string SPREADSHEET_PATH = "1Ue14hDMO9B-Bkuw-3RdXX5rkul7auqvCcgr06rxMLrs";
        private const string CREDENTIALS_PATH = "spiderrigsheet-228e740064b3.json";

        private const string LOCALIZATION_DATA = "Localization";

        [MenuItem("GoogleSheet/Import Remote Settings")]
        private static async void LoadRemoteItemsSettings()
        {
            IImporter sheetImporter = new GoogleSheetsImporter(CREDENTIALS_PATH, SPREADSHEET_PATH);

            await LoadSettings(sheetImporter);
        }


        private static async Task LoadSettings(IImporter excelImporter)
        {
            IGoogleSheetParser localizationParser = new LocalizationDataParser();
            await excelImporter.DownloadAndParseSheet(LOCALIZATION_DATA, localizationParser);

            foreach (ScriptableObject asset in Resources.LoadAll<ScriptableObject>("StaticData"))
                EditorUtility.SetDirty(asset);

            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();

            Debug.Log("Все прошло успешно");
        }
    }
}