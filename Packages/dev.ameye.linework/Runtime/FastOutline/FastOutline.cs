using System;
using System.Linq;
using Linework.Common.Utils;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_6000_0_OR_NEWER
using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;
#endif
using UnityEngine.Rendering.Universal;

namespace Linework.FastOutline
{
    [ExcludeFromPreset]
    [DisallowMultipleRendererFeature("Fast Outline")]
#if UNITY_6000_0_OR_NEWER
    [SupportedOnRenderer(typeof(UniversalRendererData))]
#endif
    [Tooltip("Fast Outline renders outlines by rendering an extruded version of an object behind the original object.")]
    [HelpURL("https://linework.ameye.dev/outlines/fast-outline")]
    public class FastOutline : ScriptableRendererFeature
    {
        private class FastOutlinePass : ScriptableRenderPass
        {
            private FastOutlineSettings settings;
            private Material mask, outlineBase, outlineInstancedBase, clear;
            private readonly ProfilingSampler maskSampler, outlineSampler;

            public FastOutlinePass()
            {
                profilingSampler = new ProfilingSampler(nameof(FastOutlinePass));
                maskSampler = new ProfilingSampler(ShaderPassName.Mask);
                outlineSampler = new ProfilingSampler(ShaderPassName.Outline);
            }
            
            public bool Setup(ref FastOutlineSettings fastOutlineSettings, ref Material maskMaterial, ref Material outlineMaterial, ref Material outlineInstancedMaterial, ref Material clearMaterial)
            {
                settings = fastOutlineSettings;
                mask = maskMaterial;
                outlineBase = outlineMaterial;
                outlineInstancedBase = outlineInstancedMaterial;
                clear = clearMaterial;
                renderPassEvent = (RenderPassEvent) fastOutlineSettings.InjectionPoint;

                foreach (var outline in settings.Outlines)
                {
                    if (outline.material == null || outline.materialInstanced == null)
                    {
                        outline.AssignMaterials(outlineBase, outlineInstancedBase);
                    }
                }

                foreach (var outline in settings.Outlines)
                {
                    if (!outline.IsActive())
                    {
                        continue;
                    }
                    
                    var material = outline.gpuInstancing ? outline.materialInstanced : outline.material;

                    var (srcBlend, dstBlend) = RenderUtils.GetSrcDstBlend(outline.blendMode);
                    material.SetInt(CommonShaderPropertyId.BlendModeSource, srcBlend);
                    material.SetInt(CommonShaderPropertyId.BlendModeDestination, dstBlend);
                    switch (outline.maskingStrategy)
                    {
                        case MaskingStrategy.Stencil:
                            material.SetFloat(CommonShaderPropertyId.CullMode, (float) CullMode.Off);
                            break;
                        case MaskingStrategy.CullFrontFaces:
                            material.SetFloat(CommonShaderPropertyId.CullMode, (float) CullMode.Front);
                            break;
                    }
                    material.SetColor(CommonShaderPropertyId.OutlineColor, outline.color);
                    material.SetColor(ShaderPropertyId.OutlineOccludedColor, outline.occlusion == Occlusion.WhenOccluded ? outline.color : outline.occludedColor);
                    material.SetFloat(ShaderPropertyId.OutlineWidth, outline.width);
                    if (outline.extrusionMethod == ExtrusionMethod.ClipSpaceNormalVector)
                    {
                        material.SetFloat(ShaderPropertyId.OutlineWidth, outline.width);
                        material.SetFloat(ShaderPropertyId.MinOutlineWidth, outline.minWidth);
                    }
                    else
                    {
                        material.SetFloat(ShaderPropertyId.OutlineWidth, outline.width * 0.015f);
                        material.SetFloat(ShaderPropertyId.MinOutlineWidth, outline.minWidth * 0.015f);
                    }
                    if (outline.enableOcclusion) material.EnableKeyword(ShaderFeature.Occlusion);
                    else material.DisableKeyword(ShaderFeature.Occlusion);
                    if (outline.scaling == Scaling.ScaleWithDistance) material.EnableKeyword(ShaderFeature.ScaleWithDistance);
                    else material.DisableKeyword(ShaderFeature.ScaleWithDistance);
                    switch (outline.occlusion)
                    {
                        case Occlusion.Always:
                            material.SetFloat(CommonShaderPropertyId.ZTest, (float) CompareFunction.Always);
                            break;
                        case Occlusion.WhenOccluded:
                            material.SetFloat(CommonShaderPropertyId.ZTest, (float) CompareFunction.GreaterEqual);
                            break;
                        case Occlusion.WhenNotOccluded:
                            material.SetFloat(CommonShaderPropertyId.ZTest, (float) CompareFunction.LessEqual);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                return settings.Outlines.Any(ShouldRenderOutline);
            }
            
            private static bool ShouldRenderStencilMask(Outline outline)
            {
                return outline.IsActive() && (outline.maskingStrategy == MaskingStrategy.Stencil || outline.occlusion != Occlusion.WhenNotOccluded);
            }

            private static bool ShouldRenderOutline(Outline outline)
            {
                return outline.IsActive();
            }
            
#if UNITY_6000_0_OR_NEWER
            private class PassData
            {
                internal RendererListHandle MaskRendererListHandle;
                internal readonly List<RendererListHandle> OutlineRendererListHandles = new();
            }
            
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resourceData = frameData.Get<UniversalResourceData>();

                // 1. Mask.
                // -> Render a mask to the stencil buffer.
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(ShaderPassName.Mask, out var passData))
                {
                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);
                
                    InitMaskRendererList(renderGraph, frameData, ref passData);
                    builder.UseRendererList(passData.MaskRendererListHandle);
                
                    builder.AllowPassCulling(false);
                
                    builder.SetRenderFunc((PassData data, RasterGraphContext context) => { context.cmd.DrawRendererList(data.MaskRendererListHandle); });
                }

