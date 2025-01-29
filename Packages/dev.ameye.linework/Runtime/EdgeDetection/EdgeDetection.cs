using System;
using Linework.Common.Utils;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif
using UnityEngine.Rendering.Universal;

namespace Linework.EdgeDetection
{
    [ExcludeFromPreset]
    [DisallowMultipleRendererFeature("Edge Detection")]
#if UNITY_6000_0_OR_NEWER
    [SupportedOnRenderer(typeof(UniversalRendererData))]
#endif
    [Tooltip("Edge Detection renders outlines by detecting edges and discontinuities within the scene.")]
    [HelpURL("https://linework.ameye.dev/outlines/edge-detection")]
    public class EdgeDetection : ScriptableRendererFeature
    {
        private class EdgeDetectionPass : ScriptableRenderPass
        {
            private EdgeDetectionSettings settings;
            private Material outline, section;
            private readonly ProfilingSampler sectionSampler, outlineSampler;
            
            public EdgeDetectionPass()
            {
                profilingSampler = new ProfilingSampler(nameof(EdgeDetectionPass));
                sectionSampler = new ProfilingSampler(ShaderPassName.Section);
                outlineSampler = new ProfilingSampler(ShaderPassName.Outline);
            }

            public bool Setup(ref EdgeDetectionSettings edgeDetectionSettings, ref Material sectionMaterial, ref Material outlineMaterial)
            {
                settings = edgeDetectionSettings;
                section = sectionMaterial;
                outline = outlineMaterial;
                renderPassEvent = (RenderPassEvent) edgeDetectionSettings.InjectionPoint;

                if (settings.objectId) section.EnableKeyword(ShaderFeature.ObjectId);
                else section.DisableKeyword(ShaderFeature.ObjectId);
                
                if (settings.particles) section.EnableKeyword(ShaderFeature.Particles);
                else section.DisableKeyword(ShaderFeature.Particles);

                switch (edgeDetectionSettings.sectionMapInput)
                {
                    case SectionMapInput.None or SectionMapInput.Custom:
                        section.DisableKeyword(ShaderFeature.InputVertexColor);
                        section.DisableKeyword(ShaderFeature.InputTexture);
                        break;
                    case SectionMapInput.VertexColors:
                        section.EnableKeyword(ShaderFeature.InputVertexColor);
                        section.DisableKeyword(ShaderFeature.InputTexture);
                        switch (edgeDetectionSettings.vertexColorChannel)
                        {
                            case Channel.R:
                                section.EnableKeyword(ShaderFeature.VertexColorChannelR);
                                section.DisableKeyword(ShaderFeature.VertexColorChannelG);
                                section.DisableKeyword(ShaderFeature.VertexColorChannelB);
                                section.DisableKeyword(ShaderFeature.VertexColorChannelA);
                                break;
                            case Channel.G:
                                section.DisableKeyword(ShaderFeature.VertexColorChannelR);
                                section.EnableKeyword(ShaderFeature.VertexColorChannelG);
                                section.DisableKeyword(ShaderFeature.VertexColorChannelB);
                                section.DisableKeyword(ShaderFeature.VertexColorChannelA);
                                break;
                            case Channel.B:
                                section.DisableKeyword(ShaderFeature.VertexColorChannelR);
                                section.DisableKeyword(ShaderFeature.VertexColorChannelG);
                                section.EnableKeyword(ShaderFeature.VertexColorChannelB);
                                section.DisableKeyword(ShaderFeature.VertexColorChannelA);
                                break;
                            case Channel.A:
                                section.DisableKeyword(ShaderFeature.VertexColorChannelR);
                                section.DisableKeyword(ShaderFeature.VertexColorChannelG);
                                section.DisableKeyword(ShaderFeature.VertexColorChannelB);
                                section.EnableKeyword(ShaderFeature.VertexColorChannelA);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        break;
                    case SectionMapInput.SectionTexture:
                        section.DisableKeyword(ShaderFeature.InputVertexColor);
                        section.EnableKeyword(ShaderFeature.InputTexture);
                        section.SetTexture(ShaderPropertyId.SectionTexture, edgeDetectionSettings.sectionTexture);
                        switch (edgeDetectionSettings.sectionTextureUvSet)
                        {
                            case UVSet.UV0:
                                section.EnableKeyword(ShaderFeature.TextureUV0);
                                section.DisableKeyword(ShaderFeature.TextureUV1);
                                section.DisableKeyword(ShaderFeature.TextureUV2);
                                section.DisableKeyword(ShaderFeature.TextureUV3);
                                break;
                            case UVSet.UV1:
                                section.DisableKeyword(ShaderFeature.TextureUV0);
                                section.EnableKeyword(ShaderFeature.TextureUV1);
                                section.DisableKeyword(ShaderFeature.TextureUV2);
                                section.DisableKeyword(ShaderFeature.TextureUV3);
                                break;
                            case UVSet.UV2:
                                section.DisableKeyword(ShaderFeature.TextureUV0);
                                section.DisableKeyword(ShaderFeature.TextureUV1);
                                section.EnableKeyword(ShaderFeature.TextureUV2);
                                section.DisableKeyword(ShaderFeature.TextureUV3);
                                break;
                            case UVSet.UV3:
                                section.DisableKeyword(ShaderFeature.TextureUV0);
                                section.DisableKeyword(ShaderFeature.TextureUV1);
                                section.DisableKeyword(ShaderFeature.TextureUV2);
                                section.EnableKeyword(ShaderFeature.TextureUV3);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        switch (edgeDetectionSettings.vertexColorChannel)
                        {
                            case Channel.R:
                                section.EnableKeyword(ShaderFeature.TextureChannelR);
                                section.DisableKeyword(ShaderFeature.TextureChannelG);
                                section.DisableKeyword(ShaderFeature.TextureChannelB);
                                section.DisableKeyword(ShaderFeature.TextureChannelA);
                                break;
                            case Channel.G:
                                section.DisableKeyword(ShaderFeature.TextureChannelR);
                                section.EnableKeyword(ShaderFeature.TextureChannelG);
                                section.DisableKeyword(ShaderFeature.TextureChannelB);
                                section.DisableKeyword(ShaderFeature.TextureChannelA);
                                break;
                            case Channel.B:
                                section.DisableKeyword(ShaderFeature.TextureChannelR);
                                section.DisableKeyword(ShaderFeature.TextureChannelG);
                                section.EnableKeyword(ShaderFeature.TextureChannelB);
                                section.DisableKeyword(ShaderFeature.TextureChannelA);
                                break;
                            case Channel.A:
                                section.DisableKeyword(ShaderFeature.TextureChannelR);
                                section.DisableKeyword(ShaderFeature.TextureChannelG);
                                section.DisableKeyword(ShaderFeature.TextureChannelB);
                                section.EnableKeyword(ShaderFeature.TextureChannelA);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
                // Set outline material properties.
                switch (edgeDetectionSettings.DebugView)
                {
                    case DebugView.None:
                        outline.DisableKeyword(ShaderFeature.DebugSections);
                        outline.DisableKeyword(ShaderFeature.DebugDepth);
                        outline.DisableKeyword(ShaderFeature.DebugNormals);
                        outline.DisableKeyword(ShaderFeature.DebugLuminance);
                        break;
                    case DebugView.Sections:
                        outline.EnableKeyword(ShaderFeature.DebugSections);
                        outline.DisableKeyword(ShaderFeature.DebugDepth);
                        outline.DisableKeyword(ShaderFeature.DebugNormals);
                        outline.DisableKeyword(ShaderFeature.DebugLuminance);
                        break;
                    case DebugView.Depth:
                        outline.DisableKeyword(ShaderFeature.DebugSections);
                        outline.EnableKeyword(ShaderFeature.DebugDepth);
                        outline.DisableKeyword(ShaderFeature.DebugNormals);
                        outline.DisableKeyword(ShaderFeature.DebugLuminance);
                        break;
                    case DebugView.Normals:
                        outline.DisableKeyword(ShaderFeature.DebugSections);
                        outline.DisableKeyword(ShaderFeature.DebugDepth);
                        outline.EnableKeyword(ShaderFeature.DebugNormals);
                        outline.DisableKeyword(ShaderFeature.DebugLuminance);
                        break;
                    case DebugView.Luminance:
                        outline.DisableKeyword(ShaderFeature.DebugSections);
                        outline.DisableKeyword(ShaderFeature.DebugDepth);
                        outline.DisableKeyword(ShaderFeature.DebugNormals);
                        outline.EnableKeyword(ShaderFeature.DebugLuminance);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                if(edgeDetectionSettings.debugSectionsRaw) outline.EnableKeyword(ShaderFeature.DebugSectionsRawValues);
                else outline.DisableKeyword(ShaderFeature.DebugSectionsRawValues);

                if (edgeDetectionSettings.discontinuityInput.HasFlag(DiscontinuityInput.Depth)) outline.EnableKeyword(ShaderFeature.DepthDiscontinuity);
                else outline.DisableKeyword(ShaderFeature.DepthDiscontinuity);
                if (edgeDetectionSettings.discontinuityInput.HasFlag(DiscontinuityInput.Normals)) outline.EnableKeyword(ShaderFeature.NormalDiscontinuity);
                else outline.DisableKeyword(ShaderFeature.NormalDiscontinuity);
                if (edgeDetectionSettings.discontinuityInput.HasFlag(DiscontinuityInput.Luminance)) outline.EnableKeyword(ShaderFeature.LuminanceDiscontinuity);
                else outline.DisableKeyword(ShaderFeature.LuminanceDiscontinuity);
                if (edgeDetectionSettings.discontinuityInput.HasFlag(DiscontinuityInput.Sections)) outline.EnableKeyword(ShaderFeature.SectionDiscontinuity);
                else outline.DisableKeyword(ShaderFeature.SectionDiscontinuity);

                outline.SetFloat(ShaderPropertyId.DepthSensitivity, edgeDetectionSettings.depthSensitivity * 100.0f);
                outline.SetFloat(ShaderPropertyId.DepthDistanceModulation, edgeDetectionSettings.depthDistanceModulation * 10.0f);
                outline.SetFloat(ShaderPropertyId.GrazingAngleMaskPower, edgeDetectionSettings.grazingAngleMaskPower * 10.0f);
                outline.SetFloat(ShaderPropertyId.GrazingAngleMaskHardness, edgeDetectionSettings.grazingAngleMaskHardness);
                outline.SetFloat(ShaderPropertyId.NormalSensitivity, edgeDetectionSettings.normalSensitivity * 10.0f);
                outline.SetFloat(ShaderPropertyId.LuminanceSensitivity, edgeDetectionSettings.luminanceSensitivity * 20.0f);

                switch (edgeDetectionSettings.kernel)
                {
                    case Kernel.RobertsCross:
                        outline.EnableKeyword(ShaderFeature.OperatorCross);
                        outline.DisableKeyword(ShaderFeature.OperatorSobel);
                        break;
                    case Kernel.Sobel:
                        outline.DisableKeyword(ShaderFeature.OperatorCross);
                        outline.EnableKeyword(ShaderFeature.OperatorSobel);
                        break;
                }
                
                // Outline thickness.
                outline.SetFloat(ShaderPropertyId.OutlineThickness, edgeDetectionSettings.outlineThickness);
                if (edgeDetectionSettings.scaleWithResolution) outline.EnableKeyword(ShaderFeature.ScaleWithResolution);
                else outline.DisableKeyword(ShaderFeature.ScaleWithResolution);
                switch (edgeDetectionSettings.referenceResolution)
                {
                    case Resolution._480:
                        outline.SetFloat(ShaderPropertyId.ReferenceResolution, 480.0f);
                        break;
                    case Resolution._720:
                        outline.SetFloat(ShaderPropertyId.ReferenceResolution, 720.0f);
                        break;
                    case Resolution._1080:
                        outline.SetFloat(ShaderPropertyId.ReferenceResolution, 1080.0f);
                        break;
                    case Resolution.Custom:
                        outline.SetFloat(ShaderPropertyId.ReferenceResolution, edgeDetectionSettings.customResolution);
                        break;
                }
                
                // Distance fade.
                if (edgeDetectionSettings.fadeInDistance) outline.EnableKeyword(ShaderFeature.FadeInDistance);
                else outline.DisableKeyword(ShaderFeature.FadeInDistance);
                outline.SetFloat(ShaderPropertyId.FadeStart, edgeDetectionSettings.fadeStart);
                outline.SetFloat(ShaderPropertyId.FadeDistance, edgeDetectionSettings.fadeDistance);
                outline.SetColor(ShaderPropertyId.FadeColor, edgeDetectionSettings.fadeColor);
                
                // Masks.
                if (edgeDetectionSettings.sectionsMask) outline.EnableKeyword(ShaderFeature.SectionsMask);
                else outline.DisableKeyword(ShaderFeature.SectionsMask);
                if (edgeDetectionSettings.depthMask) outline.EnableKeyword(ShaderFeature.DepthMask);
                else outline.DisableKeyword(ShaderFeature.DepthMask);
                if (edgeDetectionSettings.normalsMask) outline.EnableKeyword(ShaderFeature.NormalsMask);
                else outline.DisableKeyword(ShaderFeature.NormalsMask);
                if (edgeDetectionSettings.luminanceMask) outline.EnableKeyword(ShaderFeature.LuminanceMask);
                else outline.DisableKeyword(ShaderFeature.LuminanceMask);
                
                outline.SetColor(ShaderPropertyId.BackgroundColor, edgeDetectionSettings.backgroundColor);
                outline.SetColor(CommonShaderPropertyId.OutlineColor, edgeDetectionSettings.outlineColor);
                outline.SetColor(ShaderPropertyId.OutlineColorShadow, edgeDetectionSettings.outlineColorShadow);
                if (edgeDetectionSettings.overrideColorInShadow) outline.EnableKeyword(ShaderFeature.OverrideShadow);
                else outline.DisableKeyword(ShaderFeature.OverrideShadow);
                outline.SetColor(ShaderPropertyId.FillColor, edgeDetectionSettings.fillColor);

                var (sourceBlend, destinationBlend) = RenderUtils.GetSrcDstBlend(settings.blendMode);
                outline.SetInt(RenderUtils.BlendModeSourceProperty, sourceBlend);
                outline.SetInt(RenderUtils.BlendModeDestinationProperty, destinationBlend);

                return true;
            }
#if UNITY_6000_0_OR_NEWER
            private class PassData
            {
                internal RendererListHandle SectionRendererListHandle;
            }
            
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resourceData = frameData.Get<UniversalResourceData>();
                var cameraData = frameData.Get<UniversalCameraData>();

                CreateRenderGraphTextures(renderGraph, cameraData, out var sectionHandle);

                // 1. Section.
                // -> Render section map.
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(ShaderPassName.Section, out var passData))
                {
                    builder.SetRenderAttachment(sectionHandle, 0);
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);
                    builder.SetGlobalTextureAfterPass(sectionHandle, ShaderPropertyId.CameraSectioningTexture);

                    InitSectionRendererList(renderGraph, frameData, ref passData);
                    builder.UseRendererList(passData.SectionRendererListHandle);
                    
                    if (settings.sectionMapInput == SectionMapInput.Custom) builder.AllowGlobalStateModification(true);

                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        // TODO: better conditions here
                        // if (!settings.discontinuityInput.HasFlag(DiscontinuityInput.SectionMap)) return;
                        
                        // Enable section pass.
                        if (settings.sectionMapInput == SectionMapInput.Custom)
                        {
                            context.cmd.DisableKeyword(Keyword.ScreenSpaceOcclusion);
                            context.cmd.EnableKeyword(Keyword.SectionPass);
                        }

                        // Render sections.
                        context.cmd.DrawRendererList(data.SectionRendererListHandle);
                      
                        // Disable section map.
                        if (settings.sectionMapInput == SectionMapInput.Custom)
                        {
                            context.cmd.EnableKeyword(Keyword.ScreenSpaceOcclusion);
                            context.cmd.DisableKeyword(Keyword.SectionPass);
                        }
                    });
                }
                
                // 2. Composite outline.
                // -> Add the outline to the scene.
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(ShaderPassName.Outline, out _))
                {
                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                    builder.UseAllGlobalTextures(true);
                
                    builder.AllowPassCulling(false);
                
                    builder.SetRenderFunc((PassData _, RasterGraphContext context) =>
                    {
                        Blitter.BlitTexture(context.cmd, Vector2.one, outline, 0);
                    });
                }
            }

