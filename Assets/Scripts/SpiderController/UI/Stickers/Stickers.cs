using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Infastructure.StaticData.StaticDataService;
using UnityEngine;
using Zenject;

namespace SpiderController.UI.Stickers
{
    public class Stickers : MonoBehaviour
    {
        private readonly Dictionary<StickerEnum, ParticleSystem> _stickers = new();

        private readonly float _startLifeTime = 10;
        private IStaticDataService _staticDataService;

        [Inject]
        public void Construct(IStaticDataService staticDataService) =>
            _staticDataService = staticDataService;

        private void Awake() =>
            CreateAllParticles();

        private void CreateAllParticles()
        {
            Dictionary<StickerEnum, ParticleSystem> stickerDatas = _staticDataService.StickersStaticData.Stickers;

            foreach (KeyValuePair<StickerEnum, ParticleSystem> pair in stickerDatas)
            {
                StickerEnum stickerEnum = pair.Key;
                ParticleSystem prefab = pair.Value;

                ParticleSystem stickerParticle = Instantiate(prefab, transform);
                stickerParticle.transform.localPosition = new Vector3(-0.5f, 1, -1);
                stickerParticle.gameObject.SetActive(false);

                _stickers.Add(stickerEnum, stickerParticle);
            }
        }

        public void PlaySticker(StickerEnum id)
        {
            if (!_stickers.TryGetValue(id, out ParticleSystem sticker))
                return;

            if (!sticker.isPlaying)
                PlayStickerAsync(sticker).Forget();
        }


        private async UniTask PlayStickerAsync(ParticleSystem sticker)
        {
            sticker.gameObject.SetActive(true);
            sticker.Play();

            await UniTask.Delay(TimeSpan.FromSeconds(_startLifeTime));

            sticker.gameObject.SetActive(false);
        }
    }
}