                // 2. Outline.
                // -> Render an outline.
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(ShaderPassName.Outline, out var passData))
                {
                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);

                    InitOutlineRendererLists(renderGraph, frameData, ref passData);
                    foreach (var rendererListHandle in passData.OutlineRendererListHandles)
                    {
                        builder.UseRendererList(rendererListHandle);
                    }

                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        foreach (var handle in data.OutlineRendererListHandles)
                        {
                            context.cmd.DrawRendererList(handle);
                        }
                    });
                }
                
                // 3. Clear stencil.
                // -> Clear the stencil buffer.
                RenderUtils.ClearStencil(renderGraph, resourceData, clear);
            }

            private void InitMaskRendererList(RenderGraph renderGraph, ContextContainer frameData, ref PassData passData)
            {
                var renderingData = frameData.Get<UniversalRenderingData>();
                var cameraData = frameData.Get<UniversalCameraData>();
                var lightData = frameData.Get<UniversalLightData>();

                var sortingCriteria = cameraData.defaultOpaqueSortFlags;
                var renderQueueRange = RenderQueueRange.opaque;
                var layer = new RenderingLayerMask();
                layer = settings.Outlines
                    .Where(ShouldRenderStencilMask)
                    .Aggregate(layer, (current, outline) => current | outline.RenderingLayer);
                var filteringSettings = new FilteringSettings(renderQueueRange, -1, layer);
                var drawingSettings = RenderingUtils.CreateDrawingSettings(RenderUtils.DefaultShaderTagIds, renderingData, cameraData, lightData, sortingCriteria);
                drawingSettings.overrideMaterial = mask;

                var renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

                var blendState = BlendState.defaultValue;
                blendState.blendState0 = new RenderTargetBlendState(0);
                renderStateBlock.blendState = blendState;

                var stencilState = StencilState.defaultValue;
                stencilState.enabled = true;
                stencilState.SetCompareFunction(CompareFunction.Always);
                stencilState.SetPassOperation(StencilOp.Replace);
                stencilState.SetFailOperation(StencilOp.Replace);
                stencilState.SetZFailOperation(StencilOp.Replace);
                renderStateBlock.mask |= RenderStateMask.Stencil;
                renderStateBlock.stencilReference = 1;
                renderStateBlock.stencilState = stencilState;

                RenderUtils.CreateRendererListWithRenderStateBlock(renderGraph, ref renderingData.cullResults, drawingSettings, filteringSettings, renderStateBlock,
                    ref passData.MaskRendererListHandle);
            }

            private void InitOutlineRendererLists(RenderGraph renderGraph, ContextContainer frameData, ref PassData passData)
            {
                passData.OutlineRendererListHandles.Clear();

                var renderingData = frameData.Get<UniversalRenderingData>();
                var cameraData = frameData.Get<UniversalCameraData>();
                var lightData = frameData.Get<UniversalLightData>();

                var sortingCriteria = cameraData.defaultOpaqueSortFlags;
                var renderQueueRange = RenderQueueRange.opaque;

                foreach (var outline in settings.Outlines)
                {
                    if (!ShouldRenderOutline(outline))
                    {
                        continue;
                    }
                    
                    var drawingSettings = RenderingUtils.CreateDrawingSettings(RenderUtils.DefaultShaderTagIds, renderingData, cameraData, lightData, sortingCriteria);
                    drawingSettings.overrideMaterial = outline.gpuInstancing ? outline.materialInstanced : outline.material;
                    drawingSettings.overrideMaterialPassIndex = (int) outline.extrusionMethod;
                    drawingSettings.enableInstancing = outline.gpuInstancing;
                    switch (outline.materialType)
                    {
                        case MaterialType.Basic:
                            drawingSettings.overrideMaterial = outline.gpuInstancing ? outline.materialInstanced : outline.material;
                            drawingSettings.overrideMaterialPassIndex = (int) outline.extrusionMethod;
                            drawingSettings.enableInstancing = outline.gpuInstancing;
                            break;
                        case MaterialType.Custom when outline.customMaterial != null:
                            drawingSettings.overrideMaterial = outline.customMaterial;
                            break;
                    }
         
                    var filteringSettings = new FilteringSettings(renderQueueRange, -1, outline.RenderingLayer);

                    // Override stencil state.
                    var renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
                    if (ShouldRenderStencilMask(outline))
                    {
                        var stencilState = StencilState.defaultValue;
                        stencilState.enabled = true;
                        stencilState.SetCompareFunction(CompareFunction.NotEqual);
                        stencilState.SetPassOperation(StencilOp.Zero);
                        stencilState.SetFailOperation(StencilOp.Keep); // Why is Zero not possible here?
                        renderStateBlock.mask |= RenderStateMask.Stencil;
                        renderStateBlock.stencilReference = 1;
                        renderStateBlock.stencilState = stencilState;
                    }

                    var handle = new RendererListHandle();
                    RenderUtils.CreateRendererListWithRenderStateBlock(renderGraph, ref renderingData.cullResults, drawingSettings, filteringSettings, renderStateBlock,
                        ref handle);
                    passData.OutlineRendererListHandles.Add(handle);

                }
            }
