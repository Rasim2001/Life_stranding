using UnityEngine;
using UnityEngine.Rendering;

namespace Infastructure.StaticData.VolumeProfiles
{
    [CreateAssetMenu(fileName = "VolumeProfiles", menuName = "StaticData/VolumeProfiles")]
    public class VolumeProfilesStaticData : ScriptableObject
    {
        public VolumeProfile DefaultProfile;
        public VolumeProfile StartGameFirstCameraProfile;
    }
}