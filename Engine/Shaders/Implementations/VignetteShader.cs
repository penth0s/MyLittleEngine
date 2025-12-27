using System.Runtime.Serialization;
using Engine.Core;
using Engine.Rendering;
using Engine.Systems;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using RenderingMode = Engine.Rendering.RenderingMode;

namespace Engine.Shaders.Implementations;

/// <summary>
/// Post-processing shader for vignette effect (darkened corners)
/// </summary>
public sealed class VignetteShader : ShaderBase
{
    #region Constants

    private const string DEFAULT_VERTEX_SHADER = "vignette.vert";
    private const string DEFAULT_FRAGMENT_SHADER = "vignette.frag";

    private const int SCREEN_TEXTURE_UNIT = 0;

    // Uniform Names
    private const string UNIFORM_INTENSITY = "intensity";
    private const string UNIFORM_SMOOTHNESS = "smoothness";
    private const string UNIFORM_ROUNDNESS = "roundness";
    private const string UNIFORM_SCREEN_RESOLUTION = "screenResolution";
    private const string UNIFORM_SCREEN_TEXTURE = "screenTexture";

    #endregion

    #region Properties

    public float Intensity
    {
        get => _intensity;
        set => _intensity = Math.Max(0f, value);
    }

    public float Smoothness
    {
        get => _smoothness;
        set => _smoothness = Math.Clamp(value, 0f, 1f);
    }

    public float Roundness
    {
        get => _roundness;
        set => _roundness = Math.Max(0f, value);
    }

    // Shader Properties
    protected override string VertexShaderName { get; } = DEFAULT_VERTEX_SHADER;
    protected override string FragmentShaderName { get; } = DEFAULT_FRAGMENT_SHADER;
    protected override string GeometryShaderName { get; } = null;

    #endregion

    #region Fields

    private float _intensity = 1.3f;
    private float _smoothness = 0.5f;
    private float _roundness = 1.0f;

    private RenderSystem _renderSystem;

    #endregion

    #region Constructors

    public VignetteShader() : this(DEFAULT_VERTEX_SHADER, DEFAULT_FRAGMENT_SHADER)
    {
        RenderingMode = RenderingMode.OPAQUE;
    }

    public VignetteShader(string vertexName, string fragmentName, string geometryName = null)
        : base(vertexName, fragmentName, geometryName)
    {
        Initialize();
    }

    #endregion

    #region Initialization

    private void Initialize()
    {
        CacheSystems();
    }

    private void CacheSystems()
    {
        _renderSystem = SystemManager.GetSystem<RenderSystem>();

        if (_renderSystem == null) throw new InvalidOperationException("RenderSystem not found in SystemManager");
    }

    #endregion

    #region Serialization

    [OnDeserialized]
    internal void OnDeserializedMethod(StreamingContext context)
    {
        LoadShader(DEFAULT_VERTEX_SHADER, DEFAULT_FRAGMENT_SHADER);
        CacheSystems();
    }

    #endregion

    #region Shader Updates

    public void UpdateProperties(FrameBuffer currentFrameBuffer)
    {
        ValidateRenderState();

        UpdateVignetteUniforms();
        UpdateScreenUniforms();
        BindInputTexture(currentFrameBuffer);
        RenderVignettePass();
    }

    private void ValidateRenderState()
    {
        if (_renderSystem?.FrameBuffer == null)
            throw new InvalidOperationException("RenderSystem FrameBuffer is not initialized");
    }

    #endregion

    #region Vignette Uniforms

    private void UpdateVignetteUniforms()
    {
        SetFloat(UNIFORM_INTENSITY, Intensity);
        SetFloat(UNIFORM_SMOOTHNESS, Smoothness);
        SetFloat(UNIFORM_ROUNDNESS, Roundness);
    }

    #endregion

    #region Screen Uniforms

    private void UpdateScreenUniforms()
    {
        var viewportSize = Screen.GetViewportSize();
        SetVector2(UNIFORM_SCREEN_RESOLUTION, new Vector2(viewportSize.X, viewportSize.Y));
    }

    #endregion

    #region Texture Binding

    private void BindInputTexture(FrameBuffer currentFrameBuffer)
    {
        SetInt(UNIFORM_SCREEN_TEXTURE, SCREEN_TEXTURE_UNIT);

        GL.ActiveTexture(TextureUnit.Texture0 + SCREEN_TEXTURE_UNIT);
        GL.BindTexture(TextureTarget.Texture2D, currentFrameBuffer.ColorTexture);
    }

    #endregion

    #region Rendering

    private void RenderVignettePass()
    {
        ConfigureRenderState();
        //ClearBuffers();
        DrawScreenQuad();
        RestoreRenderState();
    }

    private void ConfigureRenderState()
    {
        GL.Disable(EnableCap.DepthTest);
    }

    private void ClearBuffers()
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    private void DrawScreenQuad()
    {
        ScreenQuadRenderer.Draw();
    }

    private void RestoreRenderState()
    {
        GL.Enable(EnableCap.DepthTest);
    }

    #endregion
}