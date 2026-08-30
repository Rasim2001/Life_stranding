using Infastructure.Services.ProgressWatchers;
using Infastructure.Services.SaveLoadService;
using UnityEngine;
using Zenject;

namespace Common
{
    [RequireComponent(typeof(MarkerUniqueId))]
    public class SceneProgressActor : MonoBehaviour
    {
        private IProgressWatchersService _watchers;
        private ISavedProgressReader[] _readers;

        [Inject]
        public void Construct(IProgressWatchersService watchers) => _watchers = watchers;

        private void Awake() => _readers = GetComponents<ISavedProgressReader>();

        private void Start()
        {
            foreach (ISavedProgressReader reader in _readers)
                _watchers.RegisterWatcher(reader);
        }

        private void OnDestroy()
        {
            if (_watchers == null)
                return;

            foreach (ISavedProgressReader reader in _readers)
                _watchers.Release(reader);
        }
    }
}