#endif
            private RTHandle cameraDepthRTHandle;
            
            #pragma warning disable 618, 672
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                ConfigureTarget(cameraDepthRTHandle);
            }
            
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                // 1. Mask.
                // -> Render a mask to the stencil buffer.
                var maskCmd = CommandBufferPool.Get();

                using (new ProfilingScope(maskCmd, maskSampler))
                {
                    context.ExecuteCommandBuffer(maskCmd);
                    maskCmd.Clear();

                    var sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
                    var renderQueueRange = RenderQueueRange.opaque;

                    uint layer = 0;
                    layer = settings.Outlines
                        .Where(ShouldRenderStencilMask)
                        .Aggregate(layer, (current, outline) => current | outline.RenderingLayer);
                    var filteringSettings = new FilteringSettings(renderQueueRange, -1, layer);
                    var drawingSettings = RenderingUtils.CreateDrawingSettings(RenderUtils.DefaultShaderTagIds, ref renderingData, sortingCriteria);
                   
                    drawingSettings.overrideMaterial = mask;

                    var renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

                    var blendState = BlendState.defaultValue;
                    blendState.blendState0 = new RenderTargetBlendState(0);
                    renderStateBlock.blendState = blendState;

                    var stencilState = StencilState.defaultValue;
                    stencilState.enabled = true;
                    stencilState.SetCompareFunction(CompareFunction.Always);
                    stencilState.SetPassOperation(StencilOp.Replace);
                    stencilState.SetFailOperation(StencilOp.Replace);
                    stencilState.SetZFailOperation(StencilOp.Replace);
                    renderStateBlock.mask |= RenderStateMask.Stencil;
                    renderStateBlock.stencilReference = 1;
                    renderStateBlock.stencilState = stencilState;

                    context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);
                }

                context.ExecuteCommandBuffer(maskCmd);
                CommandBufferPool.Release(maskCmd);

                // 2. Outline.
                // -> Render an outline.
                var outlineCmd = CommandBufferPool.Get();

                using (new ProfilingScope(outlineCmd, outlineSampler))
                {
                    CoreUtils.SetRenderTarget(outlineCmd, renderingData.cameraData.renderer.cameraColorTargetHandle, cameraDepthRTHandle); // if using cameraColorRTHandle this does not render in scene view when rendering after post processing with post processing enabled
                    context.ExecuteCommandBuffer(outlineCmd);
                    outlineCmd.Clear();
                    
                    var sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
                    var renderQueueRange = RenderQueueRange.opaque;

                    foreach (var outline in settings.Outlines)
                    {
                        if (!ShouldRenderOutline(outline))
                        {
                            continue;
                        }

                        var drawingSettings = RenderingUtils.CreateDrawingSettings(RenderUtils.DefaultShaderTagIds, ref renderingData, sortingCriteria);
                        drawingSettings.overrideMaterial = outline.material;
                        drawingSettings.overrideMaterialPassIndex = (int) outline.extrusionMethod;
                        drawingSettings.perObjectData = PerObjectData.None;
                        drawingSettings.enableInstancing = false;

                        var filteringSettings = new FilteringSettings(renderQueueRange, -1, outline.RenderingLayer);
                        
                        var renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
                        if (ShouldRenderStencilMask(outline))
                        {
                            var stencilState = StencilState.defaultValue;
                            stencilState.enabled = true;
                            stencilState.SetCompareFunction(CompareFunction.NotEqual);
                            stencilState.SetPassOperation(StencilOp.Zero);
                            stencilState.SetFailOperation(StencilOp.Keep);
                            renderStateBlock.mask |= RenderStateMask.Stencil;
                            renderStateBlock.stencilReference = 1;
                            renderStateBlock.stencilState = stencilState;
                        }
                        
                        context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);

                    }
                }

                context.ExecuteCommandBuffer(outlineCmd);
                CommandBufferPool.Release(outlineCmd);
            }
            #pragma warning restore 618, 672
            
            public void SetTarget(RTHandle depth)
            {
                cameraDepthRTHandle = depth;
            }

            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                if (cmd == null)
                {
                    throw new ArgumentNullException(nameof(cmd));
                }
                
                cameraDepthRTHandle = null;
            }

            public void Dispose()
            {
                settings = null; // de-reference settings to allow them to be freed from memory
            }
        }

        [SerializeField] private FastOutlineSettings settings;
        [SerializeField] private ShaderResources shaders;
        private Material maskMaterial, outlineMaterial, outlineInstancedMaterial, clearMaterial;
        private FastOutlinePass fastOutlinePass;

        /// <summary>
        /// Called
        /// - When the Scriptable Renderer Feature loads the first time.
        /// - When you enable or disable the Scriptable Renderer Feature.
        /// - When you change a property in the Inspector window of the Renderer Feature.
        /// </summary>
        public override void Create()
        {
            if (settings == null) return;
            settings.OnSettingsChanged = null;
            settings.OnSettingsChanged += Create;

            shaders = new ShaderResources().Load();
            fastOutlinePass ??= new FastOutlinePass();
        }

        /// <summary>
        /// Called
        /// - Every frame, once for each camera.
        /// </summary>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings == null) return;

            // Don't render for some views.
            if (renderingData.cameraData.cameraType == CameraType.Preview
                || renderingData.cameraData.cameraType == CameraType.Reflection
                || renderingData.cameraData.cameraType == CameraType.SceneView && !settings.ShowInSceneView
#if UNITY_6000_0_OR_NEWER
                || UniversalRenderer.IsOffscreenDepthTexture(ref renderingData.cameraData))
