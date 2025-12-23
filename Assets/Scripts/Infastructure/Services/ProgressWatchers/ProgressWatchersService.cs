using System.Collections.Generic;
using System.Linq;
using Infastructure.Services.SaveLoadService;
using UnityEngine;

namespace Infastructure.Services.ProgressWatchers
{
    public class ProgressWatchersService : IProgressWatchersService
    {
        public List<ISavedProgressReader> ProgressReaders { get; } = new List<ISavedProgressReader>();
        public List<ISavedProgress> ProgressWriters { get; } = new List<ISavedProgress>();

        public void RegisterWatchers(GameObject gameObject)
        {
            foreach (ISavedProgressReader progressReader in gameObject.GetComponentsInChildren<ISavedProgressReader>())
            {
                if (progressReader is ISavedProgress progressWriter)
                {
                    if (!ProgressWriters.Contains(progressWriter))
                        ProgressWriters.Add(progressWriter);
                }

                if (!ProgressReaders.Contains(progressReader))
                    ProgressReaders.Add(progressReader);
            }
        }

        public void Release(ISavedProgressReader progressReader)
        {
            if (progressReader is ISavedProgress progressWriter && ProgressWriters.Contains(progressReader))
                ProgressWriters.Remove(progressWriter);

            if (ProgressReaders.Contains(progressReader))
                ProgressReaders.Remove(progressReader);
        }

        public void Clear()
        {
            ProgressReaders.Clear();
            ProgressWriters.Clear();
        }
    }
}