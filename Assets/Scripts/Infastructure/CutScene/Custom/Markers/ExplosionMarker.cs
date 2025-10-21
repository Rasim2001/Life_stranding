using System;

namespace Infastructure.CutScene.Custom.Markers
{
    [Serializable]
    public class ExplosionMarker : TransformMarker
    {
        public float Force = 10f;
        public float Radius = 10f;
    }
}