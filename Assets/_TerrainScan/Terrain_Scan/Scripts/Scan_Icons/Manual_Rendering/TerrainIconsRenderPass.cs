using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;


namespace GameDevBuddies
{
    public class TerrainIconsRenderPass : ScriptableRenderPass
    {
        [Serializable]
        private class RenderPassData
        {
            public RTHandle OutputColorRTHandle;
            public RTHandle OutputDepthRTHandle;
            public bool ShouldClearOutputTexture;
            public ProfilingSampler ProfilingSampler;

            public LayerMask SupportedLayersMask;
            public List<ShaderTagId> SupportedShaderTagIds;
            public FilteringSettings FilteringSettings;

            public Material OverrideMaterial;
            public int OverrideMaterialPassIndex;
        }

        private string _cameraTagId = string.Empty;
        private List<RenderPassData> _renderPassesData = new List<RenderPassData>();

        public TerrainIconsRenderPass(string cameraTagId, List<TerrainIconsRenderPassSettings> renderPassesSettings)
        {
            _cameraTagId = cameraTagId;

            Dictionary<RenderTexture, RTHandle> createdHandlesForTextures = new Dictionary<RenderTexture, RTHandle>();
            foreach (TerrainIconsRenderPassSettings renderPassSettings in renderPassesSettings)
            {
                // If the current pass doesn't have an output render texture assigned yet,
                // we can't correctly set up the render pass data, so it will be skipped until
                // the output texture is correctly assigned.
                RenderTexture outputColorTexture = renderPassSettings.OutputColorTexture;
                if (outputColorTexture == null)
                {
                    break;
                }

                // Creating RTHandles for output textures with keeping in mind no to create
                // duplicate RTHandles if multiple render passes use the same output texture.
                RTHandle outputColorRTHandle = CreateOrReuseRTHandle(outputColorTexture, ref createdHandlesForTextures);
                RTHandle outputDepthRTHandle = CreateOrReuseRTHandle(renderPassSettings.OutputDepthTexture,
                    ref createdHandlesForTextures);

                // Create a list of shader tag IDs that will be used for filtering which materials would
                // be rendered with this custom render pass.
                List<ShaderTagId> supportedShaderTagIds = new List<ShaderTagId>();
                foreach (string supportedShaderPassName in renderPassSettings.SupportedShaderPassNames)
                {
                    supportedShaderTagIds.Add(new ShaderTagId(supportedShaderPassName));
                }

                // Create filtering settings for the layers of objects that will be rendered with this pass.
                FilteringSettings filteringSettings = new FilteringSettings(new RenderQueueRange(0, 5000),
                    renderPassSettings.SupportedLayersMask);

                // Create a profiling sampler to track this render pass in the frame debugger.
                ProfilingSampler profilingSampler = new ProfilingSampler(renderPassSettings.ProfilingPassName);

                // Cache everything in a helper data class.
                RenderPassData renderPassData = new RenderPassData();
                renderPassData.OutputColorRTHandle = outputColorRTHandle;
                renderPassData.OutputDepthRTHandle = outputDepthRTHandle;
                renderPassData.ShouldClearOutputTexture = renderPassSettings.ShouldClearOutputTexture;
                renderPassData.ProfilingSampler = profilingSampler;
                renderPassData.SupportedLayersMask = renderPassSettings.SupportedLayersMask;
                renderPassData.SupportedShaderTagIds = supportedShaderTagIds;
                renderPassData.FilteringSettings = filteringSettings;
                renderPassData.OverrideMaterial = renderPassSettings.OverrideMaterial;
                renderPassData.OverrideMaterialPassIndex = renderPassSettings.OverrideMaterialPassIndex;

                // Cache the helper data class into a collection for all passes.
                _renderPassesData.Add(renderPassData);
            }
        }   // конец конструктора TerrainIconsRenderPass(...)

