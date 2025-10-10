using System;
using HUD;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace PickupObjects.PickUpOnPlatform
{
    public class Flower : PickupObjectBase
    {
        [SerializeField] private MMF_Player _feedbackPlayer;
        
        public Action OnDroppedFromPlatform;

        private FlowerPointIndicator _flowerPointIndicator;


        public void Initialize(FlowerPointIndicator flowerPointIndicator) =>
            _flowerPointIndicator = flowerPointIndicator;

        public override void StopSimulatePhysics()
        {
            base.StopSimulatePhysics();

            _flowerPointIndicator.HideTargetPoint();
        }

        protected override void StartSimulatePhysics()
        {
            base.StartSimulatePhysics();
            
            _feedbackPlayer.PlayFeedbacks();
            _flowerPointIndicator.ShowTargetPoint();

            OnDroppedFromPlatform?.Invoke();
        }
    }
}