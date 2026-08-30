using System;
using UnityEngine;

namespace Infastructure.StaticData.World
{
    /// <summary>
    /// Ссылка на сцену, переживающая переименование и перенос ассета.
    /// Истина — ссылка на ассет сцены; имя производное и пересчитывается
    /// из неё в редакторе (SceneReferencePostprocessor).
    /// Рантайм-API загрузки сцены по GUID в Unity нет, поэтому в билд уезжает
    /// именно имя — но как производное значение, а не как то, что правят руками.
    /// </summary>
    [Serializable]
    public class SceneReference
    {
#if UNITY_EDITOR
        [SerializeField] private UnityEditor.SceneAsset _sceneAsset;
#endif
        [SerializeField, HideInInspector] private string _sceneName;

        public string SceneName => _sceneName;

        public bool IsValid => !string.IsNullOrEmpty(_sceneName);

#if UNITY_EDITOR
        public UnityEditor.SceneAsset SceneAsset => _sceneAsset;

        /// <summary>Назначает ссылку на ассет сцены и пересчитывает производное имя.</summary>
        public void SetAsset(UnityEditor.SceneAsset asset)
        {
            _sceneAsset = asset;
            SyncFromAsset();
        }

        /// <summary>Пересчитывает производное имя. Возвращает true, если оно изменилось.</summary>
        public bool SyncFromAsset()
        {
            string name = _sceneAsset != null
                ? System.IO.Path.GetFileNameWithoutExtension(UnityEditor.AssetDatabase.GetAssetPath(_sceneAsset))
                : "";

            if (_sceneName == name)
                return false;

            _sceneName = name;
            return true;
        }
#endif
    }
}
