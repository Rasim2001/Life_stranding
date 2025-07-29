using UnityEngine;

namespace CameraFollow
{
    public class CameraSystem : MonoBehaviour
    {
        [SerializeField] private CameraFollower _cameraFollower;

        public void Initialize(Transform spiderTransform)
        {
            _cameraFollower.SetTarget(spiderTransform);
        }
    }
}