#else
                )
#endif
                return;

            if (!CreateMaterials())
            {
                Debug.LogWarning("Not all required materials could be created. Fast Outline will not render.");
                return;
            }
            
            var render = fastOutlinePass.Setup(ref settings, ref maskMaterial, ref outlineMaterial, ref outlineInstancedMaterial, ref clearMaterial);
            if (render) renderer.EnqueuePass(fastOutlinePass);
        }
        
        #pragma warning disable 618, 672
        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            if (settings == null || renderingData.cameraData.cameraType == CameraType.SceneView && !settings.ShowInSceneView) return;

            fastOutlinePass.SetTarget(renderer.cameraDepthTargetHandle);
        }
        #pragma warning restore 618, 672
        
        /// <summary>
        /// Clean up resources allocated to the Scriptable Renderer Feature such as materials.
        /// </summary>
        override protected void Dispose(bool disposing)
        {
            fastOutlinePass?.Dispose();
            fastOutlinePass = null;
            DestroyMaterials();
        }
        
        private void OnDestroy()
        {
            settings = null; // de-reference settings to allow them to be freed from memory
            fastOutlinePass?.Dispose();
        }

        private void DestroyMaterials()
        {
            CoreUtils.Destroy(maskMaterial);
            CoreUtils.Destroy(outlineMaterial);
            CoreUtils.Destroy(outlineInstancedMaterial);
            CoreUtils.Destroy(clearMaterial);
        }

        private bool CreateMaterials()
        {
            if (maskMaterial == null)
            {
                maskMaterial = CoreUtils.CreateEngineMaterial(shaders.mask);
            }
            
            if (outlineMaterial == null)
            {
                outlineMaterial = CoreUtils.CreateEngineMaterial(shaders.outline);
            }
            
            if (outlineInstancedMaterial == null)
            {
                outlineInstancedMaterial = CoreUtils.CreateEngineMaterial(shaders.outlineInstanced);
            }
            
            if (clearMaterial == null)
            {
                clearMaterial = CoreUtils.CreateEngineMaterial(shaders.clear);
            }

            return maskMaterial != null && outlineMaterial != null && outlineInstancedMaterial != null && clearMaterial != null;
        }
    }
}