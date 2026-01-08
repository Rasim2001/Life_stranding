using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Infastructure.Common;
using Infastructure.StaticData.Task;
using Localization;
using UI;
using UnityEngine;

namespace GoogleImporter.Parsers
{
    public class TaskPopupLocalizationParser : IGoogleSheetParser
    {
        private readonly TasksStaticData _tasksStaticData =
            Resources.Load<TasksStaticData>(AssetsPath.TasksStaticDataPath);

        private TaskId _currentTaskId;
        private string _currentTaskType;

        public async UniTask Parse(string header, string value)
        {
            switch (header)
            {
                case "ID":
                    if (!string.IsNullOrEmpty(value))
                        _currentTaskId = Enum.Parse<TaskId>(value);
                    break;
                case "RU":
                    Translate(header, value);
                    break;
                case "EN":
                    Translate(header, value);
                    break;
                case "TaskType":
                    _currentTaskType = value;
                    break;
            }
        }

        private void Translate(string header, string value)
        {
            LanguageId currentLanguage = Enum.Parse<LanguageId>(header);

            switch (_currentTaskType)
            {
                case "Name":
                    GetTaskData(_currentTaskId).TaskName.Set(currentLanguage, value);
                    break;
                case "Description":
                    GetTaskData(_currentTaskId).TaskDescription.Set(currentLanguage, value);
                    break;
            }
        }

        private TaskData GetTaskData(TaskId id) =>
            _tasksStaticData.TaskDatas.GetValueOrDefault(id);
    }
}