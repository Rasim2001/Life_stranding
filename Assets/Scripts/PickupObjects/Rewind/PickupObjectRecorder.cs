using System.Collections.Generic;
using UnityEngine;

namespace PickupObjects.Rewind
{
    public class PickupObjectRecorder
    {
        private const float RecordDuration = 5f;

        private readonly Queue<PickupObjectSnapshot> _snapshots = new();

        public bool HasSnapshots => _snapshots.Count > 0;

        public void Record(IPickupRewindable source)
        {
            PickupObjectSnapshot snapshot = source.Capture();
            _snapshots.Enqueue(snapshot);

            while (_snapshots.Count > 0 && Time.fixedTime - _snapshots.Peek().Time > RecordDuration)
                _snapshots.Dequeue();
        }

        public PickupObjectSnapshot[] GetSnapshotsReversed()
        {
            PickupObjectSnapshot[] arr = _snapshots.ToArray();
            System.Array.Reverse(arr);
            return arr;
        }

        public void Clear() =>
            _snapshots.Clear();
    }
}
