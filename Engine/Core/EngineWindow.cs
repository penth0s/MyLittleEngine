using Adapters;
using Engine.Systems;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.Core;

/// <summary>
/// Main entry point for the game engine. Handles initialization, updates, rendering, and editor integration.
/// </summary>
public class EngineWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
    : GameWindow(gameWindowSettings, nativeWindowSettings)
{
    #region Constants

    public static float ScaleFactor = 0.01f;

    private const int FALLBACK_WIDTH = 1280;
    private const int FALLBACK_HEIGHT = 720;

    #endregion

    #region Fields

    private bool _isShuttingDown;

    private RenderSystem _renderSystem;
    private SceneSystem _sceneSystem;
    private EngineInfoProviderSystem _engineInfoProviderSystem;
    private IEditorInfoProvider _editorInfoProvier;
    
    #endregion

    #region Events
    
    internal static event Action EngineInitialized;
    
    #endregion

    #region Initialization
    
    public void InitEngine()
    {
        SystemManager.InitializeAllSystems();
    }


    protected override void OnLoad()
    {
        base.OnLoad();
        
        InitializeSystems();
        InitializeEditor();
        
        EngineInitialized?.Invoke();
        _sceneSystem.LoadScene();
    }

    private void InitializeSystems()
    {
        _renderSystem = SystemManager.GetSystem<RenderSystem>();
        _sceneSystem = SystemManager.GetSystem<SceneSystem>();
        _engineInfoProviderSystem = SystemManager.GetSystem<EngineInfoProviderSystem>();
    }

    private void InitializeEditor()
    {
        // OnLoad is the first point where the window is actually realized, so re-query
        // the sizes here. Before Run() the framebuffer can still report 0x0 on macOS.
        RefreshScreenMetrics();

        _engineInfoProviderSystem.Resolution = Screen.Resolution;
        _engineInfoProviderSystem.OnEngineInitialized();
    }

    /// <summary>
    /// Pulls the real framebuffer (physical pixels) and client (logical points) sizes
    /// from the window into <see cref="Screen"/>, falling back to a sane default if the
    /// platform has not produced a valid surface yet.
    /// </summary>
    private void RefreshScreenMetrics()
    {
        var framebufferSize = FramebufferSize;
        var logicalSize = ClientSize;

        if (logicalSize.X <= 0 || logicalSize.Y <= 0)
            logicalSize = new Vector2i(FALLBACK_WIDTH, FALLBACK_HEIGHT);

        // Some platforms report 0x0 for the framebuffer until the first real frame.
        if (framebufferSize.X <= 0 || framebufferSize.Y <= 0)
            framebufferSize = logicalSize;

        Screen.Resolution = framebufferSize;
        Screen.LogicalSize = logicalSize;

        Console.WriteLine(
            $"Screen metrics: framebuffer {framebufferSize.X}x{framebufferSize.Y}, " +
            $"client {logicalSize.X}x{logicalSize.Y}, dpi scale {Screen.DpiScale:0.##}");
    }

    public void ImportEditorProvider(IEditorInfoProvider editorInfoProvier)
    {
        _editorInfoProvier = editorInfoProvier;
        _editorInfoProvier.RaycastRequest += OnEditorRaycastRequest;

        // Ask the OS for the real sizes instead of assuming a 2x Retina scale.
        // These get refreshed again in OnLoad once the surface really exists.
        Screen.Initialize(_editorInfoProvier, FramebufferSize, ClientSize);
    }

    public IGameEngineInfoProvider GetEngineInfoProvider()
    {
        return SystemManager.GetSystem<EngineInfoProviderSystem>();
    }

    #endregion

    #region Editor Integration

    private void OnEditorRaycastRequest(object sender, EventArgs e)
    {
        if (_sceneSystem?.CurrentScene?.Camera == null)
            return;

        var camera = _sceneSystem.CurrentScene.Camera;
        var origin = camera.Transform.LocalPosition;
        var direction = camera.GetCameraRay();

        var rb = Physics.Physics.RayCast(origin, direction);
        if (rb != null) _engineInfoProviderSystem.OnSelectObject(rb.Id);
    }

    #endregion

    #region Rendering

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);

        if (_isShuttingDown)
            return;

        if (_sceneSystem.IsSceneReady)
        {
            var allRenderSystems = SystemManager.GetAllRenderSystemsSorted();
            var renderSystems = allRenderSystems as IRenderSystem[] ?? allRenderSystems.ToArray();
            ExecuteRenderStage(renderSystems, r => r.InitRender());
            ExecuteRenderStage(renderSystems, r => r.RenderUpdate());
            ExecuteRenderStage(renderSystems, r => r.PostRenderUpdate());
            ExecuteRenderStage(renderSystems, r => r.PostProcessUpdate());
        }

        RenderEditorView(e);
        SwapBuffers();
    }

    private static void ExecuteRenderStage(IEnumerable<IRenderSystem> renderSystems, Action<IRenderSystem> action)
    {
        foreach (var system in renderSystems) action(system);
    }


    private void RenderEditorView(FrameEventArgs e)
    {
        if (_sceneSystem?.CurrentScene == null || _engineInfoProviderSystem == null)
            return;

        GL.ClearColor(_sceneSystem.CurrentScene.ClearColor);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        GL.Viewport(0, 0, Screen.Resolution.X, Screen.Resolution.Y);
        _engineInfoProviderSystem.OnDrawEditor(this, _renderSystem.FrameBuffer.ColorTexture, e);

        var editorViewport = _editorInfoProvier.GetViewportSize();
        GL.Viewport(0, 0, editorViewport.X, editorViewport.Y);
    }

    #endregion

    #region Update Loop

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);

        if (_isShuttingDown)
            return;

        if (!IsFocused)
            return;

        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            // Only ask the window to close. Tearing down GPU resources here would leave
            // the render frame that still runs this iteration drawing with disposed
            // framebuffers/meshes. The actual shutdown happens in OnUnload.
            BeginShutdown();
            return;
        }

        Title = _sceneSystem.CurrentScene.SceneName;
        UpdateEngineState(e);

        if (_sceneSystem.IsSceneReady)
        {
            ExecuteAllGameUpdates();
            _engineInfoProviderSystem.OnGameFrameUpdate(e, KeyboardState, MouseState);
        }
    }

    private void UpdateEngineState(FrameEventArgs e)
    {
        Time.FrameEvent = e;
        Input.Keybord = KeyboardState;
        Input.Mouse = MouseState;
    }

    private void ExecuteAllGameUpdates()
    {
        var updateSystems = SystemManager.GetAllGameUpdateSystemsSorted();
        foreach (var system in updateSystems) system.FrameUpdate();
    }

    #endregion

    #region Shutdown

    /// <summary>
    /// Requests a clean shutdown. Only flips the flag and asks GLFW to close the window -
    /// no GPU resource is touched here, because the current loop iteration may still run a
    /// render frame. The real teardown happens in <see cref="OnUnload"/>, once the loop has
    /// exited and nothing else will draw.
    /// </summary>
    private void BeginShutdown()
    {
        if (_isShuttingDown)
            return;

        _isShuttingDown = true;
        Close();
    }

    /// <summary>
    /// Called by OpenTK after the run loop has ended, for any close reason (Escape, the
    /// window's close button, or the OS). This is the only safe place to release
    /// framebuffers, meshes, shaders and the ImGui backend.
    /// </summary>
    protected override void OnUnload()
    {
        _isShuttingDown = true;

        _engineInfoProviderSystem?.OnEngineShutdown();

        base.OnUnload();
    }

    #endregion

    #region Resize / Input Events

    protected override void OnFramebufferResize(FramebufferResizeEventArgs e)
    {
        base.OnFramebufferResize(e);

        if (_isShuttingDown)
            return;

        if (e.Width <= 0 || e.Height <= 0)
            return;

        Console.WriteLine($"Framebuffer resized to {e.Width}x{e.Height} (client {ClientSize.X}x{ClientSize.Y})");
        Screen.Resolution = new Vector2i(e.Width, e.Height);
        Screen.LogicalSize = ClientSize;

        _engineInfoProviderSystem.OnResizeEditor(Screen.Resolution);
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        if (_isShuttingDown)
            return;

        _engineInfoProviderSystem.OnTextInput(e);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        if (_isShuttingDown)
            return;

        _engineInfoProviderSystem.OnMouseWheel(e);
    }

    #endregion
}