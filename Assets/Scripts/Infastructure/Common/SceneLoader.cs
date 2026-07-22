using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using Google.Apis.Logging;
using Infastructure.StaticData;
using Infastructure.StaticData.StaticDataService;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Infastructure.Common
{
    public interface ISceneLoader
    {
        void Load(string name, Action onLoaded = null);
        void LoadAllScenes(string[] scenes, Action onLoaded = null);
        bool IsTutorialScene();
    }

    public class SceneLoader : ISceneLoader
    {
        private readonly ICoroutineRunner _coroutineRunner;
        private readonly IStaticDataService _staticDataService;

        private GameStaticData GameData => _staticDataService.GameStaticData;

        public SceneLoader(ICoroutineRunner coroutineRunner, IStaticDataService staticDataService)
        {
            _staticDataService = staticDataService;
            _coroutineRunner = coroutineRunner;
        }

        public void Load(string name, Action onLoaded = null) =>
            _coroutineRunner.StartCoroutine(LoadScene(name, onLoaded));

        public void LoadAllScenes(string[] scenes, Action onLoaded = null) =>
            LoadAllSceneCoroutine(scenes, onLoaded).Forget();

        public bool IsTutorialScene() =>
            SceneManager.GetActiveScene().name == GameData.TutorialSceneName;

        private IEnumerator LoadScene(string nextScene, Action onLoaded = null)
        {
            AsyncOperation waitNextScene = SceneManager.LoadSceneAsync(nextScene);

            while (!waitNextScene.isDone)
                yield return null;

            onLoaded?.Invoke();
        }

        private async UniTask LoadAllSceneCoroutine(string[] scenes, Action onLoaded = null)
        {
            foreach (string name in scenes)
                await SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive).ToUniTask();

            onLoaded?.Invoke();
        }
    }
}