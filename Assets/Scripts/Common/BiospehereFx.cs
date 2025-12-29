using System;
using DG.Tweening;
using UnityEngine;

namespace Common
{
    public class BiosphereFx : MonoBehaviour
    {
        private const string Disolve = "_Disolve";

        private MeshRenderer _meshRenderer;

        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();

            Material temp = _meshRenderer.material;
            _meshRenderer.material = new Material(temp);
        }


        public void ShowFx(float valueNormalized)
        {
            float currentValue = 0.5f - (valueNormalized - valueNormalized / 2);

            _meshRenderer.material.DOFloat(currentValue, Disolve, 2f);
        }
    }
}