using Engine.Core;
using Engine.Rendering;
using Engine.Shaders.Implementations;
using Engine.Systems;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using RenderingMode = Engine.Rendering.RenderingMode;

namespace Engine.Components;

/// <summary>
/// Represents a light source in the scene that can cast shadows and illuminate objects.
/// Supports directional, point, and spot light types with configurable shadow mapping.
/// </summary>
public sealed class Light : Component, IDisposable
{
    #region Constants

    private const float DEFAULT_INTENSITY = 1.0f;
    private const float DEFAULT_RANGE = 100.0f;
    private const float DEFAULT_SPOT_ANGLE = 100.0f;
    private const float DEFAULT_INNER_SPOT_ANGLE = 50.0f;
    private const int DEFAULT_ORTHO_HEIGHT = 250;
    private const int DEFAULT_ORTHO_WIDTH = 250;
    private const float SHADOW_NEAR_PLANE = 0.1f;
    private const float SHADOW_FAR_PLANE = 200f;
    private const float LIGHT_OFFSET_DISTANCE = -10f;

    #endregion

    #region Properties - Light Configuration

    /// <summary>
    /// Gets or sets the color of the light.
    /// </summary>
    public Color4 LightColor { get; set; } = Color4.White;

    /// <summary>
    /// Gets or sets the intensity of the light.
    /// </summary>
    public float Intensity { get; set; } = DEFAULT_INTENSITY;

    /// <summary>
    /// Gets or sets the type of light (Directional, Point, Spot).
    /// </summary>
    public LightType LightType { get; set; } = LightType.Spot;

    #endregion

    #region Properties - Shadow Configuration

    /// <summary>
    /// Gets or sets whether this light casts shadows.
    /// </summary>
    public bool CastShadow { get; set; }

    /// <summary>
    /// Gets or sets the maximum distance at which this light affects objects.
    /// </summary>
    public float Range { get; set; } = DEFAULT_RANGE;

    /// <summary>
    /// Gets or sets the outer cone angle for spot lights in degrees.
    /// </summary>
    public float SpotAngle { get; set; } = DEFAULT_SPOT_ANGLE;

    /// <summary>
    /// Gets or sets the inner cone angle for spot lights in degrees.
    /// </summary>
    public float InnerSpotAngle { get; set; } = DEFAULT_INNER_SPOT_ANGLE;

    /// <summary>
    /// The height of the orthographic shadow projection.
    /// </summary>
    public int OrthographicHeight = DEFAULT_ORTHO_HEIGHT;

    /// <summary>
    /// The width of the orthographic shadow projection.
    /// </summary>
    public int OrthographicWidth = DEFAULT_ORTHO_WIDTH;

    #endregion

    #region Properties - Shadow Textures

    /// <summary>
    /// Gets the OpenGL texture handle for the shadow map.
    /// </summary>
    public int ShadowTextureIndex => _shadowFrameBuffer?.ShadowMap ?? 0;

    #endregion

    #region Fields

    private ShadowFramebuffer _shadowFrameBuffer;
    private Material _shadowMaterial;
    private SceneSystem _sceneSystem;
    private bool _isDisposed;

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the light component with shadow rendering materials.
    /// </summary>
    internal override void Initialize(GameObject owner)
    {
        base.Initialize(owner);

        InitializeShadowFrameBuffer();
        InitializeShadowMaterial();
        InitializeSystems();
    }

    private void InitializeShadowFrameBuffer()
    {
        _shadowFrameBuffer = new ShadowFramebuffer();
    }

    private void InitializeShadowMaterial()
    {
        _shadowMaterial = new Material();
        _shadowMaterial.UpdateShader<ShadowShader>();
    }

    private void InitializeSystems()
    {
        _sceneSystem = SystemManager.GetSystem<SceneSystem>();
    }

    #endregion

    #region Shadow Rendering

    /// <summary>
    /// Renders the shadow map from this light's perspective.
    /// </summary>
    public void RenderShadowMap()
    {
        if (!CastShadow || _shadowMaterial == null || _shadowFrameBuffer == null)
            return;

        PrepareShadowRendering();
        RenderShadowPass();
    }

    private void PrepareShadowRendering()
    {
        ConfigureDepthTesting();
        BindShadowShader();
    }

