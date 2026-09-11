using System;
using System.Collections;
using Dreamteck.Splines;
using Infastructure.CutScenes;
using Infastructure.Data;
using Infastructure.Services.CameraProvider;
using Infastructure.Services.CutScene;
using Infastructure.Services.ProgressWatchers;
using Infastructure.Services.SaveLoadService;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;

namespace Common.Laboratory
{
    public class LaboratoryExplosion : MonoBehaviour, ISavedProgressReader
    {
        private const string SealedScenario = "Sealed";
        private const string BreachedScenario = "Breached";

        private static readonly int ExplosionTriggerHash = Animator.StringToHash("ExplosionTrigger");

        [SerializeField] private Animator _animator;
        [SerializeField] private GameObject _intactGroup;
        [SerializeField] private GameObject _defaultGroup;
        [SerializeField] private GameObject _animationGroup;
        [SerializeField] private GameObject _finishedDebris;
        [SerializeField] private ReflectionProbe _sealedProbe;
        [SerializeField] private ReflectionProbe _breachedProbe;
        [SerializeField] private float _lightBlendDelay = 0.3f;
        [SerializeField] private float _lightBlendDuration = 0.5f;

        private ICutSceneService _cutSceneService;
        private ICameraProviderService _providerService;

        private float _blendTime;
        private Coroutine _coroutine;
        private Coroutine _lightCoroutine;
        private IProgressWatchersService _progressWatchersService;

#if UNITY_EDITOR
        // Активный сценарий сериализуется в BS_World_Tower.asset, и SetActiveScenario метит ассет
        // грязным. Без возврата ассет остаётся в Breached и ломает шаг 4 ритуала запекания.
        private string _scenarioBeforePlay;
#endif

        [Inject]
        public void Construct(ICutSceneService cutSceneService, ICameraProviderService providerService,
            IProgressWatchersService progressWatchersService)
        {
            _progressWatchersService = progressWatchersService;
            _providerService = providerService;
            _cutSceneService = cutSceneService;
        }

        private void Awake()
        {
            if (_sealedProbe == null || _breachedProbe == null)
                Debug.LogError($"{nameof(LaboratoryExplosion)}: не назначены отражающие пробники — " +
                               "отражения в лаборатории не переключатся.", this);

            if (_intactGroup == null || _defaultGroup == null || _animationGroup == null || _finishedDebris == null)
                Debug.LogError($"{nameof(LaboratoryExplosion)}: не назначены группы геометрии стены — " +
                               "стена не сменит состояние.", this);

#if UNITY_EDITOR
            _scenarioBeforePlay = ProbeReferenceVolume.instance.lightingScenario;
#endif

            _progressWatchersService.RegisterWatchers(gameObject);
        }

        public void LoadProgress(PlayerProgress progress)
        {
            bool isBreached = progress.WorldProgressData.CutsceneData.FlowerWasPicked;

            SetScenario(isBreached ? BreachedScenario : SealedScenario);
            SwitchProbes(isBreached);

            if (isBreached)
                ApplyFinishedState();
            else
                ApplyIntactState();
        }


        private void Start()
        {
            _blendTime = _providerService.CameraTransform.GetComponent<CinemachineBrain>().DefaultBlend.Time;

            _cutSceneService.OnCutsceneActiveChanged += CutSceneStarted;
        }

        private void OnDestroy()
        {
            if (_coroutine != null)
                StopCoroutine(_coroutine);

            if (_lightCoroutine != null)
                StopCoroutine(_lightCoroutine);

#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(_scenarioBeforePlay))
                SetScenario(_scenarioBeforePlay);
#endif

            _progressWatchersService.Release(this);
            _cutSceneService.OnCutsceneActiveChanged -= CutSceneStarted;
        }

        private void CutSceneStarted(bool isStarted)
        {
            if (_cutSceneService.CutsceneId != CutsceneId.FlowerPickupCutscene)
                return;

            if (isStarted)
                _coroutine = StartCoroutine(StartExplosion());
            else
                SwitchProbes(true);
        }

        private IEnumerator StartExplosion()
        {
            yield return new WaitForSeconds(_blendTime);

            // Группа включается до SetTrigger: на выключенном аниматоре триггер теряется
            // и в консоль уходит "Animator is not playing an AnimatorController".
            ApplyExplodingState();

            _animator.SetTrigger(ExplosionTriggerHash);

            _lightCoroutine = StartCoroutine(BlendToBreached());

            yield return new WaitForSeconds(5);

            ApplyFinishedState();
        }

        private IEnumerator BlendToBreached()
        {
            ProbeReferenceVolume probeVolume = ProbeReferenceVolume.instance;

            if (probeVolume.currentBakingSet == null)
            {
                Debug.LogError($"{nameof(LaboratoryExplosion)}: набор запекания APV не загружен, " +
                               "сценарий не сменится.", this);
                yield break;
            }

            // Клип начинается не с первого кадра: стена стоит на месте, пока не отыграет
            // раскачка. Свет ждёт ровно столько же, иначе приходит в целую стену.
            yield return new WaitForSeconds(_lightBlendDelay);

            float elapsed = 0f;

            while (elapsed < _lightBlendDuration)
            {
                elapsed += Time.deltaTime;

                probeVolume.BlendLightingScenario(BreachedScenario, Mathf.Clamp01(elapsed / _lightBlendDuration));

                yield return null;
            }

            // Коммит: сеттер уходит в SetActiveScenario, который сам обнуляет фактор блендинга
            // и освобождает оба пула. Без него они остаются занятыми навсегда.
            probeVolume.lightingScenario = BreachedScenario;
        }

        private void SetScenario(string scenario)
        {
            ProbeReferenceVolume probeVolume = ProbeReferenceVolume.instance;

            if (probeVolume.currentBakingSet == null)
                return;

            probeVolume.lightingScenario = scenario;
        }

        // Каждое состояние проставляет все четыре группы целиком, а не разницу с предыдущим:
        // в LoadProgress предыдущего состояния нет вообще.
        private void ApplyIntactState() =>
            ApplyWallGroups(intact: true, common: false, animation: false, debris: false);

        private void ApplyExplodingState() =>
            ApplyWallGroups(intact: false, common: true, animation: true, debris: false);

        private void ApplyFinishedState() =>
            ApplyWallGroups(intact: false, common: true, animation: false, debris: true);

        private void ApplyWallGroups(bool intact, bool common, bool animation, bool debris)
        {
            if (_intactGroup != null)
                _intactGroup.SetActive(intact);

            if (_defaultGroup != null)
                _defaultGroup.SetActive(common);

            if (_animationGroup != null)
                _animationGroup.SetActive(animation);

            _finishedDebris.SetActive(debris);
        }

        private void SwitchProbes(bool isBreached)
        {
            if (_sealedProbe == null || _breachedProbe == null)
                return;

            _sealedProbe.gameObject.SetActive(!isBreached);
            _breachedProbe.gameObject.SetActive(isBreached);
        }
    }
}
