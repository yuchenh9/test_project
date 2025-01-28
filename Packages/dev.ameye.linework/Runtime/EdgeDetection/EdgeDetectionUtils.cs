using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Linework.EdgeDetection
{
    [Serializable]
    public sealed class ShaderResources
    {
        public Shader section;
        public Shader outline;

        public ShaderResources Load()
        {
            section = Shader.Find(ShaderPath.Section);
            outline = Shader.Find(ShaderPath.Outline);
            return this;
        }
    }
    
    static class ShaderPath
    {
        public const string Outline = "Hidden/Outlines/Edge Detection/Outline";
        public const string Section = "Hidden/Outlines/Edge Detection/Section";
    }

    static class Keyword
    {
        public static readonly GlobalKeyword ScreenSpaceOcclusion = GlobalKeyword.Create("_SCREEN_SPACE_OCCLUSION");
        public static readonly GlobalKeyword SectionPass = GlobalKeyword.Create("_SECTION_PASS");
    }

    static class ShaderPassName
    {
        public const string Section = "Section (Edge Detection)";
        public const string Outline = "Outline (Edge Detection)";
    }
    
    static class ShaderPropertyId
    {
        // Line appearance.
        public static readonly int BackgroundColor = Shader.PropertyToID("_BackgroundColor");
        public static readonly int OutlineColorShadow = Shader.PropertyToID("_OutlineColorShadow");
        public static readonly int FillColor = Shader.PropertyToID("_FillColor");
        public static readonly int OutlineThickness = Shader.PropertyToID("_OutlineThickness");
        public static readonly int ReferenceResolution = Shader.PropertyToID("_ReferenceResolution");
        public static readonly int FadeStart = Shader.PropertyToID("_FadeStart");
        public static readonly int FadeDistance = Shader.PropertyToID("_FadeDistance");
        public static readonly int FadeColor = Shader.PropertyToID("_FadeColor");
        
        // Edge detection.
        public static readonly int DepthSensitivity = Shader.PropertyToID("_DepthSensitivity");
        public static readonly int DepthDistanceModulation = Shader.PropertyToID("_DepthDistanceModulation");
        public static readonly int GrazingAngleMaskPower = Shader.PropertyToID("_GrazingAngleMaskPower");
        public static readonly int GrazingAngleMaskHardness = Shader.PropertyToID("_GrazingAngleMaskHardness");
        public static readonly int NormalSensitivity = Shader.PropertyToID("_NormalSensitivity");
        public static readonly int LuminanceSensitivity = Shader.PropertyToID("_LuminanceSensitivity");
        public static readonly int CameraSectioningTexture = Shader.PropertyToID("_CameraSectioningTexture");
  
        // Section map.
        public static readonly int SectionTexture = Shader.PropertyToID("_SectionTexture");
    }

    static class Buffer
    {
        public const string Section = "_SectionBuffer";
    }
    
    [Flags]
    public enum DiscontinuityInput
    {
        None = 0,
        Depth = 1 << 0,
        Normals = 1 << 1,
        Luminance = 1 << 2,
        Sections = 1 << 3,
        All = ~0,
    }
    
    public enum DebugView
    {
        None,
        [InspectorName("Depth")]
        Depth,
        [InspectorName("Normals")]
        Normals,
        [InspectorName("Luminance")]
        Luminance,
        [InspectorName("Sections")]
        Sections
    }
    
    static class ShaderFeature
    {
        public const string DepthDiscontinuity = "DEPTH";
        public const string NormalDiscontinuity = "NORMALS";
        public const string LuminanceDiscontinuity = "LUMINANCE";
        public const string SectionDiscontinuity = "SECTIONS";

        public const string TextureUV0 = "TEXTURE_UV_SET_UV0";
        public const string TextureUV1 = "TEXTURE_UV_SET_UV1";
        public const string TextureUV2 = "TEXTURE_UV_SET_UV2";
        public const string TextureUV3 = "TEXTURE_UV_SET_UV3";
        
        public const string VertexColorChannelR = "VERTEX_COLOR_CHANNEL_R";
        public const string VertexColorChannelG = "VERTEX_COLOR_CHANNEL_G";
        public const string VertexColorChannelB = "VERTEX_COLOR_CHANNEL_B";
        public const string VertexColorChannelA = "VERTEX_COLOR_CHANNEL_A";
        
        public const string TextureChannelR = "TEXTURE_CHANNEL_R";
        public const string TextureChannelG = "TEXTURE_CHANNEL_G";
        public const string TextureChannelB = "TEXTURE_CHANNEL_B";
        public const string TextureChannelA = "TEXTURE_CHANNEL_A";

        public const string OperatorCross = "OPERATOR_CROSS";
        public const string OperatorSobel = "OPERATOR_SOBEL";

        public const string DebugDepth = "DEBUG_DEPTH";
        public const string DebugNormals = "DEBUG_NORMALS";
        public const string DebugLuminance = "DEBUG_LUMINANCE";
        public const string DebugSections = "DEBUG_SECTIONS";
        public const string DebugSectionsRawValues = "DEBUG_SECTIONS_RAW_VALUES";
        public const string OverrideShadow = "OVERRIDE_SHADOW";
        public const string ScaleWithResolution = "SCALE_WITH_RESOLUTION";
        public const string FadeInDistance = "FADE_IN_DISTANCE";
        public const string SectionsMask = "SECTIONS_MASK";
        public const string DepthMask = "DEPTH_MASK";
        public const string NormalsMask = "NORMALS_MASK";
        public const string LuminanceMask = "LUMINANCE_MASK";

        public const string ObjectId = "OBJECT_ID";
        public const string Particles = "PARTICLES";
        public const string InputVertexColor = "INPUT_VERTEX_COLOR";
        public const string InputTexture = "INPUT_TEXTURE";
    }
    
    public enum SectionMapInput
    {
        [InspectorName("Solid Color")]
        None,
        [InspectorName("Vertex Color")]
        VertexColors,
        [InspectorName("Section Texture")]
        SectionTexture,
        [InspectorName("Custom")]
        Custom
    }
    
    public enum Kernel
    {
        RobertsCross,
        Sobel
    }
    
    public enum UVSet
    {
        UV0,
        UV1,
        UV2,
        UV3
    }
    
    public enum Resolution
    {
        [InspectorName("480px")]
        _480,
        [InspectorName("720px")]
        _720,
        [InspectorName("1080px")]
        _1080,
        [InspectorName("Custom")]
        Custom
    }
}