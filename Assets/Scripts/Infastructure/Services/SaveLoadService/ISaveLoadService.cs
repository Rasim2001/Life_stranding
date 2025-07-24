using Infastructure.Data;

namespace Infastructure.Services.SaveLoadService
{
    public interface ISaveLoadService
    {
        void SaveProgress();
        PlayerProgress LoadPlayerProgress();
    }
}