            private void InitSectionRendererList(RenderGraph renderGraph, ContextContainer frameData, ref PassData passData)
            {
                var universalRenderingData = frameData.Get<UniversalRenderingData>();
                var cameraData = frameData.Get<UniversalCameraData>();
                var lightData = frameData.Get<UniversalLightData>();

                var sortingCriteria = cameraData.defaultOpaqueSortFlags;
                var renderQueueRange = RenderQueueRange.opaque;

                var drawingSettings = RenderingUtils.CreateDrawingSettings(RenderUtils.DefaultShaderTagIds, universalRenderingData, cameraData, lightData, sortingCriteria);

                var filteringSettings = new FilteringSettings(renderQueueRange, -1, settings.SectionRenderingLayer);

                if (settings.sectionMapInput is SectionMapInput.None or SectionMapInput.SectionTexture or SectionMapInput.VertexColors)
                {
                    drawingSettings.overrideMaterial = section;
                }

                var renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
                RenderUtils.CreateRendererListWithRenderStateBlock(renderGraph, ref universalRenderingData.cullResults, drawingSettings, filteringSettings, renderStateBlock,
                    ref passData.SectionRendererListHandle);
            }

            private void CreateRenderGraphTextures(RenderGraph renderGraph, UniversalCameraData cameraData, out TextureHandle sectionHandle)
            {
                // Section buffer.
                var sectionBufferDescriptor = cameraData.cameraTargetDescriptor;
                sectionBufferDescriptor.graphicsFormat = GraphicsFormat.R16_UNorm;
                sectionBufferDescriptor.depthBufferBits = (int) DepthBits.None;
                sectionBufferDescriptor.msaaSamples = 1;
                sectionHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, sectionBufferDescriptor, Buffer.Section, false);
            }
#endif
            private RTHandle cameraDepthRTHandle, sectionRTHandle;
            private RTHandle[] handles;

