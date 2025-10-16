using UnityEngine;

namespace Infastructure.StaticData.Materials
{
    [CreateAssetMenu(fileName = "MaterialsData", menuName = "StaticData/MaterialsData")]
    public class MaterialsStaticData : ScriptableObject
    {
        public Material PlaneBlinkMaterial;
    }
}