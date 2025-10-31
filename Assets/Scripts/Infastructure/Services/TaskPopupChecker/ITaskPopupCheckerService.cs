using UI;

namespace Infastructure.Services.TaskPopupChecker
{
    public interface ITaskPopupCheckerService
    {
        void AddTask(TaskId taskId);
        bool IsWasOpened(TaskId taskId);
    }
}