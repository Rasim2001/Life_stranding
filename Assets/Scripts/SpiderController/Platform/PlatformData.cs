using System;
using UnityEngine;

namespace SpiderController.Platform
{
    [Serializable]
    public class PlatformData
    {
        public GameObject[] AllPieceObjects;
        public Collider Collider;
        public SkinnedMeshRenderer MeshRenderer;
    }
}