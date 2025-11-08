using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UI;
using UnityEngine;
using UnityEngine.Video;

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
        public string TaskName;
        public string TaskDescription;
        public Sprite ScreenIcon;
    }
}