using Cysharp.Threading.Tasks;
using Infastructure.Common;
using Infastructure.StaticData.Task;
using UnityEngine;

namespace GoogleImporter.Parsers
{
    public class LocalizationDataParser : IGoogleSheetParser
    {
        private readonly TasksStaticData _tasksStaticData =
            Resources.Load<TasksStaticData>(AssetsPath.TasksStaticDataPath);

        public async UniTask Parse(string header, string token)
        {
            Debug.Log($"Header : {header} and Token : {token}");

            /*switch (header)
            {
                default:
                    Debug.Log($"Нет такого Header : {header}");
                    break;
            }*/
        }
    }
}