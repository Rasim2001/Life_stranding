namespace PickupObjects.Rewind
{
    public interface IPickupRewindable
    {
        PickupObjectSnapshot Capture();

        void ApplyLerp(PickupObjectSnapshot from, PickupObjectSnapshot to, float t);

        void ApplyFinalSnapshot(PickupObjectSnapshot snapshot);
        void FreezeForRewind();
    }
}