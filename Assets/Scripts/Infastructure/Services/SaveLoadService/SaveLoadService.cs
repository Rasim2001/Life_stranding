using System.IO;
using Infastructure.Data;
using Infastructure.Services.PlayerProgressService;
using UnityEngine;

namespace Infastructure.Services.SaveLoadService
{
    public class SaveLoadService : ISaveLoadService
    {
        private const string FileName = "progress.json";
        private readonly IPersistentProgressService _progressService;

        private string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        public SaveLoadService(IPersistentProgressService progressService) =>
            _progressService = progressService;

        public void SaveProgress()
        {
            string json = JsonUtility.ToJson(_progressService.PlayerProgress, prettyPrint: false);
            File.WriteAllText(SavePath, json);
        }

        public PlayerProgress LoadPlayerProgress()
        {
            if (!File.Exists(SavePath))
                return new PlayerProgress();

            string json = File.ReadAllText(SavePath);
            if (string.IsNullOrEmpty(json))
                return new PlayerProgress();

            return JsonUtility.FromJson<PlayerProgress>(json) ?? new PlayerProgress();
        }

        public void ClearProgress() =>
            File.Delete(SavePath);
    }
}