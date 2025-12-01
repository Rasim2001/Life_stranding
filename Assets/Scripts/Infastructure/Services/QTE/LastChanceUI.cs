using UnityEngine;

namespace QTE
{
    public class LastChanceUI : MonoBehaviour
    {
        [SerializeField] private Transform _container;
        [SerializeField] private Transform _circleBG;
        [SerializeField] private Transform _icon;

        public Transform FlowerTransform;

        /*public void Update()
        {
            if (FlowerTransform == null)
                return;

            _container.position = FlowerTransform.position;
        }*/

        public void StartAnimation()
        {
        }
    }
}