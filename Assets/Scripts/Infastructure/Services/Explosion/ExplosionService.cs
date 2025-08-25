using Infastructure.Services.PlayerInput;
using SpiderController;
using UnityEngine;
using Zenject;

namespace Infastructure.Services.Explosion
{
    public class ExplosionService : ITickable
    {
        private readonly LayerMask _groundMask = 1 << LayerMask.NameToLayer("Default");
        private readonly LayerMask _spiderMask = 1 << LayerMask.NameToLayer("Spider");

        private readonly IInputService _inputService;
        private readonly Collider[] _results = new Collider[10];

        public ExplosionService(IInputService inputService) =>
            _inputService = inputService;

        public void Tick()
        {
            if (_inputService.LeftMousePressed)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out RaycastHit hit, 100f, _groundMask))
                {
                    Vector3 explosionPosition = hit.point;

                    Explode(explosionPosition, 20, 10);
                }
            }
        }

        private void Explode(Vector3 explosionPosition, int explosionForce, float radius)
        {
            int size = Physics.OverlapSphereNonAlloc(explosionPosition, radius, _results, _spiderMask);

            for (int i = 0; i < size; i++)
            {
                Spider spider = _results[i].GetComponent<Spider>();
                if (spider != null)
                    spider.SpiderImpactReceiver.ApplyExplosionForce(explosionPosition, explosionForce, radius);
            }
        }
    }
}