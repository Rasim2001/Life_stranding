using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Editor.Lighting
{
    public sealed class WorldStaticModelPostprocessor : AssetPostprocessor
    {
        private const string _worldsPathPrefix = "Assets/Art/Worlds/";
        private const string _staticModelPrefix = "SM_";

        private void OnPostprocessModel(GameObject root)
        {
            if (!assetPath.StartsWith(_worldsPathPrefix, StringComparison.Ordinal))
                return;

            string modelName = Path.GetFileNameWithoutExtension(assetPath);
            if (!modelName.StartsWith(_staticModelPrefix, StringComparison.Ordinal))
                return;

            int rendererCount = ConfigureModel(root);
            Debug.Log($"[APV Import] {modelName} ({assetPath}): renderers={rendererCount}; " +
                      "added ContributeGI, ReflectionProbeStatic; MeshRenderer ReceiveGI=LightProbes");
        }

        private static int ConfigureModel(GameObject root)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform transform in transforms)
            {
                GameObject gameObject = transform.gameObject;
                StaticEditorFlags existingFlags = GameObjectUtility.GetStaticEditorFlags(gameObject);
                GameObjectUtility.SetStaticEditorFlags(gameObject,
                    existingFlags | StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic);
            }

            // Set Receive GI after Contribute GI, which can otherwise leave the renderer using lightmaps.
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer is MeshRenderer meshRenderer)
                    meshRenderer.receiveGI = ReceiveGI.LightProbes;
            }

            return renderers.Length;
        }
    }
}
