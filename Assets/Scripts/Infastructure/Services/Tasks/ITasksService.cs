using System;
using UI;

namespace Infastructure.Services.Tasks
{
    public interface ITasksService
    {
        void AddTask(TaskId taskId);
        bool IsWasOpened(TaskId taskId);
        event Action AllTasksCompleted;
        void Initialize();
        void Dispose();
    }
}