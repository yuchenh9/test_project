using System;
using System.Linq;
using Linework.Common.Utils;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
#if UNITY_6000_0_OR_NEWER
using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;
#endif
using UnityEngine.Rendering.Universal;

namespace Linework.WideOutline
{
    [ExcludeFromPreset]
    [DisallowMultipleRendererFeature("Wide Outline")]
#if UNITY_6000_0_OR_NEWER
    [SupportedOnRenderer(typeof(UniversalRendererData))]
#endif
    [Tooltip("Wide Outline renders an outline by generating a signed distance field (SDF) for each object and then sampling it. This creates consistent outlines that smoothly follows the shape of an object.")]
    [HelpURL("https://linework.ameye.dev/outlines/wide-outline")]
    public class WideOutline : ScriptableRendererFeature
    {
        private class WideOutlinePass : ScriptableRenderPass
        {
            private WideOutlineSettings settings;
            private Material mask, silhouetteBase, silhouetteInstancedBase, composite;
            private readonly ProfilingSampler maskSampler, silhouetteSampler, floodSampler, outlineSampler;

            public WideOutlinePass()
            {
                profilingSampler = new ProfilingSampler(nameof(WideOutlinePass));
                maskSampler = new ProfilingSampler(ShaderPassName.Mask);
                silhouetteSampler = new ProfilingSampler(ShaderPassName.Silhouette);
                floodSampler = new ProfilingSampler(ShaderPassName.Flood);
                outlineSampler = new ProfilingSampler(ShaderPassName.Outline);
            }
            