            #pragma warning disable 618, 672
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                if(handles is not {Length: 1})
                {
                    handles = new RTHandle[1];
                }
                handles[0] = sectionRTHandle;
                
                ConfigureTarget(handles, cameraDepthRTHandle);
                ConfigureClear(ClearFlag.Color, Color.clear);
            }
            
            public void CreateHandles(RenderingData renderingData)
            {
                // Section buffer.
                var sectionBufferDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                sectionBufferDescriptor.graphicsFormat = GraphicsFormat.R8_UNorm;
                sectionBufferDescriptor.depthBufferBits = (int) DepthBits.None;
                sectionBufferDescriptor.msaaSamples = 1;
                RenderingUtils.ReAllocateIfNeeded(ref sectionRTHandle, sectionBufferDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: Buffer.Section);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                // 1. Section.
                // -> Render section map.
                // TODO: better conditions here
                if (true || settings.discontinuityInput.HasFlag(DiscontinuityInput.Sections))
                {
                    var sectionCmd = CommandBufferPool.Get();

                    using (new ProfilingScope(sectionCmd, sectionSampler))
                    {
                        context.ExecuteCommandBuffer(sectionCmd);
                        sectionCmd.Clear();

                        var sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
                        var renderQueueRange = RenderQueueRange.opaque;

                        var drawingSettings = RenderingUtils.CreateDrawingSettings(RenderUtils.DefaultShaderTagIds, ref renderingData, sortingCriteria);

                        var filteringSettings = new FilteringSettings(renderQueueRange, -1, settings.SectionRenderingLayer);

                        if (settings.sectionMapInput is SectionMapInput.None or SectionMapInput.SectionTexture or SectionMapInput.VertexColors)
                        {
                            drawingSettings.overrideMaterial = section;
                        }

                        var renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
                        
                        // Enable section pass.
                        if (settings.sectionMapInput == SectionMapInput.Custom)
                        {
                            sectionCmd.DisableKeyword(Keyword.ScreenSpaceOcclusion);
                            sectionCmd.EnableKeyword(Keyword.SectionPass);
                        }
                        context.ExecuteCommandBuffer(sectionCmd);

                        // Render sections.
                        context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);

                        // Disable section map.
                        if (settings.sectionMapInput == SectionMapInput.Custom)
                        {
                            sectionCmd.EnableKeyword(Keyword.ScreenSpaceOcclusion);
                            sectionCmd.DisableKeyword(Keyword.SectionPass);
                        }
                        context.ExecuteCommandBuffer(sectionCmd);
                    }

                    sectionCmd.SetGlobalTexture(ShaderPropertyId.CameraSectioningTexture, sectionRTHandle.nameID);
                    context.ExecuteCommandBuffer(sectionCmd);
                    CommandBufferPool.Release(sectionCmd);
                }

                // 2. Composite outline.
                // -> Add the outline to the scene.
                var outlineCmd = CommandBufferPool.Get();

                using (new ProfilingScope(outlineCmd, outlineSampler))
                {
                    CoreUtils.SetRenderTarget(outlineCmd, renderingData.cameraData.renderer.cameraColorTargetHandle); // if using cameraColorRTHandle this does not render in scene view when rendering after post processing with post processing enabled
                    context.ExecuteCommandBuffer(outlineCmd);
                    outlineCmd.Clear();
                   
                    Blitter.BlitTexture(outlineCmd, Vector2.one, outline, 0);
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
                
                sectionRTHandle?.Release();
            }
        }

        [SerializeField] private EdgeDetectionSettings settings;
        [SerializeField] private ShaderResources shaders;
        private Material sectionMaterial, outlineMaterial;
        private EdgeDetectionPass edgeDetectionPass;

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
            edgeDetectionPass ??= new EdgeDetectionPass();
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
                Debug.LogWarning("Not all required materials could be created. Edge Detection will not render.");
                return;
            }

            var input = ScriptableRenderPassInput.None;
            if (settings.discontinuityInput.HasFlag(DiscontinuityInput.Depth))
            {
                input |= ScriptableRenderPassInput.Depth;
            }
            if (settings.discontinuityInput.HasFlag(DiscontinuityInput.Luminance))
            {
                input |= ScriptableRenderPassInput.Color;
            }
            if (settings.discontinuityInput.HasFlag(DiscontinuityInput.Normals))
            {
                input |= ScriptableRenderPassInput.Normal;
            }
            edgeDetectionPass.ConfigureInput(input);
            
