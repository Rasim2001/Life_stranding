using GameDevBuddies;
using TMPro;
using UnityEngine;

namespace SpiderController.UI
{
    public class ReloadUI : BarBaseUI
    {
        [SerializeField] private TextMeshProUGUI _reloadText;

        private HologramEffect _hologramEffect;
        private Color _colorDefault;

        private void Awake()
        {
            _colorDefault = _reloadText.color;
            _hologramEffect = new HologramEffect(GetSegments(), GetContainers(), GetOtherObjects());
        }

        private void Start()
        {
            ShowHologram();

            TerrainScan.Instance.OnTerrainScanStart += TerrainStart;
        }


        private void OnDestroy() =>
            TerrainScan.Instance.OnTerrainScanStart -= TerrainStart;


        private void TerrainStart(TerrainScanInfo obj)
        {
            _hologramEffect.Stop();
            _reloadText.color = _colorDefault;
        }

        public void ShowHologram()
        {
            _hologramEffect.Play();

            Color newColor = _colorDefault;
            newColor.a = 0;
            _reloadText.color = newColor;
        }
    }
}