            public bool Setup(ref WideOutlineSettings wideOutlineSettings, ref Material maskMaterial, ref Material silhouetteMaterial, ref Material silhouetteInstancedMaterial, ref Material compositeMaterial, float renderScale)
            {
                settings = wideOutlineSettings;
                mask = maskMaterial;
                silhouetteBase = silhouetteMaterial;
                silhouetteInstancedBase = silhouetteInstancedMaterial;
                composite = compositeMaterial;
                renderPassEvent = (RenderPassEvent) wideOutlineSettings.InjectionPoint;

                foreach (var outline in settings.Outlines)
                {
                    if (outline.material == null || outline.materialInstanced == null)
                    {
                        outline.AssignMaterials(silhouetteBase, silhouetteInstancedBase);
                    }
                }

                foreach (var outline in settings.Outlines)
                {
                    if (!outline.IsActive())
                    {
                        continue;
                    }
                    
                    var silhouette = outline.gpuInstancing ? outline.materialInstanced : outline.material;
                    
                    silhouette.SetColor(CommonShaderPropertyId.OutlineColor, outline.color);
                    if (outline.occlusion == WideOutlineOcclusion.AsMask) silhouette.SetColor(CommonShaderPropertyId.OutlineColor, Color.clear);

                    if (outline.alphaCutout) silhouette.EnableKeyword(ShaderFeature.AlphaCutout);
                    else silhouette.DisableKeyword(ShaderFeature.AlphaCutout);
                    silhouette.SetTexture(CommonShaderPropertyId.AlphaCutoutTexture, outline.alphaCutoutTexture);
                    silhouette.SetFloat(CommonShaderPropertyId.AlphaCutoutThreshold, outline.alphaCutoutThreshold);

                    switch (outline.cullingMode)
                    {
                        case CullingMode.Off:
                            silhouette.SetFloat(CommonShaderPropertyId.CullMode, (float) CullMode.Off);
                            break;
                        case CullingMode.Back:
                            silhouette.SetFloat(CommonShaderPropertyId.CullMode, (float) CullMode.Back);
                            break;
                    }

                    if (settings.customDepthBuffer)
                    {
                        silhouette.SetFloat(CommonShaderPropertyId.ZTest, (float) CompareFunction.LessEqual);
                    }
                    else
                    {
                        switch (outline.occlusion)
                        {
                            case WideOutlineOcclusion.Always:
                                silhouette.SetFloat(CommonShaderPropertyId.ZTest, (float) CompareFunction.Always);
                                break;
                            case WideOutlineOcclusion.WhenOccluded:
                                silhouette.SetFloat(CommonShaderPropertyId.ZTest, (float) CompareFunction.Greater);
                                break;
                            case WideOutlineOcclusion.WhenNotOccluded:
                                silhouette.SetFloat(CommonShaderPropertyId.ZTest, (float) CompareFunction.LessEqual);
                                break;
                            case WideOutlineOcclusion.AsMask:
                                silhouette.SetFloat(CommonShaderPropertyId.ZTest, (float) CompareFunction.Always);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    silhouette.SetFloat(CommonShaderPropertyId.ZWrite, settings.customDepthBuffer ? 1.0f : 0.0f);
                }

                // Set outline material properties.
                var (sourceBlend, destinationBlend) = RenderUtils.GetSrcDstBlend(settings.blendMode);
                composite.SetInt(CommonShaderPropertyId.BlendModeSource, sourceBlend);
                composite.SetInt(CommonShaderPropertyId.BlendModeDestination, destinationBlend);
                composite.SetColor(ShaderPropertyId.OutlineOccludedColor, settings.occludedColor);
                composite.SetFloat(ShaderPropertyId.OutlineWidth, settings.width);
                composite.SetFloat(ShaderPropertyId.RenderScale, renderScale);
                if (settings.customDepthBuffer) composite.EnableKeyword(ShaderFeature.CustomDepth);
                else composite.DisableKeyword(ShaderFeature.CustomDepth);

                // Set custom material properties.
                // if (settings.materialType == MaterialType.Custom && settings.customMaterial != null)
                // {
                //     settings.customMaterial.SetFloat(ShaderPropertyId.OutlineWidth, settings.width);
                // }

                return settings.Outlines.Any(ShouldRenderOutline);
            }

            private static bool ShouldRenderOutline(Outline outline)
            {
                return outline.IsActive() && outline.occlusion != WideOutlineOcclusion.AsMask;
            }

            private static bool ShouldRenderStencilMask(Outline outline)
            {
                return outline.IsActive() && outline.occlusion == WideOutlineOcclusion.WhenOccluded;
            }

#if UNITY_6000_0_OR_NEWER
            private class PassData
            {
                internal readonly List<RendererListHandle> MaskRendererListHandles = new();
                internal readonly List<(RendererListHandle handle, bool vertexAnimated)> SilhouetteRendererListHandles = new();
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resourceData = frameData.Get<UniversalResourceData>();
                var cameraData = frameData.Get<UniversalCameraData>();

                // Ensure that the render pass doesn't blit from the back buffer.
                if (resourceData.isActiveTargetBackBuffer) return;

                CreateRenderGraphTextures(renderGraph, cameraData, out var silhouetteHandle, out var silhouetteDepthHandle, out var pingHandle, out var pongHandle);
                if (!silhouetteHandle.IsValid() || !silhouetteDepthHandle.IsValid() || !pingHandle.IsValid() || !pongHandle.IsValid()) return;
                
                // 1. Mask.
                // -> Render a mask to the stencil buffer.
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(ShaderPassName.Mask, out var passData))
                {
                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);

                    InitMaskRendererLists(renderGraph, frameData, ref passData);
                    foreach (var rendererListHandle in passData.MaskRendererListHandles)
                    {
                        builder.UseRendererList(rendererListHandle);
                    }

                    builder.AllowPassCulling(true);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        foreach (var handle in data.MaskRendererListHandles)
                        {
                            context.cmd.DrawRendererList(handle);
                        }
                    });
                }

