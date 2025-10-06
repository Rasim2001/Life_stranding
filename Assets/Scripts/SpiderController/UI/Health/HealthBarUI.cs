namespace SpiderController.UI.Health
{
    public class HealthBarUI : BarBaseUI
    {
        private HologramEffect _hologramEffect;

        private void Awake() =>
            _hologramEffect = new HologramEffect(GetSegments(), GetContainers(), GetOtherObjects());

        public void PlayFadeHologramEffect() =>
            _hologramEffect.Play();

        public void ShowHologram() =>
            _hologramEffect.Stop();
    }
}