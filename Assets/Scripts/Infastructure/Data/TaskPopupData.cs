using System;
using System.Collections.Generic;
using UI;

namespace Infastructure.Data
{
    [Serializable]
    public class TaskPopupData
    {
        public List<TaskId> CompletedTaskIds = new List<TaskId>();
    }
}