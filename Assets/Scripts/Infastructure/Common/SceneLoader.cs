using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Infastructure.Common
{
    public interface ISceneLoader
    {
        void Load(string name, Action onLoaded = null);
        bool IsGameScene();
    }

    public class SceneLoader : ISceneLoader
    {
        private readonly ICoroutineRunner coroutineRunner;

        public SceneLoader(ICoroutineRunner coroutineRunner) =>
            this.coroutineRunner = coroutineRunner;

        public void Load(string name, Action onLoaded = null) =>
            coroutineRunner.StartCoroutine(LoadScene(name, onLoaded));

        public bool IsGameScene() =>
            SceneManager.GetActiveScene().name == AssetsPath.GameScene;

        private IEnumerator LoadScene(string nextScene, Action onLoaded = null)
        {
            AsyncOperation waitNextScene = SceneManager.LoadSceneAsync(nextScene);

            while (!waitNextScene.isDone)
                yield return null;

            onLoaded?.Invoke();
        }
    }
}