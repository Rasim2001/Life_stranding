using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Infastructure.Services.VolumeManagement
{
    public class VolumeService : IVolumeService
    {
        private readonly ColorAdjustments _colorAdjustments;

        public VolumeService(Volume volume)
        {
            if (volume.profile.TryGet<ColorAdjustments>(out var colorAdjustments))
                _colorAdjustments = colorAdjustments;
        }

        public void SetSaturation(float value) =>
            _colorAdjustments.saturation.value = value;
    }
}