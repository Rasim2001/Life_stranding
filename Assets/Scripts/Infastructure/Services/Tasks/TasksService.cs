using System;
using System.Collections.Generic;
using Infastructure.Data;
using Infastructure.Services.ProgressWatchers;
using Infastructure.Services.SaveLoadService;
using UI;

namespace Infastructure.Services.Tasks
{
    public class TasksService : ITasksService, ISavedProgress
    {
        public event Action AllTasksCompleted;

        private readonly IProgressWatchersService _progressWatchersService;

        private List<TaskId> _taskIds = new List<TaskId>();
        private bool _isCheating;

        public TasksService(IProgressWatchersService progressWatchersService) =>
            _progressWatchersService = progressWatchersService;

        public void Initialize()
        {
            _progressWatchersService.RegisterWatcher(this);

            //_isCheating = true;
        }

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

        public bool IsWasOpened(TaskId taskId)
        {
            return _taskIds.Contains(taskId) || _isCheating;
        }

        public void Dispose()
        {
            _taskIds.Clear();

            _progressWatchersService.Release(this);
        }
    }
}