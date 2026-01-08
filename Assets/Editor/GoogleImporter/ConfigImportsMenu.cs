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

        private const string TASKPOPUP_LOCALIZATION_DATA = "TaskPopup_Localization";
        private const string PRODUCTPOPUP_LOCALIZATION_DATA = "ProductPopup_Localization";
        private const string STATICTEXTS_LOCALIZATION_DATA = "StaticTexts_Localization";

        [MenuItem("GoogleSheet/Import Remote Settings")]
        private static async void LoadRemoteItemsSettings()
        {
            IImporter sheetImporter = new GoogleSheetsImporter(CREDENTIALS_PATH, SPREADSHEET_PATH);

            await LoadSettings(sheetImporter);
        }


        private static async Task LoadSettings(IImporter excelImporter)
        {
            IGoogleSheetParser taskPopupLocalizationParser = new TaskPopupLocalizationParser();
            IGoogleSheetParser _productDescriptionLocalizationParser = new ProductDescriptionLocalizationParser();
            IGoogleSheetParser _windowStaticLocalizationParser = new WindowStaticLocalizationParser();

            await excelImporter.DownloadAndParseSheet(TASKPOPUP_LOCALIZATION_DATA, taskPopupLocalizationParser);
            await excelImporter.DownloadAndParseSheet(PRODUCTPOPUP_LOCALIZATION_DATA,
                _productDescriptionLocalizationParser);
            await excelImporter.DownloadAndParseSheet(STATICTEXTS_LOCALIZATION_DATA, _windowStaticLocalizationParser);

            foreach (ScriptableObject asset in Resources.LoadAll<ScriptableObject>("StaticData"))
                EditorUtility.SetDirty(asset);

            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();

            Debug.Log("Все прошло успешно");
        }
    }
}