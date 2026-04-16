using System.Collections.Generic;
using SpiderController.Platform;
using SpiderController.SpiderMove;
using SpiderController.UI.Health;
using UnityEngine;

namespace SpiderController.StateMachine.States.Rewind
{
    public class SpiderRewindRecorder
    {
        private const float RecordDuration = 5f;

        private readonly SpiderStateContext _stateContext;
        private readonly PlatformSelector _platformSelector;
        private readonly Queue<SpiderSnapshot> _snapshots = new();

        private Rigidbody Rigidbody => _stateContext.Rigidbody;
        private LegDataStruct[] Legs => _stateContext.Legs;
        private SpiderHealth SpiderHealth => _stateContext.SpiderUI.SpiderHealth;
        private StateMachineData Data => _stateContext.Data;
        private BodyOrientation BodyOrientation => _stateContext.BodyOrientation;
        public bool HasSnapshots => _snapshots.Count > 0;

        public SpiderRewindRecorder(SpiderStateContext stateContext, PlatformSelector platformSelector)
        {
            _stateContext = stateContext;
            _platformSelector = platformSelector;
        }

        public SpiderSnapshot[] GetSnapshotsReversed()
        {
            SpiderSnapshot[] arr = _snapshots.ToArray();
            System.Array.Reverse(arr);
            return arr;
        }

        public void FixedUpdate()
        {
            if (!Data.CanRecordFootprints)
                return;

            Record();
        }

        public void Clear() =>
            _snapshots.Clear();

        private void Record()
        {
            Vector3[] legPositions = new Vector3[Legs.Length];
            for (int i = 0; i < Legs.Length; i++)
                legPositions[i] = Legs[i].Leg.Position;

            SpiderSnapshot snapshot = new SpiderSnapshot
            {
                Time = Time.fixedTime,
                Position = Rigidbody.position,
                Rotation = Rigidbody.rotation,
                LegPositions = legPositions,
                CurrentEnergy = Data.CurrentEnergyFillAmount,
                CurrentHp = SpiderHealth.CurrentHP,
                LegsRootRotation = BodyOrientation.LegsRootBoneRotation,
                HeadRootRotation = BodyOrientation.HeadRootRotation,
                RaycastRigRotation = BodyOrientation.RaycastRigRotation,
                PlaneRotation = _stateContext.RotationPlaneTransform.localRotation,
                PlatformIndex = _platformSelector.CurrentIndex
            };

            _snapshots.Enqueue(snapshot);

            while (_snapshots.Count > 0 && Time.fixedTime - _snapshots.Peek().Time > RecordDuration)
                _snapshots.Dequeue();
        }
    }
}