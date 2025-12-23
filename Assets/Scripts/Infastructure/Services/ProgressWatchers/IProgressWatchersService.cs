using System.Collections.Generic;
using Infastructure.Services.SaveLoadService;
using UnityEngine;

namespace Infastructure.Services.ProgressWatchers
{
    public interface IProgressWatchersService
    {
        List<ISavedProgressReader> ProgressReaders { get; }
        List<ISavedProgress> ProgressWriters { get; }
        void RegisterWatchers(GameObject gameObject);
        void Release(ISavedProgressReader progressReader);
        void Clear();
    }
}