using System;
using UnityEngine;

namespace SpiderController.Platform
{
    [Serializable]
    public class PlatformData
    {
        public float Weight;

        public GameObject[] AllPieceObjects;
        public SkinnedMeshRenderer MeshRenderer;
        public Collider Collider;
        public Collider BlinkDetectionCollider;
    }
}