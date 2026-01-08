using System;
using System.Collections.Generic;
using Localization;
using Sirenix.OdinInspector;
using UI;
using UnityEngine;

namespace Infastructure.StaticData.Task
{
    [CreateAssetMenu(fileName = "TasksPopupData", menuName = "StaticData/TasksPopupData")]
    public class TasksStaticData : SerializedScriptableObject
    {
        public Dictionary<TaskId, TaskData> TaskDatas = new Dictionary<TaskId, TaskData>();
    }

    [Serializable]
    public class TaskData
    {
        public LocalizationText TaskName = new();
        public LocalizationText TaskDescription = new();
        public Sprite ScreenIcon;
    }
}