        // === Render Graph support (URP 17+) ===
        private class RGPassData
        {
            public RendererListHandle RendererList;
            public bool DoClear;
            public Color ClearColor;
        }


        // Путь для Render Graph (работает, когда RG включен и режим совместимости выключен)
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var camData = frameData.Get<UniversalCameraData>();
            // Если нужно снова фильтровать по камере-иконок, раскомментируй 2 строки ниже:
             if (!camData.camera || !camData.camera.CompareTag(_cameraTagId))
                 return;

            var res = frameData.Get<UniversalResourceData>();
            var urp = frameData.Get<UniversalRenderingData>();

            foreach (var pass in _renderPassesData)
            {
                Debug.Log($"[IconsRG] Icons pass on camera '{camData.camera.name}'");

                // Импорт/выбор целей
                var colorTarget = pass.OutputColorRTHandle != null
                    ? renderGraph.ImportTexture(pass.OutputColorRTHandle)
                    : res.activeColorTexture;

                var depthTarget = pass.OutputDepthRTHandle != null
                    ? renderGraph.ImportTexture(pass.OutputDepthRTHandle)
                    : res.activeDepthTexture;

                // Сборка RendererList
                var sortingSettings = new SortingSettings(camData.camera) { criteria = SortingCriteria.CommonTransparent };
                
                List<ShaderTagId> tagIds = pass.SupportedShaderTagIds;
                if (pass.OverrideMaterial != null)
                {
                    tagIds = new List<ShaderTagId>(tagIds);
                    tagIds.Insert(0, new ShaderTagId("SRPDefaultUnlit"));
                }

                var drawing = new DrawingSettings(tagIds[0], sortingSettings);
                for (int i = 1; i < tagIds.Count; i++)
                    drawing.SetShaderPassName(i, tagIds[i]);

                if (pass.OverrideMaterial != null)
                {
                    drawing.overrideMaterial = pass.OverrideMaterial;
                    drawing.overrideMaterialPassIndex = pass.OverrideMaterialPassIndex;
                }

                var filtering = pass.FilteringSettings;
                filtering.renderQueueRange = RenderQueueRange.all;

                var rlParams = new RendererListParams(urp.cullResults, drawing, filtering);

                using (var builder = renderGraph.AddRasterRenderPass<RGPassData>("Terrain Icons", out var data, pass.ProfilingSampler))
                {
                    data.RendererList = renderGraph.CreateRendererList(rlParams);
                    data.DoClear = pass.ShouldClearOutputTexture;
                    data.ClearColor = Color.clear;

                    builder.UseRendererList(data.RendererList);
                    builder.SetRenderAttachment(colorTarget, 0, AccessFlags.ReadWrite);
                    builder.SetRenderAttachmentDepth(depthTarget, AccessFlags.ReadWrite);
                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc((RGPassData d, RasterGraphContext ctx) =>
                    {
                        if (d.DoClear)
                            ctx.cmd.ClearRenderTarget(true, true, d.ClearColor);

                        ctx.cmd.DrawRendererList(d.RendererList);
                    });
                }
            }
        }

        // === /Render Graph support ===


        /// <inheritdoc/>
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Only rendering terrain icons with the special orthographic terrain icons camera.
            if (!renderingData.cameraData.camera.CompareTag(_cameraTagId))
            {
                return;
            }

