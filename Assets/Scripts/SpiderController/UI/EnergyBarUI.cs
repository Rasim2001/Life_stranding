namespace SpiderController.UI
{
    public class EnergyBarUI : BarBaseUI
    {
        private HologramEffect _hologramEffect;

        private void Awake() =>
            _hologramEffect = new HologramEffect(GetSegments(), GetContainers());

        public void PlayFadeHologramEffect() =>
            _hologramEffect.Play();

        public void ShowHologram() =>
            _hologramEffect.Stop();
    }
}