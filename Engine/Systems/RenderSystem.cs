using Engine.Components;
using Engine.Configs;
using Engine.Core;
using Engine.Rendering;
using OpenTK.Graphics.OpenGL4;
using Environment = Engine.Scene.Environment;
using RenderingMode = Engine.Rendering.RenderingMode;

namespace Engine.Systems;

/// <summary>
/// Main rendering system that manages the rendering pipeline.
/// Handles forward rendering, shadow mapping, transparency, and debug visualization.
/// </summary>
public sealed class RenderSystem : IRenderSystem<RenderConfig>
{
    #region Properties

    /// <summary>
    /// Gets the framebuffer used for off-screen rendering.
    /// </summary>
    public FrameBuffer FrameBuffer { get; internal set; }

    /// <summary>
    /// Gets the render priority for this system. Lower values render first.
    /// </summary>
    public int RenderPriority { get; } = -1;

    #endregion

    #region Events

    /// <summary>
    /// Event invoked before the main render pass.
    /// </summary>
    public event Action PreRenderPass;

    /// <summary>
    /// Event invoked during shadow map rendering.
    /// </summary>
    public event Action RenderShadowPass;

    /// <summary>
    /// Event invoked after the main render pass.
    /// </summary>
    public event Action PostRenderPass;

    /// <summary>
    /// Event invoked during debug rendering.
    /// </summary>
    public event Action DebugRenderPass;

    #endregion

    #region Fields

    private SceneSystem _sceneSystem;
    private Grid _grid;

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the render system with the specified configuration.
    /// </summary>
    public void Initialize(RenderConfig config)
    {
        EngineWindow.EngineInitialized += OnEngineInitialized;
    }

    private void OnEngineInitialized()
    {
        ConfigureOpenGLState();
        InitializeFrameBuffer();
        InitializeSystems();
        InitializeRenderers();
    }

    private void OnEngineShutdown()
    {
        FrameBuffer.Dispose();
        _grid.Dispose();
        DebugRenderer.Cleanup();
    }

    private void ConfigureOpenGLState()
    {
        GL.Enable(EnableCap.DepthTest);
    }

    private void InitializeFrameBuffer()
    {
        var viewportSize = Screen.GetViewportSize();
        FrameBuffer = new FrameBuffer(viewportSize.X, viewportSize.Y);
    }

    private void InitializeSystems()
    {
        _sceneSystem = SystemManager.GetSystem<SceneSystem>();
    }

    private void InitializeRenderers()
    {
        Environment.Initialize();
        DebugRenderer.Initialize();
        ScreenQuadRenderer.Initialize();

        _grid = new Grid();
    }

    #endregion

    #region Render Pipeline

    /// <summary>
    /// Initializes the render pass by invoking pre-render events and opening the framebuffer.
    /// </summary>
    public void InitRender()
    {
        PreRenderPass?.Invoke();
        BindFrameBuffer();
    }

    /// <summary>
    /// Performs the main rendering operations.
    /// </summary>
    public void RenderUpdate()
    {
        ExecuteForwardRenderPass();
        GizmoRendering();
    }

    /// <summary>
    /// Finalizes the render pass by closing the framebuffer.
    /// </summary>
    public void PostRenderUpdate()
    {
        UnbindFrameBuffer();
    }

    /// <summary>
    /// Performs post-processing operations.
    /// </summary>
    public void PostProcessUpdate()
    {
        PostRenderPass?.Invoke();
    }

    private void BindFrameBuffer()
    {
        FrameBuffer.Bind();
    }

    private void UnbindFrameBuffer()
    {
        FrameBuffer.Unbind();
    }

    private void ExecuteDebugRenderPass()
    {
        DebugRenderPass?.Invoke();
    }

    private void GizmoRendering()
    {
        if (Screen.IsDebugView()) ExecuteDebugRenderPass();

        if (Screen.IsGridActive()) _grid.Draw();
    }

    #endregion

    #region Forward Rendering

