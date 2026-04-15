using System;
using Cysharp.Threading.Tasks;
using HighlightPlus;

namespace SpiderController
{
    public class EnergyLegs
    {
        private readonly HighlightEffect[] _highlightEffect;

        private int _count;

        public EnergyLegs(HighlightEffect[] highlightEffect) =>
            _highlightEffect = highlightEffect;

        public void AddEnergyOnLeg()
        {
            if (_count < _highlightEffect.Length)
                AddEnergyOnLegAsync().Forget();
        }

        private async UniTask AddEnergyOnLegAsync()
        {
            _count++;

            _highlightEffect[_count].gameObject.SetActive(true);
            _highlightEffect[_count].Highlighted = true;

            await UniTask.Delay(TimeSpan.FromSeconds(2));

            _highlightEffect[_count].Highlighted = false;
        }
    }
}