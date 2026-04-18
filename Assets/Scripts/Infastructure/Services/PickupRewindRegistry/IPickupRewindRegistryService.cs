using System.Collections.Generic;
using PickupObjects.Rewind;

namespace Infastructure.Services.PickupRewindRegistry
{
    public interface IPickupRewindRegistryService
    {
        IReadOnlyList<IPickupRewindable> All { get; }

        void Register(IPickupRewindable rewindable);

        void Unregister(IPickupRewindable rewindable);

        void ClearAll();

        PickupObjectSnapshot[] GetSnapshotsReversed(IPickupRewindable rewindable);
        void PauseRecording();
        void ResumeRecording();
    }
}