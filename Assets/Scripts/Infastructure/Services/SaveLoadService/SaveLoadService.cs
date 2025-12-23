using System.IO;
using Infastructure.Data;
using Infastructure.Services.PlayerProgressService;
using Infastructure.Services.ProgressWatchers;
using UnityEngine;

namespace Infastructure.Services.SaveLoadService
{
    public class SaveLoadService : ISaveLoadService
    {
        private const string FileName = "progress.json";

        private readonly IPersistentProgressService _progressService;
        private readonly IProgressWatchersService _progressWatchersService;

        private string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        public SaveLoadService(IPersistentProgressService progressService,
            IProgressWatchersService progressWatchersService)
        {
            _progressService = progressService;
            _progressWatchersService = progressWatchersService;
        }

        public void SaveProgress()
        {
            foreach (ISavedProgress writer in _progressWatchersService.ProgressWriters)
                writer.UpdateProgress(_progressService.PlayerProgress);

            string json = JsonUtility.ToJson(_progressService.PlayerProgress, prettyPrint: true);
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