    private void ConfigureDepthTesting()
    {
        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Less);
        GL.Clear(ClearBufferMask.DepthBufferBit);
    }

    private void BindShadowShader()
    {
        _shadowMaterial!.BindShader();
        _shadowMaterial.Shader.SetMatrix4("lightSpaceMatrix", GetLightSpaceMatrix());
    }

    private void RenderShadowPass()
    {
        _shadowFrameBuffer!.Bind();
        RenderSceneFromLightPerspective();
    }

    private void RenderSceneFromLightPerspective()
    {
        if (_sceneSystem == null)
            return;

        var renderers = _sceneSystem.CurrentScene.GetComponents<Renderer>();

        foreach (var renderer in renderers)
        {
            if (ShouldSkipRenderer(renderer))
                continue;

            RenderObjectShadow(renderer);
        }
    }

    private bool ShouldSkipRenderer(Renderer renderer)
    {
        return renderer.Material.RenderingMode == RenderingMode.TRANSPARENT;
    }

    private void RenderObjectShadow(Renderer renderer)
    {
        var modelMatrix = renderer.Transform.GetWorldTransformMatrix();
        _shadowMaterial!.Shader.SetMatrix4("model", modelMatrix);
        renderer.RenderShadow(_shadowMaterial.Shader);
    }

    /// <summary>
    /// Completes the shadow map rendering and restores the viewport.
    /// </summary>
    public void CompleteShadowMapRendering()
    {
        if (_shadowFrameBuffer == null)
            return;

        var viewportSize = Screen.GetViewportSize();
        _shadowFrameBuffer.Unbind(viewportSize.X, viewportSize.Y);
    }

    #endregion

    #region Light Space Matrix

    /// <summary>
    /// Calculates the light space transformation matrix for shadow mapping.
    /// </summary>
    /// <returns>The combined view-projection matrix from the light's perspective.</returns>
    public Matrix4 GetLightSpaceMatrix()
    {
        var lightViewMatrix = CalculateLightViewMatrix();
        var lightProjectionMatrix = CalculateLightProjectionMatrix();

        return lightViewMatrix * lightProjectionMatrix;
    }

    private Matrix4 CalculateLightViewMatrix()
    {
        var lightPosition = CalculateLightPosition();
        return Matrix4.LookAt(lightPosition, Transform.WorldPosition, Transform.Up);
    }

    private Vector3 CalculateLightPosition()
    {
        return Transform.WorldPosition + Transform.Forward * LIGHT_OFFSET_DISTANCE;
    }

    private Matrix4 CalculateLightProjectionMatrix()
    {
        return GetOrthographicProjectionMatrix();
    }

    private Matrix4 GetOrthographicProjectionMatrix()
    {
        var aspectRatio = Screen.GetAspectRatio();
        var orthoWidth = OrthographicWidth * aspectRatio;

        return Matrix4.CreateOrthographic(
            orthoWidth,
            OrthographicHeight,
            SHADOW_NEAR_PLANE,
            SHADOW_FAR_PLANE
        );
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets the RGB color components of this light as a Vector3.
    /// </summary>
    /// <returns>A Vector3 containing the R, G, B components.</returns>
    public Vector3 GetRGBColor()
    {
        return new Vector3(LightColor.R, LightColor.G, LightColor.B);
    }

    #endregion

    #region Disposal

    /// <summary>
    /// Releases GPU resources used by this light component.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        Console.WriteLine($"[Light] Disposing light: {GameObject?.Name ?? "Unknown"}");

        try
        {
            DisposeShadowFrameBuffer();
            DisposeShadowMaterial();

            _isDisposed = true;
            Console.WriteLine("[Light] ✓ Light disposed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Light] ❌ Error disposing light: {ex.Message}");
            Console.WriteLine($"[Light] Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private void DisposeShadowFrameBuffer()
    {
        if (_shadowFrameBuffer != null)
        {
            Console.WriteLine($"[Light]   Disposing shadow framebuffer (ShadowMap: {_shadowFrameBuffer.ShadowMap})...");
            _shadowFrameBuffer.Dispose();
            _shadowFrameBuffer = null;
            Console.WriteLine("[Light]   ✓ Shadow framebuffer disposed");
        }
    }

    private void DisposeShadowMaterial()
    {
        if (_shadowMaterial != null)
        {
            Console.WriteLine("[Light]   Disposing shadow material...");
            _shadowMaterial.Dispose();
            _shadowMaterial = null;
            Console.WriteLine("[Light]   ✓ Shadow material disposed");
        }
    }

    /// <summary>
    /// Called when the component is being destroyed.
    /// </summary>
    public override void Destroy()
    {
        base.Destroy();
        Dispose();
    }

    #endregion
}