                // 2. Silhouette.
                // -> Render a silhouette.
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(ShaderPassName.Silhouette, out var passData))
                {
                    builder.SetRenderAttachment(silhouetteHandle, 0);
                    builder.SetRenderAttachmentDepth(settings.customDepthBuffer ? silhouetteDepthHandle : resourceData.activeDepthTexture);

                    builder.SetGlobalTextureAfterPass(silhouetteHandle, ShaderPropertyId.SilhouetteBuffer);
                    if (settings.customDepthBuffer) builder.SetGlobalTextureAfterPass(silhouetteDepthHandle, ShaderPropertyId.SilhouetteDepthBuffer);
                    
                    InitSilhouetteRendererLists(renderGraph, frameData, ref passData);
                    foreach (var rendererListHandle in passData.SilhouetteRendererListHandles)
                    {
                        builder.UseRendererList(rendererListHandle.handle);
                    }
                    
                    builder.AllowGlobalStateModification(true); // vertex animation
                    builder.AllowPassCulling(true);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        foreach (var handle in data.SilhouetteRendererListHandles)
                        {
                            if (handle.vertexAnimated)
                            {
                                context.cmd.EnableKeyword(Keyword.OutlineColor);
                            }
                            
                            context.cmd.DrawRendererList(handle.handle);
                            
                            if (handle.vertexAnimated)
                            {
                                context.cmd.DisableKeyword(Keyword.OutlineColor);
                            }
                        }
                    });
                }
                
                // 3. Flood.
                // -> Flood the silhouette.
                using (var builder = renderGraph.AddUnsafePass<PassData>(ShaderPassName.Flood, out _))
                {
                    builder.UseTexture(silhouetteHandle);
                    builder.UseTexture(pingHandle, AccessFlags.ReadWrite);
                    builder.UseTexture(pongHandle, AccessFlags.ReadWrite);
                
                    builder.AllowPassCulling(true);
                    
                    builder.SetRenderFunc((PassData _, UnsafeGraphContext context) =>
                    {
                        var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
                
                        Blitter.BlitCameraTexture(cmd, silhouetteHandle, pingHandle, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, composite, ShaderPass.FloodInit);
                        
                        var width = settings.width * cameraData.renderScale;
                        var numberOfMips = Mathf.CeilToInt(Mathf.Log(width + 1.0f, 2.0f));
                
                        for (var i = numberOfMips - 1; i >= 0; i--)
                        {
                            var stepWidth = Mathf.Pow(2, i) + 0.5f;
                
                            cmd.SetGlobalVector(ShaderPropertyId.AxisWidthId, new Vector2(stepWidth, 0f));
                            Blitter.BlitCameraTexture(cmd, pingHandle, pongHandle, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, composite, ShaderPass.FloodJump);
                            cmd.SetGlobalVector(ShaderPropertyId.AxisWidthId, new Vector2(0f, stepWidth));
                            Blitter.BlitCameraTexture(cmd, pongHandle, pingHandle, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, composite, ShaderPass.FloodJump);
                        }
                    });
                }
                
                // 4. Outline.
                // -> Render an outline.
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(ShaderPassName.Outline, out _))
                {
                    builder.UseTexture(pingHandle);

                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                    builder.SetRenderAttachmentDepth(settings.customDepthBuffer ? silhouetteDepthHandle : resourceData.activeDepthTexture);
                    
                    builder.AllowPassCulling(true);
                    
                    builder.SetRenderFunc((PassData _, RasterGraphContext context) =>
                    {
                        Blitter.BlitTexture(context.cmd, pingHandle, Vector2.one, composite, ShaderPass.Outline);
                    });
                }
            }

            private void InitMaskRendererLists(RenderGraph renderGraph, ContextContainer frameData, ref PassData passData)
            {
                passData.MaskRendererListHandles.Clear();

                var renderingData = frameData.Get<UniversalRenderingData>();
                var cameraData = frameData.Get<UniversalCameraData>();
                var lightData = frameData.Get<UniversalLightData>();

                var sortingCriteria = cameraData.defaultOpaqueSortFlags;
                var renderQueueRange = RenderQueueRange.opaque;

                var i = 0;
                foreach (var outline in settings.Outlines)
                {
                    if (!ShouldRenderStencilMask(outline))
                    {
                        i++;
                        continue;
                    }

                    var drawingSettings = RenderingUtils.CreateDrawingSettings(RenderUtils.DefaultShaderTagIds, renderingData, cameraData, lightData, sortingCriteria);
                    drawingSettings.overrideMaterial = mask;
                    drawingSettings.overrideShaderPassIndex = ShaderPass.Mask;

                    var filteringSettings = new FilteringSettings(renderQueueRange, -1, outline.RenderingLayer);
                    var renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

                    var blendState = BlendState.defaultValue;
                    blendState.blendState0 = new RenderTargetBlendState(0);
                    renderStateBlock.blendState = blendState;

                    // Set stencil state.
                    var stencilState = StencilState.defaultValue;
                    stencilState.enabled = true;
                    stencilState.SetCompareFunction(CompareFunction.Always);
                    stencilState.SetPassOperation(StencilOp.Replace);
                    stencilState.SetFailOperation(StencilOp.Keep);
                    stencilState.SetZFailOperation(StencilOp.Keep);
                    stencilState.writeMask = (byte) (1 << i);
                    renderStateBlock.mask |= RenderStateMask.Stencil;
                    renderStateBlock.stencilReference = 1 << i;
                    renderStateBlock.stencilState = stencilState;

                    var handle = new RendererListHandle();
                    RenderUtils.CreateRendererListWithRenderStateBlock(renderGraph, ref renderingData.cullResults, drawingSettings, filteringSettings, renderStateBlock,
                        ref handle);
                    passData.MaskRendererListHandles.Add(handle);
                }
            }

            private void InitSilhouetteRendererLists(RenderGraph renderGraph, ContextContainer frameData, ref PassData passData)
            {
                passData.SilhouetteRendererListHandles.Clear();

                var universalRenderingData = frameData.Get<UniversalRenderingData>();
                var cameraData = frameData.Get<UniversalCameraData>();
                var lightData = frameData.Get<UniversalLightData>();

                var sortingCriteria = cameraData.defaultOpaqueSortFlags;
                var renderQueueRange = RenderQueueRange.opaque;

                var i = 0;
                foreach (var outline in settings.Outlines)
                {
                    if (!outline.IsActive())
                    {
                        i++;
                        continue;
                    }

                    var drawingSettings = RenderingUtils.CreateDrawingSettings(RenderUtils.DefaultShaderTagIds, universalRenderingData, cameraData, lightData, sortingCriteria);
                    if (!outline.vertexAnimation)
                    {
                        drawingSettings.overrideMaterial = outline.gpuInstancing ? outline.materialInstanced : outline.material;
                        drawingSettings.overrideMaterialPassIndex = ShaderPass.Silhouette;
                        drawingSettings.enableInstancing = outline.gpuInstancing;
                    }
                    
                    var filteringSettings = new FilteringSettings(renderQueueRange, -1, outline.RenderingLayer);

                    var renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

                    var stencilState = StencilState.defaultValue;
                    stencilState.enabled = true;
                    stencilState.SetCompareFunction(outline.occlusion == WideOutlineOcclusion.WhenOccluded ? CompareFunction.NotEqual : CompareFunction.Always);
                    stencilState.SetPassOperation(StencilOp.Replace);
                    stencilState.SetFailOperation(StencilOp.Keep);
                    stencilState.SetZFailOperation(outline.closedLoop ? StencilOp.Keep : StencilOp.Replace);
                    stencilState.readMask = (byte) (1 << i);
                    stencilState.writeMask = (byte) (1 << i);
                    renderStateBlock.mask |= RenderStateMask.Stencil;
                    renderStateBlock.stencilReference = 1 << i;
                    renderStateBlock.stencilState = stencilState;
                   
                    if (outline.vertexAnimation)
                    {
                        var depthState = DepthState.defaultValue;
                        switch (outline.occlusion)
                        {
                            case WideOutlineOcclusion.Always:
                                depthState.compareFunction = CompareFunction.Always;
                                break;
                            case WideOutlineOcclusion.WhenOccluded:
                                depthState.compareFunction = CompareFunction.Greater;
                                break;
                            case WideOutlineOcclusion.WhenNotOccluded:
                                depthState.compareFunction = CompareFunction.LessEqual;
                                break;
                            case WideOutlineOcclusion.AsMask:
                                depthState.compareFunction = CompareFunction.Always;
                                break;
                        }
                        renderStateBlock.mask |= RenderStateMask.Depth;
                        renderStateBlock.depthState = depthState;
                    }

                    var handle = new RendererListHandle();
                    RenderUtils.CreateRendererListWithRenderStateBlock(renderGraph, ref universalRenderingData.cullResults, drawingSettings, filteringSettings, renderStateBlock, ref handle);
                    passData.SilhouetteRendererListHandles.Add((handle, outline.vertexAnimation));

                    i++;
                }
            }

            private static void CreateRenderGraphTextures(RenderGraph renderGraph, UniversalCameraData cameraData,
                out TextureHandle silhouetteHandle,
                out TextureHandle silhouetteDepthHandle,
                out TextureHandle pingHandle,
                out TextureHandle pongHandle)
            {
                // Silhouette buffer.
                var silhouetteDescriptor = cameraData.cameraTargetDescriptor;
                silhouetteDescriptor.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
                silhouetteDescriptor.depthBufferBits = (int) DepthBits.None;
                silhouetteDescriptor.sRGB = false;
                silhouetteDescriptor.useMipMap = false;
                silhouetteDescriptor.autoGenerateMips = false;
                //silhouetteDescriptor.bindMS = silhouetteDescriptor.msaaSamples > 1;
                silhouetteHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, silhouetteDescriptor, Buffer.Silhouette, false);

                // Silhouette depth buffer.
                var silhouetteDepthDescriptor = cameraData.cameraTargetDescriptor;
                silhouetteDepthDescriptor.graphicsFormat = GraphicsFormat.None;
                silhouetteDepthDescriptor.depthBufferBits = (int) DepthBits.Depth32;
                silhouetteDepthHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, silhouetteDepthDescriptor, Buffer.SilhouetteDepth, false);

                // Ping pong buffers.
                var pingPongDescriptor = cameraData.cameraTargetDescriptor;
                pingPongDescriptor.graphicsFormat = SystemInfo.IsFormatSupported(GraphicsFormat.R16G16_SNorm, GraphicsFormatUsage.Render)
                    ? GraphicsFormat.R16G16_SNorm
                    : GraphicsFormat.R32G32_SFloat;
                pingPongDescriptor.depthBufferBits = (int) DepthBits.None;
                pingHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, pingPongDescriptor, Buffer.Ping, false);
                pongHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, pingPongDescriptor, Buffer.Pong, false);
            }
