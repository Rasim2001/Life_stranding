using Infastructure.StaticData.Task;
using UI.MVVM.Base;

namespace UI.MVVM.View.TaskPopup
{
    public class TaskPopupViewModel : WindowViewModel
    {
        public override string Id => "TaskPopup";

        public TaskData TaskData;

        public TaskPopupViewModel(TaskData taskData) =>
            TaskData = taskData;
    }
}