#if UNITY_6000_0_OR_NEWER
            // NOTE: This is needed because the shader needs the current screen contents as input texture, but also needs to write to it, so a copy is needed.
            edgeDetectionPass.requiresIntermediateTexture = true;
#endif
            var render = edgeDetectionPass.Setup(ref settings, ref sectionMaterial, ref outlineMaterial);
            if (render) renderer.EnqueuePass(edgeDetectionPass);
        }
        
        #pragma warning disable 618, 672
        
        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            if (settings == null || renderingData.cameraData.cameraType == CameraType.SceneView && !settings.ShowInSceneView) return;

            edgeDetectionPass.CreateHandles(renderingData);
            edgeDetectionPass.SetTarget(renderer.cameraDepthTargetHandle);
        }
        
        #pragma warning restore 618, 672

        /// <summary>
        /// Clean up resources allocated to the Scriptable Renderer Feature such as materials.
        /// </summary>
        override protected void Dispose(bool disposing)
        {
            edgeDetectionPass?.Dispose();
            edgeDetectionPass = null;
            DestroyMaterials();
        }
        
        private void OnDestroy()
        {
            settings = null; // de-reference settings to allow them to be freed from memory
            edgeDetectionPass?.Dispose();
        }

        private void DestroyMaterials()
        {
            CoreUtils.Destroy(sectionMaterial);
            CoreUtils.Destroy(outlineMaterial);
        }

        private bool CreateMaterials()
        {
            if (sectionMaterial == null)
            {
                sectionMaterial = CoreUtils.CreateEngineMaterial(shaders.section);
            }

            if (outlineMaterial == null)
            {
                outlineMaterial = CoreUtils.CreateEngineMaterial(shaders.outline);
            }

            return sectionMaterial != null && outlineMaterial != null;
        }
    }
}