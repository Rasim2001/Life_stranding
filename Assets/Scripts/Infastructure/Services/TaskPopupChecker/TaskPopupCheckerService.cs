using System;
using System.Collections.Generic;
using UI;
using WaterSystem;

namespace Infastructure.Services.TaskPopupChecker
{
    public class TaskPopupCheckerService : ITaskPopupCheckerService, IDisposable
    {
        public event Action AllTasksCompleted;

        private readonly List<TaskId> _taskIds = new List<TaskId>();


        public void AddTask(TaskId taskId)
        {
            if (_taskIds.Contains(taskId))
                return;

            if (taskId == TaskId.LastTask)
                AllTasksCompleted?.Invoke();

            _taskIds.Add(taskId);
        }

        public bool IsWasOpened(TaskId taskId) =>
            _taskIds.Contains(taskId);

        public void Dispose() =>
            _taskIds.Clear();
    }
}