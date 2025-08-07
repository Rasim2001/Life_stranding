using UnityEngine;

namespace Common.Obstacles
{
    public class ObstacleMove : MonoBehaviour
    {
        public Vector3 movementDirection = Vector3.forward;

        public float speed = 2f;
        public float moveDistance = 3f;

        public bool useLocalSpace = false;

        private Vector3 _startPosition;
        private bool _movingForward = true;

        void Start() =>
            _startPosition = transform.position;

        void Update()
        {
            Vector3 direction = movementDirection.normalized;

            if (!_movingForward)
                direction = -direction;

            transform.Translate(direction * (speed * Time.deltaTime), useLocalSpace ? Space.Self : Space.World);

            float distanceMoved = Vector3.Distance(_startPosition, transform.position);
            if (distanceMoved >= moveDistance)
                _movingForward = !_movingForward;
        }
    }
}