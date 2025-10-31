using Infastructure.StaticData.XRay;
using UnityEngine;

namespace Infastructure.Services.XRay
{
    public interface IXRayService
    {
        void Add(XRayMarker xRayMarker);
        void Remove(XRayMarker xRayMarker);
        void Initialize(Transform xRayContainer, Transform hudTransform);
        void Show(XRayMarker xRayMarker);
        void Hide(XRayMarker xRayMarker);
    }
}