            // Go over every render pass and execute a render command.
            RTHandle currentColorOutputRTHandle = null;
            RTHandle currentDepthOutputRTHandle = null;
            foreach (RenderPassData renderPassData in _renderPassesData)
            {
                bool depthOutputChanged = false;
                bool colorOutputChanged = false;

                if ((renderPassData.OutputDepthRTHandle != null &&
                     renderPassData.OutputDepthRTHandle != currentDepthOutputRTHandle) ||
                    (renderPassData.OutputDepthRTHandle == null && currentDepthOutputRTHandle != null))
                {
                    currentDepthOutputRTHandle = renderPassData.OutputDepthRTHandle;
                    depthOutputChanged = true;
                }

                if (renderPassData.OutputColorRTHandle != currentColorOutputRTHandle)
                {
                    currentColorOutputRTHandle = renderPassData.OutputColorRTHandle;
                    colorOutputChanged = true;
                }

                if (colorOutputChanged || depthOutputChanged)
                {
                    if (currentDepthOutputRTHandle == null)
                    {
                        ConfigureTarget(currentColorOutputRTHandle);
                    }
                    else
                    {
                        ConfigureTarget(currentColorOutputRTHandle, currentDepthOutputRTHandle);
                    }
                }

                renderingData.cameraData.camera.TryGetCullingParameters(
                    out ScriptableCullingParameters cullingParameters);
                ExecuteRenderPass(context, ref renderingData, ref cullingParameters, renderPassData);
            }
        }

        private void ExecuteRenderPass(ScriptableRenderContext context, ref RenderingData renderingData,
            ref ScriptableCullingParameters cullingParameters, RenderPassData renderPassData)
        {
            // Get a command buffer instance from the pool.
            CommandBuffer cmd = CommandBufferPool.Get();

            // Create drawing settings for this render pass based on the supported shader tags.
            DrawingSettings drawingSettings = CreateDrawingSettings(renderPassData.SupportedShaderTagIds,
                ref renderingData, SortingCriteria.CommonTransparent);
            if (renderPassData.OverrideMaterial != null)
            {
                drawingSettings.overrideMaterial = renderPassData.OverrideMaterial;
                drawingSettings.overrideMaterialPassIndex = renderPassData.OverrideMaterialPassIndex;
            }

            // Update the camera culling mask to the desired rendering layers for this render pass.
            cullingParameters.cullingMask = (uint)(int)renderPassData.SupportedLayersMask;

            using (new ProfilingScope(cmd, renderPassData.ProfilingSampler))
            {
                // Always clear the command buffer first.
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                if (renderPassData.ShouldClearOutputTexture)
                {
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                    cmd.ClearRenderTarget(true, true, Color.clear);
                }

                // Create a list of renderers that will need to be drawn in this pass based on filtering settings
                // for layers and materials and by the camera frustum culling.
                RendererListParams rendererListParams = new RendererListParams
                {
                    cullingResults = context.Cull(ref cullingParameters),
                    drawSettings = drawingSettings,
                    filteringSettings = renderPassData.FilteringSettings
                };
                RendererList rendererList = context.CreateRendererList(ref rendererListParams);

                // Execute the draw command if the renderer list is not empty.
                cmd.DrawRendererList(rendererList);
            }

            // Execute all commands of the command buffer.
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            // Release command buffer back to the pool so that it can be reused.
            CommandBufferPool.Release(cmd);
        }

        private RTHandle CreateOrReuseRTHandle(RenderTexture renderTexture,
            ref Dictionary<RenderTexture, RTHandle> createdHandlesForTextures)
        {
            if (renderTexture == null)
            {
                return null;
            }

            // Re-use or create a new RTHandle for the color and depth textures.
            if (createdHandlesForTextures.ContainsKey(renderTexture))
            {
                return createdHandlesForTextures[renderTexture];
            }
            else
            {
                RTHandle outputRTHandle = RTHandles.Alloc(renderTexture);
                createdHandlesForTextures.Add(renderTexture, outputRTHandle);
                return outputRTHandle;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_renderPassesData == null)
            {
                return;
            }

            foreach (RenderPassData renderPassData in _renderPassesData)
            {
                if (renderPassData.OutputColorRTHandle != null)
                {
                    renderPassData.OutputColorRTHandle = null;
                }

                if (renderPassData.OutputDepthRTHandle != null)
                {
                    renderPassData.OutputDepthRTHandle = null;
                }
            }

            _renderPassesData.Clear();
        }
    }
}