#endif
            private RTHandle cameraDepthRTHandle, silhouetteRTHandle, silhouetteDepthRTHandle, pingRTHandle, pongRTHandle;

            #pragma warning disable 618, 672
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                ConfigureTarget(silhouetteRTHandle, settings.customDepthBuffer ? silhouetteDepthRTHandle : renderingData.cameraData.renderer.cameraDepthTargetHandle);
                ConfigureClear(settings.customDepthBuffer ? ClearFlag.All : ClearFlag.Color, Color.clear);
            }

            public void CreateHandles(RenderingData renderingData)
            {
                const float renderTextureScale = 1.0f;
                var width = (int) (renderingData.cameraData.cameraTargetDescriptor.width * renderTextureScale);
                var height = (int) (renderingData.cameraData.cameraTargetDescriptor.height * renderTextureScale);

                var descriptor = new RenderTextureDescriptor(width, height)
                {
                    dimension = TextureDimension.Tex2D,
                    msaaSamples = 1,
                    sRGB = false,
                    useMipMap = false,
                    autoGenerateMips = false,
                    graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm,
                    depthBufferBits = (int) DepthBits.None,
                    colorFormat = RenderTextureFormat.Default
                };
                RenderingUtils.ReAllocateIfNeeded(ref silhouetteRTHandle, descriptor, FilterMode.Point, TextureWrapMode.Clamp, name: Buffer.Silhouette);

                // Silhouette depth buffer.
                var silhouetteDepthDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                silhouetteDepthDescriptor.graphicsFormat = GraphicsFormat.None;
                silhouetteDepthDescriptor.depthBufferBits = (int) DepthBits.Depth32;
                silhouetteDepthDescriptor.msaaSamples = 1;
                RenderingUtils.ReAllocateIfNeeded(ref silhouetteDepthRTHandle, silhouetteDepthDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: Buffer.SilhouetteDepth);

                // Ping pong buffers.
                var pingPongDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                pingPongDescriptor.graphicsFormat = SystemInfo.IsFormatSupported(GraphicsFormat.R16G16_SNorm, FormatUsage.Render)
                    ? GraphicsFormat.R16G16_SNorm
                    : GraphicsFormat.R32G32_SFloat;
                pingPongDescriptor.depthBufferBits = (int) DepthBits.None;
                pingPongDescriptor.msaaSamples = 1;
                RenderingUtils.ReAllocateIfNeeded(ref pingRTHandle, pingPongDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: Buffer.Ping);
                RenderingUtils.ReAllocateIfNeeded(ref pongRTHandle, pingPongDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: Buffer.Pong);
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

                    var maskIndex = 0;
                    foreach (var outline in settings.Outlines)
                    {
                        if (!ShouldRenderStencilMask(outline))
                        {
                            maskIndex++;
                            continue;
                        }

                        var drawingSettings = RenderingUtils.CreateDrawingSettings(RenderUtils.DefaultShaderTagIds, ref renderingData, sortingCriteria);
                        drawingSettings.overrideMaterial = mask;

                        var filteringSettings = new FilteringSettings(renderQueueRange, -1, outline.RenderingLayer);
                        var renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

                        var blendState = BlendState.defaultValue;
                        blendState.blendState0 = new RenderTargetBlendState(0);
                        renderStateBlock.blendState = blendState;

                        var stencilState = StencilState.defaultValue;
                        stencilState.enabled = true;
                        stencilState.SetCompareFunction(CompareFunction.Always);
                        stencilState.SetPassOperation(StencilOp.Replace);
                        stencilState.SetFailOperation(StencilOp.Keep);
                        stencilState.SetZFailOperation(StencilOp.Keep);
                        stencilState.writeMask = (byte) (1 << maskIndex);
                        renderStateBlock.mask |= RenderStateMask.Stencil;
                        renderStateBlock.stencilReference = 1 << maskIndex;
                        renderStateBlock.stencilState = stencilState;

                        context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);

                        maskIndex++;
                    }
                }

                context.ExecuteCommandBuffer(maskCmd);
                CommandBufferPool.Release(maskCmd);

                // 2. Silhouette.
                // -> Render a silhouette.
                var silhouetteCmd = CommandBufferPool.Get();

                using (new ProfilingScope(silhouetteCmd, silhouetteSampler))
                {
                    CoreUtils.SetRenderTarget(silhouetteCmd, silhouetteRTHandle, settings.customDepthBuffer ? silhouetteDepthRTHandle : renderingData.cameraData.renderer.cameraDepthTargetHandle);

                    context.ExecuteCommandBuffer(silhouetteCmd);
                    silhouetteCmd.Clear();

                    var sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
                    var renderQueueRange = RenderQueueRange.opaque;

                    var i = 0;
                    foreach (var outline in settings.Outlines)
                    {
                        if (!outline.IsActive())
                        {
                            i++;
                            continue;
                        }

                        var drawingSettings = RenderingUtils.CreateDrawingSettings(RenderUtils.DefaultShaderTagIds, ref renderingData, sortingCriteria);
                        if (!outline.vertexAnimation)
                        {
                            drawingSettings.overrideMaterial = outline.gpuInstancing ? outline.materialInstanced : outline.material;
                            drawingSettings.overrideMaterialPassIndex = ShaderPass.Silhouette;
                            drawingSettings.enableInstancing = outline.gpuInstancing;
                        }
                        
                        var filteringSettings = new FilteringSettings(renderQueueRange, -1, outline.RenderingLayer);
                        
                        var renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

                        var stencilState = StencilState.defaultValue;
                        stencilState.enabled = true;
                        stencilState.SetCompareFunction(outline.occlusion == WideOutlineOcclusion.WhenOccluded ? CompareFunction.NotEqual : CompareFunction.Always);
                        stencilState.SetPassOperation(StencilOp.Replace);
                        stencilState.SetFailOperation(StencilOp.Keep);
                        stencilState.SetZFailOperation(outline.closedLoop ? StencilOp.Keep : StencilOp.Replace);
                        stencilState.readMask = (byte) (1 << i);
                        stencilState.writeMask = (byte) (1 << i);
                        renderStateBlock.mask |= RenderStateMask.Stencil;
                        renderStateBlock.stencilReference = 1 << i;
                        renderStateBlock.stencilState = stencilState;

                        if (outline.vertexAnimation)
                        {
                            var depthState = DepthState.defaultValue;
                            switch (outline.occlusion)
                            {
                                case WideOutlineOcclusion.Always:
                                    depthState.compareFunction = CompareFunction.Always;
                                    break;
                                case WideOutlineOcclusion.WhenOccluded:
                                    depthState.compareFunction = CompareFunction.Greater;
                                    break;
                                case WideOutlineOcclusion.WhenNotOccluded:
                                    depthState.compareFunction = CompareFunction.LessEqual;
                                    break;
                                case WideOutlineOcclusion.AsMask:
                                    depthState.compareFunction = CompareFunction.Always;
                                    break;
                            }
                            renderStateBlock.mask |= RenderStateMask.Depth;
                            renderStateBlock.depthState = depthState;
                        }
                        
                        var blendState = BlendState.defaultValue;
                        blendState.blendState0 = new RenderTargetBlendState(0);
                        renderStateBlock.blendState = blendState;

                        if (outline.vertexAnimation) silhouetteCmd.EnableKeyword(Keyword.OutlineColor);
                        context.ExecuteCommandBuffer(silhouetteCmd);
                        context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);
                        if (outline.vertexAnimation) silhouetteCmd.DisableKeyword(Keyword.OutlineColor);
                        context.ExecuteCommandBuffer(silhouetteCmd);
                        
                        i++;
                    }
                }

                if (settings.customDepthBuffer) silhouetteCmd.SetGlobalTexture(ShaderPropertyId.SilhouetteDepthBuffer, silhouetteDepthRTHandle.nameID);
                silhouetteCmd.SetGlobalTexture(ShaderPropertyId.SilhouetteBuffer, silhouetteRTHandle.nameID);
                context.ExecuteCommandBuffer(silhouetteCmd);
                CommandBufferPool.Release(silhouetteCmd);

                // 3. Flood.
                // -> Flood the silhouette.
                var floodCmd = CommandBufferPool.Get();

                using (new ProfilingScope(floodCmd, floodSampler))
                {
                    context.ExecuteCommandBuffer(floodCmd);
                    floodCmd.Clear();

                    Blitter.BlitCameraTexture(floodCmd, silhouetteRTHandle, pingRTHandle, composite, ShaderPass.FloodInit);

                    var width = settings.width * renderingData.cameraData.renderScale;
                    var numberOfMips = Mathf.CeilToInt(Mathf.Log(width + 1.0f, 2f));

                    for (var passIndex = numberOfMips - 1; passIndex >= 0; passIndex--)
                    {
                        var stepWidth = Mathf.Pow(2, passIndex) + 0.5f;

                        floodCmd.SetGlobalVector(ShaderPropertyId.AxisWidthId, new Vector2(stepWidth, 0f));
                        Blitter.BlitCameraTexture(floodCmd, pingRTHandle, pongRTHandle, composite, ShaderPass.FloodJump);
                        floodCmd.SetGlobalVector(ShaderPropertyId.AxisWidthId, new Vector2(0f, stepWidth));
                        Blitter.BlitCameraTexture(floodCmd, pongRTHandle, pingRTHandle, composite, ShaderPass.FloodJump);
                    }
                }

                context.ExecuteCommandBuffer(floodCmd);
                CommandBufferPool.Release(floodCmd);

                // 4. Outline.
                // -> Render an outline.
                var outlineCmd = CommandBufferPool.Get();

                using (new ProfilingScope(outlineCmd, outlineSampler))
                {
                    context.ExecuteCommandBuffer(outlineCmd);
                    outlineCmd.Clear();

                    CoreUtils.SetRenderTarget(outlineCmd, renderingData.cameraData.renderer.cameraColorTargetHandle,
                        settings.customDepthBuffer
                            ? silhouetteDepthRTHandle
                            : cameraDepthRTHandle); // if using cameraColorRTHandle this does not render in scene view when rendering after post-processing with post-processing enabled
                    Blitter.BlitTexture(outlineCmd, pingRTHandle, Vector2.one, composite, ShaderPass.Outline);
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

                silhouetteRTHandle?.Release();
                silhouetteDepthRTHandle?.Release();
                pingRTHandle?.Release();
                pongRTHandle?.Release();
            }
        }

        [SerializeField] private WideOutlineSettings settings;
        [SerializeField] private ShaderResources shaders;
        private Material maskMaterial, silhouetteMaterial, silhouetteInstancedMaterial, outlineMaterial;
        private WideOutlinePass wideOutlinePass;

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
            wideOutlinePass ??= new WideOutlinePass();
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
                Debug.LogWarning("Not all required materials could be created. Wide Outline will not render.");
                return;
            }

            var render = wideOutlinePass.Setup(ref settings, ref maskMaterial, ref silhouetteMaterial, ref silhouetteInstancedMaterial, ref outlineMaterial, renderingData.cameraData.renderScale);
            if (render) renderer.EnqueuePass(wideOutlinePass);
        }

        #pragma warning disable 618, 672
        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            if (settings == null) return;

            wideOutlinePass.CreateHandles(renderingData);
            wideOutlinePass.ConfigureInput(ScriptableRenderPassInput.Color);
            wideOutlinePass.ConfigureInput(ScriptableRenderPassInput.Depth);
            wideOutlinePass.SetTarget(renderer.cameraDepthTargetHandle);
        }
        #pragma warning restore 618, 672

        /// <summary>
        /// Clean up resources allocated to the Scriptable Renderer Feature such as materials.
        /// </summary>
        override protected void Dispose(bool disposing)
        {
            wideOutlinePass?.Dispose();
            wideOutlinePass = null;
            DestroyMaterials();
        }

        private void OnDestroy()
        {
            settings = null; // de-reference settings to allow them to be freed from memory
            wideOutlinePass?.Dispose();
        }

        private void DestroyMaterials()
        {
            CoreUtils.Destroy(maskMaterial);
            CoreUtils.Destroy(silhouetteMaterial);
            CoreUtils.Destroy(silhouetteInstancedMaterial);
            CoreUtils.Destroy(outlineMaterial);
        }

        private bool CreateMaterials()
        {
            if (maskMaterial == null)
            {
                maskMaterial = CoreUtils.CreateEngineMaterial(shaders.mask);
            }

            if (silhouetteMaterial == null)
            {
                silhouetteMaterial = CoreUtils.CreateEngineMaterial(shaders.silhouette);
            }
            
            if (silhouetteInstancedMaterial == null)
            {
                silhouetteInstancedMaterial = CoreUtils.CreateEngineMaterial(shaders.silhouetteInstanced);
            }

            if (outlineMaterial == null)
            {
                outlineMaterial = CoreUtils.CreateEngineMaterial(shaders.outline);
            }
            
            return maskMaterial != null && silhouetteMaterial != null && silhouetteInstancedMaterial != null && outlineMaterial != null;
        }
    }
}