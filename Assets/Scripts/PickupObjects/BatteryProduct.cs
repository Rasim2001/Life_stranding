using Infastructure.Services.XRay;
using Infastructure.StaticData.XRay;
using Zenject;

namespace PickupObjects
{
    public class BatteryProduct : PickupObjectBase
    {
        private XRayMarker _xRayMarker;
        private IXRayService _xRayService;

        [Inject]
        public void Construct(IXRayService xRayService) =>
            _xRayService = xRayService;

        private void Start() =>
            _xRayMarker = GetComponent<XRayMarker>();

        protected override void StartSimulatePhysics()
        {
            base.StartSimulatePhysics();

            _xRayService.Add(_xRayMarker);
        }

        public override void StopSimulatePhysics()
        {
            base.StopSimulatePhysics();

            _xRayService.Remove(_xRayMarker);
        }
    }
}