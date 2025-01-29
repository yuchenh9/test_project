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

namespace Linework.SurfaceFill
{
    [ExcludeFromPreset]
    [DisallowMultipleRendererFeature("Surface Fill")]
#if UNITY_6000_0_OR_NEWER
    [SupportedOnRenderer(typeof(UniversalRendererData))]
#endif
    [Tooltip("Surface Fill renders fills by rendering an object with a fill material.")]
    [HelpURL("https://linework.ameye.dev/outlines/surface-fill")]
    public class SurfaceFill : ScriptableRendererFeature
    {
        private class SurfaceFillPass : ScriptableRenderPass
        {
            private SurfaceFillSettings settings;
            private Material mask, fillBase;
            private RenderStateBlock fillRenderStateBlock;
            private int lastActiveFillIndex;
            private readonly ProfilingSampler maskSampler, fillSampler;
            
            public SurfaceFillPass()
            {
                profilingSampler = new ProfilingSampler(nameof(SurfaceFillPass));
                maskSampler = new ProfilingSampler(ShaderPassName.Mask);
                fillSampler = new ProfilingSampler(ShaderPassName.Fill);
            }

            public bool Setup(ref SurfaceFillSettings surfaceFillSettings, ref Material maskMaterial, ref Material fillMaterial)
            {
                settings = surfaceFillSettings;
                mask = maskMaterial;
                fillBase = fillMaterial;
                renderPassEvent = (RenderPassEvent) surfaceFillSettings.InjectionPoint;

                foreach (var fill in settings.Fills)
                {
                    if (fill.material == null)
                    {
                        fill.AssignMaterial(fillBase);
                    }
                }
               
                var i = 0;
                foreach (var fill in settings.Fills)
                {
                    if (!fill.IsActive())
                    {
                        i++;
                        continue;
                    }
                    
                    // FIXME: for some reason this is needed to make activating/de-activating fills work, but GC ALLOC
                    fill.material.CopyPropertiesFromMaterial(fillBase); 
                    
                    var (srcBlend, dstBlend) = RenderUtils.GetSrcDstBlend(fill.blendMode);
                    fill.material.SetInt(CommonShaderPropertyId.FullScreenColorBlendModeSource, srcBlend);
                    fill.material.SetInt(CommonShaderPropertyId.FullScreenColorBlendModeDestination, dstBlend);
                    
                    mask.DisableKeyword(ShaderFeature.AlphaCutout);
                    // TODO: enable in future update
                    // if (fill.alphaCutout) mask.EnableKeyword(ShaderFeature.AlphaCutout);
                    // else mask.DisableKeyword(ShaderFeature.AlphaCutout);
                    // mask.SetTexture(CommonShaderPropertyId.AlphaCutoutTexture, fill.alphaCutoutTexture);
                    // mask.SetFloat(CommonShaderPropertyId.AlphaCutoutThreshold, fill.alphaCutoutThreshold);
                    
                    switch (fill.channel)
                    {
                        case Channel.R:
                            fill.material.EnableKeyword(ShaderFeature.ChannelR);
                            fill.material.DisableKeyword(ShaderFeature.ChannelG);
                            fill.material.DisableKeyword(ShaderFeature.ChannelB);
                            fill.material.DisableKeyword(ShaderFeature.ChannelA);
                            break;
                        case Channel.G:
                            fill.material.DisableKeyword(ShaderFeature.ChannelR);
                            fill.material.EnableKeyword(ShaderFeature.ChannelG);
                            fill.material.DisableKeyword(ShaderFeature.ChannelB);
                            fill.material.DisableKeyword(ShaderFeature.ChannelA);
                            break;
                        case Channel.B:
                            fill.material.DisableKeyword(ShaderFeature.ChannelR);
                            fill.material.DisableKeyword(ShaderFeature.ChannelG);
                            fill.material.EnableKeyword(ShaderFeature.ChannelB);
                            fill.material.DisableKeyword(ShaderFeature.ChannelA);
                            break;
                        case Channel.A:
                            fill.material.DisableKeyword(ShaderFeature.ChannelR);
                            fill.material.DisableKeyword(ShaderFeature.ChannelG);
                            fill.material.DisableKeyword(ShaderFeature.ChannelB);
                            fill.material.EnableKeyword(ShaderFeature.ChannelA);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    switch (fill.pattern)
                    {
                        case Pattern.Solid:
                            fill.material.EnableKeyword(ShaderFeature.PatternSolid);
                            fill.material.DisableKeyword(ShaderFeature.PatternCheckerboard);
                            fill.material.DisableKeyword(ShaderFeature.PatternDots);
                            fill.material.DisableKeyword(ShaderFeature.PatternStripes);
                            fill.material.DisableKeyword(ShaderFeature.PatternGlow);
                            fill.material.DisableKeyword(ShaderFeature.PatternTexture);
                            break;
                        case Pattern.Checkerboard:
                            fill.material.DisableKeyword(ShaderFeature.PatternSolid);
                            fill.material.EnableKeyword(ShaderFeature.PatternCheckerboard);
                            fill.material.DisableKeyword(ShaderFeature.PatternDots);
                            fill.material.DisableKeyword(ShaderFeature.PatternStripes);
                            fill.material.DisableKeyword(ShaderFeature.PatternGlow);
                            fill.material.DisableKeyword(ShaderFeature.PatternTexture);
                            break;
                        case Pattern.Dots:
                            fill.material.DisableKeyword(ShaderFeature.PatternSolid);
                            fill.material.DisableKeyword(ShaderFeature.PatternCheckerboard);
                            fill.material.EnableKeyword(ShaderFeature.PatternDots);
                            fill.material.DisableKeyword(ShaderFeature.PatternStripes);
                            fill.material.DisableKeyword(ShaderFeature.PatternGlow);
                            fill.material.DisableKeyword(ShaderFeature.PatternTexture);
                            break;
                        case Pattern.Stripes:
                            fill.material.DisableKeyword(ShaderFeature.PatternSolid);
                            fill.material.DisableKeyword(ShaderFeature.PatternCheckerboard);
                            fill.material.DisableKeyword(ShaderFeature.PatternDots);
                            fill.material.EnableKeyword(ShaderFeature.PatternStripes);
                            fill.material.DisableKeyword(ShaderFeature.PatternGlow);
                            fill.material.DisableKeyword(ShaderFeature.PatternTexture);
                            break;
                        case Pattern.Glow:
                            fill.material.DisableKeyword(ShaderFeature.PatternSolid);
                            fill.material.DisableKeyword(ShaderFeature.PatternCheckerboard);
                            fill.material.DisableKeyword(ShaderFeature.PatternDots);
                            fill.material.DisableKeyword(ShaderFeature.PatternStripes);
                            fill.material.EnableKeyword(ShaderFeature.PatternGlow);
                            fill.material.DisableKeyword(ShaderFeature.PatternTexture);
                            break;
                        case Pattern.Texture:
                            fill.material.DisableKeyword(ShaderFeature.PatternSolid);
                            fill.material.DisableKeyword(ShaderFeature.PatternCheckerboard);
                            fill.material.DisableKeyword(ShaderFeature.PatternDots);
                            fill.material.DisableKeyword(ShaderFeature.PatternStripes);
                            fill.material.DisableKeyword(ShaderFeature.PatternGlow);
                            fill.material.EnableKeyword(ShaderFeature.PatternTexture);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    fill.material.SetColor(ShaderPropertyId.PrimaryColor, fill.primaryColor);
                    fill.material.SetColor(ShaderPropertyId.SecondaryColor, fill.secondaryColor);
                    fill.material.SetFloat(ShaderPropertyId.FrequencyX, fill.frequencyX);
                    fill.material.SetFloat(ShaderPropertyId.FrequencyY, fill.frequencyY);
                    fill.material.SetFloat(ShaderPropertyId.Density, fill.density);
                    if(fill.pattern == Pattern.Texture) fill.material.SetFloat(ShaderPropertyId.Rotation, fill.rotation * 0.5f);
                    else fill.material.SetFloat(ShaderPropertyId.Rotation, fill.rotation);
                    fill.material.SetFloat(ShaderPropertyId.Direction, fill.direction);
                    fill.material.SetFloat(ShaderPropertyId.Offset, fill.offset);
                    fill.material.SetFloat(ShaderPropertyId.Softness, fill.softness);
                    fill.material.SetFloat(ShaderPropertyId.Power, fill.power);
                    fill.material.SetFloat(ShaderPropertyId.Width, fill.width);
                    fill.material.SetFloat(ShaderPropertyId.Speed, fill.speed);
                    fill.material.SetTexture(ShaderPropertyId.Texture, fill.texture);
                    fill.material.SetFloat(ShaderPropertyId.Scale, fill.scale);

                    // Set stencil properties for fill.
                    fill.material.SetFloat(CommonShaderPropertyId.FullScreenStencilComparison, (float) CompareFunction.Equal);
                    fill.material.SetFloat(CommonShaderPropertyId.FullScreenStencilReference, 1 << i);
                    fill.material.SetFloat(CommonShaderPropertyId.FullScreenStencilReadMask, 1 << i);

                    if (fill.IsActive()) lastActiveFillIndex = i;

                    i++;
                }

                return settings.Fills.Any(fill => fill.IsActive());
            }
            

#if UNITY_6000_0_OR_NEWER
            private class PassData
            {
                internal readonly List<RendererListHandle> MaskRendererListHandles = new();
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

                    InitMaskRendererLists(renderGraph, frameData, ref passData);
                    foreach (var rendererListHandle in passData.MaskRendererListHandles)
                    {
                        builder.UseRendererList(rendererListHandle);
                    }

                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        foreach (var handle in data.MaskRendererListHandles)
                        {
                            context.cmd.DrawRendererList(handle);
                        }
                    });
                }

