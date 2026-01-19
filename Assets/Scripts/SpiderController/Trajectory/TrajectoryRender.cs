using Infastructure.Common.Trajectory;
using UnityEngine;
using Zenject;

namespace SpiderController.Trajectory
{
    public class TrajectoryRender : MonoBehaviour
    {
        private const float TimeStep = 0.1f;

        [SerializeField] private LayerMask _collisionMask;

        private LineRenderer _lineRenderer;
        private Vector3[] _points;

        private ITrajectoryEndPointDisplayer _endPointDisplayer;


        [Inject]
        public void Construct(ITrajectoryEndPointDisplayer endPointDisplayer) =>
            _endPointDisplayer = endPointDisplayer;

        private void Awake() =>
            _lineRenderer = GetComponent<LineRenderer>();

        private void Start() =>
            _points = new Vector3[100];

        public void Show()
        {
            _endPointDisplayer.Show();

            _lineRenderer.enabled = true;
        }

        public void Hide()
        {
            _endPointDisplayer.Hide();

            _lineRenderer.enabled = false;
        }

        public void FollowTrajectory(Vector3 origin, Vector3 directionSpeed)
        {
            int pointCount = 0;

            for (int i = 0; i < _points.Length - 1; i++)
            {
                float time = i * TimeStep;
                float nextTime = (i + 1) * TimeStep;

                pointCount++;

                _points[i] = origin + directionSpeed * time + Physics.gravity * (time * time) / 2f;

                Vector3 nextPosition =
                    origin + directionSpeed * nextTime + Physics.gravity * (nextTime * nextTime) / 2f;

                if (Physics.Linecast(_points[i], nextPosition, out RaycastHit hit, _collisionMask))
                {
                    _points[i + 1] = nextPosition;

                    ApplyLine(pointCount + 1);

                    if (hit.collider != null)
                        _endPointDisplayer.Apply(hit);

                    return;
                }

                ApplyLine(pointCount);
            }
        }


        private void ApplyLine(int pointCount)
        {
            _lineRenderer.positionCount = pointCount;

            for (int i = 0; i < pointCount; i++)
                _lineRenderer.SetPosition(i, _points[i]);
        }
    }
}