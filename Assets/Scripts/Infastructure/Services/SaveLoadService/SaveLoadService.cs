using Infastructure.Data;
using Infastructure.Services.PlayerProgressService;
using UnityEngine;

namespace Infastructure.Services.SaveLoadService
{
    public class SaveLoadService : ISaveLoadService
    {
        private const string ProgressKey = "Progress";
        private readonly IPersistentProgressService _progressService;

        public SaveLoadService(IPersistentProgressService progressService) =>
            _progressService = progressService;

        public void SaveProgress() =>
            PlayerPrefs.SetString(_progressService.PlayerProgress.PlayerName, _progressService.PlayerProgress.ToJson());

        public PlayerProgress LoadPlayerProgress() =>
            PlayerPrefs.GetString(ProgressKey)?.ToDeserialized<PlayerProgress>();
    }
}