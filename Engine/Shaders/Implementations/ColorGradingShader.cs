using System.Runtime.Serialization;
using Engine.Rendering;
using OpenTK.Graphics.OpenGL4;
using RenderingMode = Engine.Rendering.RenderingMode;

namespace Engine.Shaders.Implementations;

public sealed class ColorGradingShader : ShaderBase
{
    #region Constants

    private const string DEFAULT_VERTEX_SHADER = "colorgrading.vert";
    private const string DEFAULT_FRAGMENT_SHADER = "colorgrading.frag";
    private const int SCREEN_TEXTURE_UNIT = 0;

    private const string UNIFORM_SCREEN_TEXTURE = "screenTexture";
    private const string UNIFORM_EXPOSURE = "exposure";
    private const string UNIFORM_CONTRAST = "contrast";
    private const string UNIFORM_SATURATION = "saturation";
    private const string UNIFORM_TEMPERATURE = "temperature";
    private const string UNIFORM_TINT = "tint";

    #endregion

    #region Properties

    public float Exposure { get; set; } = 1.0f;
    public float Contrast { get; set; } = 1.0f;
    public float Saturation { get; set; } = 1.0f;
    public float Temperature { get; set; } = 0.1f; 
    public float Tint { get; set; } = 0.0f; 
    
    protected override string VertexShaderName { get; } = DEFAULT_VERTEX_SHADER;
    protected override string FragmentShaderName { get; } = DEFAULT_FRAGMENT_SHADER;
    protected override string GeometryShaderName { get; } = null;

    #endregion

    #region Constructors

    public ColorGradingShader() : this(DEFAULT_VERTEX_SHADER, DEFAULT_FRAGMENT_SHADER)
    {
        RenderingMode = RenderingMode.OPAQUE;
    }

    public ColorGradingShader(string vertexName, string fragmentName, string geometryName = null)
        : base(vertexName, fragmentName, geometryName)
    {
    }

    #endregion

    #region Serialization

    [OnDeserialized]
    internal void OnDeserializedMethod(StreamingContext context)
    {
        LoadShader(DEFAULT_VERTEX_SHADER, DEFAULT_FRAGMENT_SHADER);
    }

    #endregion

    #region Update

    public void UpdateProperties(FrameBuffer currentFrameBuffer)
    {
        UpdateUniforms();
        BindInputTexture(currentFrameBuffer);
        Render();
    }

    private void UpdateUniforms()
    {
        SetFloat(UNIFORM_EXPOSURE, Exposure);
        SetFloat(UNIFORM_CONTRAST, Contrast);
        SetFloat(UNIFORM_SATURATION, Saturation);
        SetFloat(UNIFORM_TEMPERATURE, Temperature);
        SetFloat(UNIFORM_TINT, Tint);
    }

    private void BindInputTexture(FrameBuffer currentFrameBuffer)
    {
        SetInt(UNIFORM_SCREEN_TEXTURE, SCREEN_TEXTURE_UNIT);
        GL.ActiveTexture(TextureUnit.Texture0 + SCREEN_TEXTURE_UNIT);
        GL.BindTexture(TextureTarget.Texture2D, currentFrameBuffer.ColorTexture);
    }

    private void Render()
    {
        GL.Disable(EnableCap.DepthTest);
        ScreenQuadRenderer.Draw();
        GL.Enable(EnableCap.DepthTest);
    }

    #endregion
}