using UnityEngine;

namespace Common.Extensions
{
    public enum CollisionLayer
    {
        Default = 0,
    }

    public static class CollisionExtensions
    {
        public static bool Matches(this Collider collider, LayerMask layerMask) =>
            ((1 << collider.gameObject.layer) & layerMask) != 0;

        public static bool Matches(this Collision collision, LayerMask layerMask) =>
            ((1 << collision.gameObject.layer) & layerMask) != 0;

        public static int AsMask(this CollisionLayer layer) =>
            1 << (int)layer;

        public static void ChangeLayer<T>(this T entity, CollisionLayer layer) where T : Component =>
            entity.gameObject.layer = (int)layer;
    }
}