using System;

namespace SpiderController.SpiderMove
{
    [Serializable]
    public struct LegDataStruct
    {
        public LegTarget Leg;
        public LegRaycast Raycast;
    }
}