using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using UnityEngine;

namespace GoogleImporter
{
    public class GoogleSheetsImporter : IImporter
    {
        private readonly SheetsService _service;
        private readonly string _spreadsheetId;

        public GoogleSheetsImporter(string credentialsPath, string spreadsheetId)
        {
            _spreadsheetId = spreadsheetId;

            GoogleCredential credential;
            using (var stream =
                   new System.IO.FileStream(credentialsPath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream).CreateScoped(SheetsService.Scope.Spreadsheets);
            }

            _service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential
            });
        }

        public async Task DownloadAndParseSheet(string sheetName, IGoogleSheetParser googleSheetParser)
        {
            List<string> headers = new();

            Debug.Log($"Starting downloading sheet (${sheetName})...");

            string range = $"{sheetName}!A1:Z";
            var request = _service.Spreadsheets.Values.Get(_spreadsheetId, range);

            ValueRange response;
            try
            {
                response = await request.ExecuteAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error retrieving Google Sheets data: {e.Message}");
                return;
            }

            if (response != null && response.Values != null)
            {
                IList<IList<object>> tableArray = response.Values;
                //Debug.Log($"Sheet downloaded successfully: {sheetName}. Parsing started.");

                IList<object> firstRow = tableArray[0];
                foreach (var cell in firstRow)
                {
                    if (!string.IsNullOrEmpty(cell.ToString()))
                    {
                        headers.Add(cell.ToString());
                    }
                }

                var rowsCount = tableArray.Count;

                for (var i = 1; i < rowsCount; i++)
                {
                    var row = tableArray[i];
                    var rowLength = row.Count;

                    for (var j = 0; j < rowLength; j++)
                    {
                        string header = headers[j];
                        if (string.IsNullOrEmpty(header))
                            continue;

                        object cell = row[j];
                        await googleSheetParser.Parse(header, cell.ToString());
                    }
                }

                Debug.Log($"Sheet : {sheetName} parsed successfully.");
            }
            else
            {
                Debug.LogWarning("No data found in Google Sheets.");
            }
        }

        public async Task AppendRow(string sheetName, IList<object> row)
        {
            string range = $"{sheetName}!A1";
            ValueRange valueRange = new ValueRange
            {
                Values = new List<IList<object>> { row }
            };

            SpreadsheetsResource.ValuesResource.AppendRequest request =
                _service.Spreadsheets.Values.Append(valueRange, _spreadsheetId, range);
            request.ValueInputOption =
                SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;

            try
            {
                await request.ExecuteAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error appending row to {sheetName}: {e.Message}");
            }
        }


        public async Task UpdateRange(string sheetName, string range, IList<IList<object>> values)
        {
            ValueRange valueRange = new ValueRange
            {
                Values = values
            };


            SpreadsheetsResource.ValuesResource.UpdateRequest request =
                _service.Spreadsheets.Values.Update(valueRange, _spreadsheetId, $"{sheetName}!{range}");
            request.ValueInputOption =
                SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;

            try
            {
                await request.ExecuteAsync();
                Debug.Log($"Range {range} updated in {sheetName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error updating range {range} in {sheetName}: {e.Message}");
            }
        }
    }
}