                // 2. Fill.
                // -> Render a fill.
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(ShaderPassName.Fill, out _))
                {
                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);

                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc((PassData _, RasterGraphContext context) =>
                    {
                        var i = 0;
                        foreach (var fill in settings.Fills)
                        {
                            if (!fill.IsActive())
                            {
                                i++;
                                continue;
                            }

                            // If this is the last render operation, clear the stencil.
                            if (i == lastActiveFillIndex)
                            {
                                fill.material.SetFloat(CommonShaderPropertyId.FullScreenStencilPass, (float) StencilOp.Zero);
                                fill.material.SetFloat(CommonShaderPropertyId.FullScreenStencilFail, (float) StencilOp.Zero);
                            }
                            
                            Blitter.BlitTexture(context.cmd, Vector2.one, fill.material, 0);
                            
                            i++;
                        }
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
                foreach (var fill in settings.Fills)
                {
                    if (!fill.IsActive())
                    {
                        i++;
                        continue;
                    }

                    var drawingSettings = RenderingUtils.CreateDrawingSettings(RenderUtils.DefaultShaderTagIds, renderingData, cameraData, lightData, sortingCriteria);
                    drawingSettings.overrideMaterial = mask;

                    var filteringSettings = new FilteringSettings(renderQueueRange, -1, fill.RenderingLayer);
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
                    stencilState.writeMask = (byte) (1 << i);
                    renderStateBlock.mask |= RenderStateMask.Stencil;
                    renderStateBlock.stencilReference = 1 << i;
                    renderStateBlock.stencilState = stencilState;
                    
                    renderStateBlock.mask |= RenderStateMask.Depth;
                    renderStateBlock.depthState = fill.occlusion switch
                    {
                        Occlusion.Always => new DepthState(false, CompareFunction.Always),
                        Occlusion.WhenOccluded => new DepthState(false, CompareFunction.Greater),
                        Occlusion.WhenNotOccluded => new DepthState(false, CompareFunction.LessEqual),
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    var handle = new RendererListHandle();
                    RenderUtils.CreateRendererListWithRenderStateBlock(renderGraph, ref renderingData.cullResults, drawingSettings, filteringSettings, renderStateBlock,
                        ref handle);
                    passData.MaskRendererListHandles.Add(handle);

                    // Mask out again to fix self-occlusion.
                    if (fill.occlusion is Occlusion.WhenOccluded)
                    {
                        renderStateBlock.depthState = new DepthState(false, CompareFunction.LessEqual);
                        renderStateBlock.stencilReference = 0;
                        stencilState.SetPassOperation(StencilOp.Replace);
                        renderStateBlock.stencilState = stencilState;

                        var handle2 = new RendererListHandle();
                        RenderUtils.CreateRendererListWithRenderStateBlock(renderGraph, ref renderingData.cullResults, drawingSettings, filteringSettings, renderStateBlock,
                            ref handle2);
                        passData.MaskRendererListHandles.Add(handle2);
                    }

                    i++;
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
                    
                    var maskIndex = 0;
                    foreach (var fill in settings.Fills)
                    {
                        if (!fill.IsActive())
                        {
                            maskIndex++;
                            continue;
                        }

                        var drawingSettings = RenderingUtils.CreateDrawingSettings(RenderUtils.DefaultShaderTagIds, ref renderingData, sortingCriteria);
                        drawingSettings.overrideMaterial = mask;
                        drawingSettings.overrideShaderPassIndex = ShaderPass.Mask;
                        
                        var filteringSettings = new FilteringSettings(renderQueueRange, -1, fill.RenderingLayer);
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

                        renderStateBlock.mask |= RenderStateMask.Depth;
                        renderStateBlock.depthState = fill.occlusion switch
                        {
                            Occlusion.Always => new DepthState(false, CompareFunction.Always),
                            Occlusion.WhenOccluded => new DepthState(false, CompareFunction.Greater),
                            Occlusion.WhenNotOccluded => new DepthState(false, CompareFunction.LessEqual),
                            _ => throw new ArgumentOutOfRangeException()
                        };

                        context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);

                        if (fill.occlusion is Occlusion.WhenOccluded)
                        {
                            renderStateBlock.depthState = new DepthState(false, CompareFunction.LessEqual);
                            renderStateBlock.stencilReference = 0;
                            stencilState.SetPassOperation(StencilOp.Replace);
                            renderStateBlock.stencilState = stencilState;

                            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);
                        }

                        maskIndex++;
                    }
                }

                context.ExecuteCommandBuffer(maskCmd);
                CommandBufferPool.Release(maskCmd);

                // 2. Fill.
                // -> Render a fill.
                var fillCmd = CommandBufferPool.Get();

                using (new ProfilingScope(maskCmd, fillSampler))
                {
                    var i = 0;
                    foreach (var fill in settings.Fills)
                    {
                        if (!fill.IsActive())
                        {
                            i++;
                            continue;
                        }

                        // If this is the last render operation, clear the stencil.
                        if (i == lastActiveFillIndex)
                        {
                            fill.material.SetFloat(CommonShaderPropertyId.FullScreenStencilPass, (float) StencilOp.Zero);
                            fill.material.SetFloat(CommonShaderPropertyId.FullScreenStencilFail, (float) StencilOp.Zero);
                        }

                        CoreUtils.SetRenderTarget(fillCmd, renderingData.cameraData.renderer.cameraColorTargetHandle, cameraDepthRTHandle); // if using cameraColorRTHandle this does not render in scene view when rendering after post processing with post processing enabled
                        Blitter.BlitTexture(fillCmd, Vector2.one, fill.material, 0);

                        i++;
                    }
                }

                context.ExecuteCommandBuffer(fillCmd);
                CommandBufferPool.Release(fillCmd);
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

        [SerializeField] private SurfaceFillSettings settings;
        [SerializeField] private ShaderResources shaders;
        private Material maskMaterial, fillMaterial;
        private SurfaceFillPass surfaceFillPass;

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
            surfaceFillPass ??= new SurfaceFillPass();
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
                Debug.LogWarning("Not all required materials could be created. Surface Fill will not render.");
                return;
            }

