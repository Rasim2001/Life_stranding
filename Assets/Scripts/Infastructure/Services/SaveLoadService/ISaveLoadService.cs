using Infastructure.Data;

namespace Infastructure.Services.SaveLoadService
{
    public interface ISaveLoadService
    {
        void SaveProgress();
        PlayerProgress LoadPlayerProgress();
        void ClearProgress();
        void SetNewProgress();
        void InitLoadingProgress();
        void SetContinueProgress();
    }
}