using System;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace SpiderRig.Editor.TestRunRecorder
{
    [InitializeOnLoad]
    public static class TestRunSceneLoaderPause
    {
        private const string LoaderKey = "DEFAULT_SCENE_LOADER_IS_ACTIVE";
        private const string RestoreKey = "SpiderRig.TestRun.RestoreSceneLoader";
        private const string PhaseKey = "SpiderRig.TestRun.SceneLoaderPausePhase";
        private static readonly Callbacks _callbacks = new Callbacks();

        static TestRunSceneLoaderPause()
        {
            TestRunnerApi.RegisterTestCallback(_callbacks);
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReload;

            // SessionState survives domain reload, but is cleared after an editor crash/restart.
            if (EditorPrefs.GetBool(RestoreKey) && SessionState.GetInt(PhaseKey, 0) == 0)
                Restore("startup recovery");
            else if (SessionState.GetInt(PhaseKey, 0) == 2)
                QueueRestore();
        }

        private static void BeforeAssemblyReload()
        {
            TestRunnerApi.UnregisterTestCallback(_callbacks);
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.update -= RestoreWhenInEditMode;
            AssemblyReloadEvents.beforeAssemblyReload -= BeforeAssemblyReload;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!EditorPrefs.GetBool(RestoreKey))
                return;

            Log(state.ToString());
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetInt(PhaseKey, 0) == 2)
                QueueRestore();
        }

        private static void QueueRestore()
        {
            // Run after all EnteredEditMode handlers, including the vendor loader.
            EditorApplication.update -= RestoreWhenInEditMode;
            EditorApplication.update += RestoreWhenInEditMode;
        }

        private static void RestoreWhenInEditMode()
        {
            if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.update -= RestoreWhenInEditMode;
                Restore("RunFinished restore");
            }
        }

        private static void Restore(string reason)
        {
            if (EditorPrefs.GetBool(RestoreKey))
            {
                EditorPrefs.SetBool(LoaderKey, true);
                EditorPrefs.DeleteKey(RestoreKey);
                Log(reason);
            }
            SessionState.EraseInt(PhaseKey);
        }

        private static void Log(string phase)
        {
            Debug.Log($"[TestRunSceneLoaderPause] {DateTime.UtcNow:O} {phase}; " +
                $"loader={EditorPrefs.GetBool(LoaderKey)}; restore={EditorPrefs.GetBool(RestoreKey)}; " +
                $"playing={EditorApplication.isPlaying}");
        }

        private sealed class Callbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
                SessionState.SetInt(PhaseKey, 1);
                if (EditorPrefs.GetBool(LoaderKey))
                {
                    EditorPrefs.SetBool(RestoreKey, true);
                    EditorPrefs.SetBool(LoaderKey, false);
                }
                Log("RunStarted");
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                Log("RunFinished");
                SessionState.SetInt(PhaseKey, 2);
                QueueRestore();
            }

            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
        }
    }
}
