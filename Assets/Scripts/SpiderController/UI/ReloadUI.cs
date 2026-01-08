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

        private TerrainScan _terrain;

        protected override void Awake()
        {
            base.Awake();

            _colorDefault = _reloadText.color;
            _hologramEffect = new HologramEffect(GetSegments(), GetContainers(), GetOtherObjects());
        }

        protected override void Start()
        {
            base.Start();

            ShowHologram();

            _terrain = TerrainScan.Instance;
            _terrain.OnTerrainScanStart += TerrainStart;
        }


        private void OnDestroy()
        {
            if (_terrain != null)
                TerrainScan.Instance.OnTerrainScanStart -= TerrainStart;

            _hologramEffect.Clear();
        }


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