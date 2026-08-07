using UnityEngine;

namespace Infastructure.Common.StableWorlUpManagement
{
    public interface IStableWorldUp
    {
        /// <param name="isGrounded">
        /// Whether the spider's legs are touching anything. A turn is never concluded in mid-air:
        /// the spider's up only means "the surface I am on" while it is actually on one.
        /// </param>
        void Rotate(Quaternion targetRotation, bool isGrounded);
        Transform StableWorldUpTransform { get; }
        void Initialize();
    }
}