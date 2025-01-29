using System;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Linework.Editor.Common.Utils
{
    public static class EditorUtils
    {
        public static class CommonStyles
        {
            // Shared.
            public static readonly GUIContent InjectionPoint = EditorGUIUtility.TrTextContent("Stage", "Controls when the render pass executes.");
            public static readonly GUIContent ShowInSceneView = EditorGUIUtility.TrTextContent("Show In Scene View", "Sets whether to render the pass in the scene view.");
            public static readonly GUIContent DebugStage = EditorGUIUtility.TrTextContent("Debug", "Which stage to render as a debug view.");
            public static readonly GUIContent Scaling = EditorGUIUtility.TrTextContent("Scaling", "How to scale the width of the outline.");
            public static readonly GUIContent MinWidth = EditorGUIUtility.TrTextContent("Min Width", "The minimum width of the outline.");
            public static readonly GUIContent ClosedLoop = EditorGUIUtility.TrTextContent("Closed Loop", "Whether to render a closed loop outline.");
            public static readonly GUIContent AlphaCutout = EditorGUIUtility.TrTextContent("Alpha Cutout", "Enable alpha cutout.");
            public static readonly GUIContent AlphaCutoutTexture = EditorGUIUtility.TrTextContent("Texture", "The alpha cutout texture.");
            public static readonly GUIContent AlphaCutoutThreshold = EditorGUIUtility.TrTextContent("Threshold", "The alpha clip threshold.");
            public static readonly GUIContent CullMode = EditorGUIUtility.TrTextContent("Cull", "For which occlusion states to render the outline.");

            // Shared outlines.
            public static readonly GUIContent Outlines = EditorGUIUtility.TrTextContent("Outlines", "The list of outlines to render.");
            public static readonly GUIContent OutlineLayer = EditorGUIUtility.TrTextContent("Layer", "The rendering layer(s) which will get an outline rendered for them.");
            public static readonly GUIContent OutlineOcclusion = EditorGUIUtility.TrTextContent("Render", "For which occlusion states to render the outline.");
            public static readonly GUIContent OutlineBlendMode = EditorGUIUtility.TrTextContent("Blend", "How to blend the outline with the rest of the scene.");
            public static readonly GUIContent BackgroundColor = EditorGUIUtility.TrTextContent("Background Color", "The color of the background.");
            public static readonly GUIContent OutlineColor = EditorGUIUtility.TrTextContent("Color", "The color of the outline.");
            public static readonly GUIContent OutlineOccludedColor = EditorGUIUtility.TrTextContent("Occluded Color", "The color of the outline when it is occluded.");
            public static readonly GUIContent OutlineWidth = EditorGUIUtility.TrTextContent("Width", "The width of the outline.");
            public static readonly GUIContent ScaleWithResolution = EditorGUIUtility.TrTextContent("Scale With Resolution", "Scale the thickness of the outline with the resolution of the screen.");
            public static readonly GUIContent VertexAnimation = EditorGUIUtility.TrTextContent("Vertex Animation", "Make the outline follow the vertex animation of the mesh.");
            public static readonly GUIContent GpuInstancing = EditorGUIUtility.TrTextContent("GPU Instancing", "Use GPU instancing to render this outline layer.");
            
            // Surface fill.
            public static readonly GUIContent Fills = EditorGUIUtility.TrTextContent("Fills", "The list of fills to render.");
            public static readonly GUIContent FillLayer = EditorGUIUtility.TrTextContent("Layer", "The rendering layer(s) which will get a fill rendered for them.");
            public static readonly GUIContent FillOcclusion = EditorGUIUtility.TrTextContent("Render", "For which occlusion states to render the fill.");
            public static readonly GUIContent FillBlendMode = EditorGUIUtility.TrTextContent("Blend", "How to blend the fill with the rest of the scene.");
            public static readonly GUIContent Pattern = EditorGUIUtility.TrTextContent("Pattern", "The fill pattern that is used.");
            public static readonly GUIContent FillColor = EditorGUIUtility.TrTextContent("Color", "The color of the fill.");
            public static readonly GUIContent PrimaryFillColor = EditorGUIUtility.TrTextContent("Primary Color", "The primary color of the fill.");
            public static readonly GUIContent SecondaryFillColor = EditorGUIUtility.TrTextContent("Secondary Color", "The secondary color of the fill.");
            public static readonly GUIContent Frequency = EditorGUIUtility.TrTextContent("Frequency", "The frequency of the pattern.");
            public static readonly GUIContent Density = EditorGUIUtility.TrTextContent("Density", "The density of the pattern.");
            public static readonly GUIContent Rotation = EditorGUIUtility.TrTextContent("Rotation", "The rotation of the pattern.");
            public static readonly GUIContent Offset = EditorGUIUtility.TrTextContent("Offset", "The offset of the pattern.");
            public static readonly GUIContent Texture = EditorGUIUtility.TrTextContent("Texture", "The texture that is rendered as the fill.");
            public static readonly GUIContent Scale = EditorGUIUtility.TrTextContent("Scale", "The scale/tiling of the texture that is used.");
            public static readonly GUIContent Channel = EditorGUIUtility.TrTextContent("Channel", "The channel of the texture that is used.");
            public static readonly GUIContent Direction = EditorGUIUtility.TrTextContent("Direction", "The movement direction of the pattern.");
            public static readonly GUIContent Speed = EditorGUIUtility.TrTextContent("Speed", "The movement speed of the pattern.");
            public static readonly GUIContent Width = EditorGUIUtility.TrTextContent("Width", "The width of the glow.");
            public static readonly GUIContent Power = EditorGUIUtility.TrTextContent("Power", "How sharp the falloff of the glow is.");
            public static readonly GUIContent Softness = EditorGUIUtility.TrTextContent("Softness", "The softness of the glow.");

            // Fast outline.
            public static readonly GUIContent MaskingStrategy = EditorGUIUtility.TrTextContent("Mask", "The masking strategy that is used to only show the outline where needed.");
            public static readonly GUIContent ExtrusionMethod = EditorGUIUtility.TrTextContent("Method", "The vertex extrusion method that is used.");

            // Soft outline.
            public static readonly GUIContent Type = EditorGUIUtility.TrTextContent("Type", "Whether to render a soft or a hard outline.");
            public static readonly GUIContent Hardness = EditorGUIUtility.TrTextContent("Hardness", "The hardness of the outline.");
            public static readonly GUIContent Intensity = EditorGUIUtility.TrTextContent("Intensity", "The intensity of the outline.");
            public static readonly GUIContent DilationMethod = EditorGUIUtility.TrTextContent("Method", "The method used to dilate the outline.");
            public static readonly GUIContent Spread = EditorGUIUtility.TrTextContent("Spread", "The spread of the Gaussian kernel (Gaussian Blur).");
            public static readonly GUIContent Passes = EditorGUIUtility.TrTextContent("Passes", "How many blur passes to perform (Kawase Blur).");
            public static readonly GUIContent KernelSize = EditorGUIUtility.TrTextContent("Width (pixels)", "The width of the outline.");

            // Wide outline.
            public static readonly GUIContent CustomDepthBuffer = EditorGUIUtility.TrTextContent("Custom Depth (Experimental)", "Use a custom depth buffer to determine the occlusion state of the outlined pixels.");
            public static readonly GUIContent MaterialType = EditorGUIUtility.TrTextContent("Type", "The alpha clip threshold.");
            public static readonly GUIContent CustomMaterial = EditorGUIUtility.TrTextContent("Material", "The alpha clip threshold.");
            
            // Edge detection.
            public static readonly GUIContent DiscontinuityInput = EditorGUIUtility.TrTextContent("Sources", "Which inputs to use as discontinuity sources for the edge detection.");
            public static readonly GUIContent Sensitivity = EditorGUIUtility.TrTextContent("Sensitivity", "The sensitivity used to detect this type of discontinuity.");
            public static readonly GUIContent DepthDistanceModulation = EditorGUIUtility.TrTextContent("Distance Mask", "Adjust how sensitive the edge detection is to changes in depth based on the distance from the camera.");
            public static readonly GUIContent GrazingAngleMaskPower = EditorGUIUtility.TrTextContent("Sharp Angle Mask", "Helps prevent edges from being falsely detected when the camera views a surface at a shallow angle.");
            public static readonly GUIContent GrazingAngleMaskHardness = EditorGUIUtility.TrTextContent("Sharp Angle Mask Multiplier", "Helps prevent edges from being falsely detected when the camera views a surface at a shallow angle.");
            public static readonly GUIContent SectionsRawValues = EditorGUIUtility.TrTextContent("Raw Values", "Renderers on these layers which will render to the section map.");
            public static readonly GUIContent SectionLayer = EditorGUIUtility.TrTextContent("Layer", "Renderers on these layers which will render to the section map.");
            public static readonly GUIContent ObjectId = EditorGUIUtility.TrTextContent("Object ID", "Whether to render each object with a unique ID to the section map.");
            public static readonly GUIContent Particles = EditorGUIUtility.TrTextContent("Particles", "Whether to render each object with a unique ID to the section map.");
            public static readonly GUIContent SectionMask = EditorGUIUtility.TrTextContent("Section Mask", "Use the section map to mask out regions where edges should not show up.");
            public static readonly GUIContent SectionMapInput = EditorGUIUtility.TrTextContent("Source", "The input used for the section map.");
            public static readonly GUIContent VertexColorChannel = EditorGUIUtility.TrTextContent("Channel", "Which vertex color channel to render to the section map.");
            public static readonly GUIContent SectionTexture = EditorGUIUtility.TrTextContent("Texture", "Which texture to sample when rendering to the section map.");
            public static readonly GUIContent SectionTextureUVSet = EditorGUIUtility.TrTextContent("UV Set", "Which UV set to use when sampling the section texture.");
            public static readonly GUIContent SectionTextureChannel = EditorGUIUtility.TrTextContent("Channel", "Which color channel of the section texture to render to the section map.");
            public static readonly GUIContent Kernel = EditorGUIUtility.TrTextContent("Kernel", "The kernel that is used to detect edges.");
            public static readonly GUIContent OutlineThickness = EditorGUIUtility.TrTextContent("Thickness (pixels)", "The thickness of the edges.");
            public static readonly GUIContent EdgeColor = EditorGUIUtility.TrTextContent("Edge Color", "The color of the outline.");
            public static readonly GUIContent OverrideShadow = EditorGUIUtility.TrTextContent("Override Shadow", "The color of the outline when it is in an area that lies within a shadow.");
            public static readonly GUIContent OutlineFillColor = EditorGUIUtility.TrTextContent("Fill Color", "The color of the outline for fill in regions in the section map.");
            public static readonly GUIContent FadeInDistance = EditorGUIUtility.TrTextContent("Fade In Distance", "Fade the outline in the distance.");
            public static readonly GUIContent FadeStart = EditorGUIUtility.TrTextContent("Start (meters)", "The distance from which to start fading the outlines.");
            public static readonly GUIContent FadeDistance = EditorGUIUtility.TrTextContent("Distance (meters)", "The distance over which the outlines are faded.");
            public static readonly GUIContent FadeColor = EditorGUIUtility.TrTextContent("Color", "The color to fade to.");
        }

        public static void SectionGUI(string title, SerializedProperty expanded, Action drawAction, SerializedObject serializedObject, SerializedProperty enabled = null)
        {
            expanded.boolValue = enabled != null
                ? CoreEditorUtils.DrawHeaderToggleFoldout(EditorGUIUtility.TrTextContent(title), expanded.boolValue, enabled, null, null, null, null)
#if UNITY_6000_0_OR_NEWER
                : CoreEditorUtils.DrawHeaderFoldout(title, expanded.boolValue, isTitleHeader: false);
#else
                : CoreEditorUtils.DrawHeaderFoldout(title, expanded.boolValue);
#endif

            if (expanded.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    if (enabled == null || enabled.boolValue)
                    {
                        drawAction?.Invoke();
                    }
                    else
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.HelpBox(title + " disabled.", MessageType.Info);
                    }
                }
                EditorGUILayout.Space();
            }
            CoreEditorUtils.DrawSplitter();
            serializedObject.ApplyModifiedProperties();
        }

        public static void OpenInspectorWindow(Object target)
        {
            var windowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            EditorWindow.GetWindow(windowType);
            AssetDatabase.OpenAsset(target);
        }
    }
}