using System.Collections.Generic;
using PickupObjects.Rewind;
using Zenject;

namespace Infastructure.Services.PickupRewindRegistry
{
    public class PickupRewindRegistryService : IPickupRewindRegistryService, IFixedTickable
    {
        private readonly List<IPickupRewindable> _all = new();
        private readonly Dictionary<IPickupRewindable, PickupObjectRecorder> _recorders = new();

        private bool _isRecording = true;

        public IReadOnlyList<IPickupRewindable> All => _all;

        public void Register(IPickupRewindable rewindable)
        {
            _all.Add(rewindable);
            _recorders[rewindable] = new PickupObjectRecorder();
        }

        public void Unregister(IPickupRewindable rewindable)
        {
            _all.Remove(rewindable);
            _recorders.Remove(rewindable);
        }

        public void FixedTick() =>
            RecordAll();

        public void PauseRecording() =>
            _isRecording = false;

        public void ResumeRecording() =>
            _isRecording = true;
        
        public void ClearAll()
        {
            foreach (PickupObjectRecorder recorder in _recorders.Values)
                recorder.Clear();
        }

        public PickupObjectSnapshot[] GetSnapshotsReversed(IPickupRewindable rewindable)
        {
            return _recorders.TryGetValue(rewindable, out PickupObjectRecorder recorder)
                ? recorder.GetSnapshotsReversed()
                : System.Array.Empty<PickupObjectSnapshot>();
        }

        private void RecordAll()
        {
            if (!_isRecording)
                return;

            foreach (IPickupRewindable rewindable in _all)
                _recorders[rewindable].Record(rewindable);
        }
    }
}