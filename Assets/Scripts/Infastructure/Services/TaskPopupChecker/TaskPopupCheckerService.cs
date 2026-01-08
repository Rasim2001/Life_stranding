using System;
using System.Collections.Generic;
using Infastructure.Data;
using Infastructure.Services.ProgressWatchers;
using Infastructure.Services.SaveLoadService;
using UI;
using UnityEngine;
using WaterSystem;
using Zenject;

namespace Infastructure.Services.TaskPopupChecker
{
    public class TaskPopupCheckerService : ITaskPopupCheckerService, ISavedProgress
    {
        public event Action AllTasksCompleted;

        private readonly IProgressWatchersService _progressWatchersService;

        private List<TaskId> _taskIds = new List<TaskId>();

        public TaskPopupCheckerService(IProgressWatchersService progressWatchersService) =>
            _progressWatchersService = progressWatchersService;

        public void Initialize() =>
            _progressWatchersService.RegisterWatcher(this);

        public void LoadProgress(PlayerProgress progress) =>
            _taskIds = new List<TaskId>(progress.TaskPopupData.CompletedTaskIds);

        public void UpdateProgress(PlayerProgress progress) =>
            progress.TaskPopupData.CompletedTaskIds = new List<TaskId>(_taskIds);

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

        public void Dispose()
        {
            _taskIds.Clear();

            _progressWatchersService.Release(this);
        }
    }
}