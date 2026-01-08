using System;

namespace Infastructure.CutScenes.Custom.Markers
{
    [Serializable]
    public class ExplosionMarker : TransformMarker
    {
        public float Force = 10f;
        public float Radius = 10f;
    }
}