            var render = surfaceFillPass.Setup(ref settings, ref maskMaterial, ref fillMaterial);
            if (render) renderer.EnqueuePass(surfaceFillPass);
        }
        
        #pragma warning disable 618, 672
        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            if (settings == null || renderingData.cameraData.cameraType == CameraType.SceneView && !settings.ShowInSceneView) return;

            surfaceFillPass.ConfigureInput(ScriptableRenderPassInput.Color);
            surfaceFillPass.ConfigureInput(ScriptableRenderPassInput.Depth);
            surfaceFillPass.SetTarget(renderer.cameraDepthTargetHandle);
        }
        #pragma warning restore 618, 672

        /// <summary>
        /// Clean up resources allocated to the Scriptable Renderer Feature such as materials.
        /// </summary>
        override protected void Dispose(bool disposing)
        {
            surfaceFillPass?.Dispose();
            surfaceFillPass = null;
            DestroyMaterials();
        }
        
        private void OnDestroy()
        {
            settings = null; // de-reference settings to allow them to be freed from memory
            surfaceFillPass?.Dispose();
        }

        private void DestroyMaterials()
        {
            CoreUtils.Destroy(maskMaterial);
            CoreUtils.Destroy(fillMaterial);
        }

        private bool CreateMaterials()
        {
            if (maskMaterial == null)
            {
                maskMaterial = CoreUtils.CreateEngineMaterial(shaders.mask);
            }

            if (fillMaterial == null)
            {
                fillMaterial = CoreUtils.CreateEngineMaterial(shaders.fill);
            }
            
            return maskMaterial != null && fillMaterial != null;
        }
    }
}