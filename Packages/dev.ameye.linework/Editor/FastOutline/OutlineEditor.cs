using System;
using Linework.Common.Utils;
using Linework.Editor.Common.Utils;
using Linework.WideOutline;
using UnityEditor;
using UnityEngine;
using Outline = Linework.FastOutline.Outline;
using Scaling = Linework.FastOutline.Scaling;

namespace Linework.Editor.FastOutline
{
    [CustomEditor(typeof(Outline))]
    public class OutlineEditor : UnityEditor.Editor
    {
        private SerializedProperty renderingLayer;
        private SerializedProperty occlusion;
        private SerializedProperty maskingStrategy;
        private SerializedProperty blendMode;
        private SerializedProperty gpuInstancing;
        private SerializedProperty color;
        private SerializedProperty enableOcclusion;
        private SerializedProperty occludedColor;
        private SerializedProperty extrusionMethod;
        private SerializedProperty scaling;
        private SerializedProperty width;
        private SerializedProperty minWidth;
        private SerializedProperty materialType;
        private SerializedProperty customMaterial;

        private void OnEnable()
        {
            renderingLayer = serializedObject.FindProperty(nameof(Outline.RenderingLayer));
            occlusion = serializedObject.FindProperty(nameof(Outline.occlusion));
            maskingStrategy = serializedObject.FindProperty(nameof(Outline.maskingStrategy));
            blendMode = serializedObject.FindProperty(nameof(Outline.blendMode));
            gpuInstancing = serializedObject.FindProperty(nameof(Outline.gpuInstancing));
            color = serializedObject.FindProperty(nameof(Outline.color));
            enableOcclusion = serializedObject.FindProperty(nameof(Outline.enableOcclusion));
            occludedColor = serializedObject.FindProperty(nameof(Outline.occludedColor));
            extrusionMethod = serializedObject.FindProperty(nameof(Outline.extrusionMethod));
            scaling = serializedObject.FindProperty(nameof(Outline.scaling));
            width = serializedObject.FindProperty(nameof(Outline.width));
            minWidth = serializedObject.FindProperty(nameof(Outline.minWidth));
            materialType = serializedObject.FindProperty(nameof(Outline.materialType));
            customMaterial = serializedObject.FindProperty(nameof(Outline.customMaterial));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Render", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(renderingLayer, EditorUtils.CommonStyles.OutlineLayer);
            EditorGUILayout.PropertyField(occlusion, EditorUtils.CommonStyles.OutlineOcclusion);
            EditorGUILayout.PropertyField(blendMode, EditorUtils.CommonStyles.OutlineBlendMode);
            if ((Occlusion) occlusion.intValue == Occlusion.WhenNotOccluded)
            {
                EditorGUILayout.PropertyField(maskingStrategy, EditorUtils.CommonStyles.MaskingStrategy);
            }
            EditorGUILayout.PropertyField(gpuInstancing, EditorUtils.CommonStyles.GpuInstancing);
            if (gpuInstancing.boolValue)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("GPU instancing breaks the SRP Batcher. See the documentation for details.", MessageType.Warning);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Outline", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(materialType, EditorUtils.CommonStyles.MaterialType);
            switch ((MaterialType) materialType.intValue)
            {
                case MaterialType.Basic:
                    EditorGUILayout.PropertyField(color, EditorUtils.CommonStyles.OutlineColor);
                    if ((Occlusion) occlusion.intValue == Occlusion.Always)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(enableOcclusion, EditorUtils.CommonStyles.OutlineOccludedColor);
                        if (enableOcclusion.boolValue) EditorGUILayout.PropertyField(occludedColor, GUIContent.none);
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.PropertyField(extrusionMethod, EditorUtils.CommonStyles.ExtrusionMethod);
                    EditorGUILayout.PropertyField(scaling, EditorUtils.CommonStyles.Scaling);
                    switch ((Scaling) scaling.intValue)
                    {
                        case Scaling.ConstantScreenSize:
                            EditorGUILayout.PropertyField(width, EditorUtils.CommonStyles.OutlineWidth);
                            break;
                        case Scaling.ScaleWithDistance:
                            EditorGUILayout.PropertyField(width, EditorUtils.CommonStyles.OutlineWidth);
                            EditorGUILayout.PropertyField(minWidth, EditorUtils.CommonStyles.MinWidth);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                case MaterialType.Custom:
                    EditorGUILayout.PropertyField(customMaterial, EditorUtils.CommonStyles.CustomMaterial);
                    break;
            }
            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();
        }
    }
}