using UnityEngine;

namespace _2
{
    public class MoveFollower : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _speed;

        private void LateUpdate()
        {
            transform.position = _target.transform.position;
            transform.rotation = _target.transform.rotation;
        }
    }
}