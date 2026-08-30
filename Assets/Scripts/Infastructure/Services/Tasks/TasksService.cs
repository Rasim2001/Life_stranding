using System;
using System.Collections.Generic;
using Infastructure.Data;
using Infastructure.Services.CurrentLevel;
using Infastructure.Services.ProgressWatchers;
using Infastructure.Services.SaveLoadService;
using Infastructure.StaticData.Cheats;
using Infastructure.StaticData.StaticDataService;
using UI;
using UnityEngine;

namespace Infastructure.Services.Tasks
{
    public class TasksService : ITasksService, ISavedProgress
    {
        public event Action AllTasksCompleted;

        private readonly IProgressWatchersService _progressWatchersService;
        private readonly IStaticDataService _staticDataService;
        private readonly ICurrentLevelService _currentLevel;
        private CheatsStaticData Cheats => _staticDataService.CheatsStaticData;

        private List<TaskId> _taskIds = new List<TaskId>();

        public TasksService(IProgressWatchersService progressWatchersService, IStaticDataService staticDataService,
            ICurrentLevelService currentLevel)
        {
            _progressWatchersService = progressWatchersService;
            _staticDataService = staticDataService;
            _currentLevel = currentLevel;
        }

        public void Initialize()
        {
            _progressWatchersService.RegisterWatcher(this);
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
            return !_currentLevel.ShowsFirstEncounter ||
                   _taskIds.Contains(taskId) ||
                   Cheats.TasksPopupEnabled;
        }

        public void Dispose()
        {
            _taskIds.Clear();

            _progressWatchersService.Release(this);
        }
    }
}