using System.Collections.Generic;
using UnityEngine;

namespace _2.RotationPlaneManagement
{
    public class RotationPlaneController : MonoBehaviour
    {
        [SerializeField] private PlaneIndicator _planeIndicator;


        private RotationPlane _rotationPlane;
        private RotationPlane_2 _rotationPlane2;
        private RotationPlane_3 _rotationPlane3;

        private readonly Dictionary<int, RotationPlaneBase> _rotationPlaneDict =
            new Dictionary<int, RotationPlaneBase>();


        private void Awake()
        {
            _rotationPlane = GetComponent<RotationPlane>();
            _rotationPlane2 = GetComponent<RotationPlane_2>();
            _rotationPlane3 = GetComponent<RotationPlane_3>();

            _rotationPlane2.enabled = false;
            _rotationPlane3.enabled = false;

            _rotationPlaneDict.Add(1, _rotationPlane);
            _rotationPlaneDict.Add(2, _rotationPlane2);
            _rotationPlaneDict.Add(3, _rotationPlane3);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Select(1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Select(2);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                Select(3);
            }
        }

        private void Select(int index)
        {
            _planeIndicator.SelectMode(index);

            foreach (KeyValuePair<int, RotationPlaneBase> rotationPlane in _rotationPlaneDict)
            {
                if (rotationPlane.Key == index)
                    rotationPlane.Value.enabled = true;
                else
                    rotationPlane.Value.enabled = false;
            }
        }
    }
}