    private void ExecuteForwardRenderPass()
    {
        ConfigureForwardRenderingState();
        RenderSkybox();
        RenderAllShadowMaps();
        RenderOpaqueGeometry();
        RenderTransparentGeometry();
        RestoreDefaultRenderingState();
    }

    private void ConfigureForwardRenderingState()
    {
        GL.Enable(EnableCap.DepthTest);
        GL.DepthMask(true);
        GL.Disable(EnableCap.Blend);
        GL.DepthFunc(DepthFunction.Less);
    }

    private void RenderSkybox()
    {
        if (_sceneSystem.CurrentScene.IsSkybox) Environment.RenderSkybox(_sceneSystem.CurrentScene.Camera);
    }

    private void RenderOpaqueGeometry()
    {
        var camera = _sceneSystem.CurrentScene.Camera;
        var renderers = _sceneSystem.CurrentScene.GetComponents<Renderer>();

        foreach (var renderer in renderers)
            if (IsOpaqueRenderer(renderer))
                renderer.Render(camera);
    }

    private bool IsOpaqueRenderer(Renderer renderer)
    {
        var mode = renderer.Material.RenderingMode;
        return mode == RenderingMode.OPAQUE || mode == RenderingMode.CUTOFF;
    }

    private void RenderTransparentGeometry()
    {
        ConfigureTransparencyState();
        RenderTransparentObjects();
        DisableTransparency();
    }

    private void ConfigureTransparencyState()
    {
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.DepthMask(false);
    }

    private void RenderTransparentObjects()
    {
        var camera = _sceneSystem.CurrentScene.Camera;
        var renderers = _sceneSystem.CurrentScene.GetComponents<Renderer>();

        foreach (var renderer in renderers)
            if (renderer.Material.RenderingMode == RenderingMode.TRANSPARENT)
                renderer.Render(camera);
    }

    private void DisableTransparency()
    {
        GL.DepthMask(true);
        GL.Disable(EnableCap.Blend);
    }

    private void RestoreDefaultRenderingState()
    {
        GL.DepthMask(true);
        GL.Disable(EnableCap.Blend);
    }

    #endregion

    #region Shadow Mapping

    private void RenderAllShadowMaps()
    {
        var lights = _sceneSystem.CurrentScene.GeSceneLights;

        RenderShadowMapsForAllLights(lights);
        CompleteShadowMapRendering(lights);
    }

    private void RenderShadowMapsForAllLights(List<Light> lights)
    {
        foreach (var light in lights)
        {
            light.RenderShadowMap();
            RenderShadowPass?.Invoke();
        }
    }

    private void CompleteShadowMapRendering(List<Light> lights)
    {
        foreach (var light in lights) light.CompleteShadowMapRendering();
    }

    #endregion

    #region Public Rendering Methods

    /// <summary>
    /// Renders only opaque objects from the specified camera's perspective.
    /// Used for specific rendering contexts that don't need the full pipeline.
    /// </summary>
    /// <param name="camera">The camera to render from.</param>
    public void RenderOpaqueObjects(Camera camera)
    {
        ClearBuffers();
        RenderSkyboxForCamera(camera);
        ConfigureOpaqueRenderingState();
        RenderOpaqueObjectsForCamera(camera);
    }

    private void ClearBuffers()
    {
        GL.ClearColor(_sceneSystem.CurrentScene.ClearColor);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    private void RenderSkyboxForCamera(Camera camera)
    {
        if (_sceneSystem.CurrentScene.IsSkybox) Environment.RenderSkybox(camera);
    }

    private void ConfigureOpaqueRenderingState()
    {
        GL.Enable(EnableCap.DepthTest);
        GL.DepthMask(true);
        GL.Disable(EnableCap.Blend);
        GL.DepthFunc(DepthFunction.Less);
    }

    private void RenderOpaqueObjectsForCamera(Camera camera)
    {
        var renderers = _sceneSystem.CurrentScene.GetComponents<Renderer>();

        foreach (var renderer in renderers)
            if (IsOpaqueRenderer(renderer))
                renderer.Render(camera);
    }

    #endregion
}