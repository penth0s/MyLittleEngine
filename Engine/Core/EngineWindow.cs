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

    #endregion

    #region Fields

    private RenderSystem _renderSystem;
    private SceneSystem _sceneSystem;
    private EngineInfoProviderSystem _engineInfoProviderSystem;
    private IEditorInfoProvier _editorInfoProvier;
    
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
        _engineInfoProviderSystem.Resolution = Screen.Resolution;
        _engineInfoProviderSystem.OnEngineInitialized();
    }
    
    public void ImportEditorProvider(IEditorInfoProvier editorInfoProvier)
    {
        _editorInfoProvier = editorInfoProvier;
        _editorInfoProvier.RaycastRequest += OnEditorRaycastRequest;

        Screen.Initialize(_editorInfoProvier, ClientSize);
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

        if (!IsFocused)
            return;

        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            _engineInfoProviderSystem.OnEngineShutdown();
            Close();
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

    #region Resize / Input Events

    protected override void OnFramebufferResize(FramebufferResizeEventArgs e)
    {
        base.OnFramebufferResize(e);

        if (e.Width <= 0 || e.Height <= 0)
            return;

        Console.WriteLine($"Framebuffer resized to {e.Width}x{e.Height}");
        Screen.Resolution = new Vector2i(e.Width, e.Height);

        _engineInfoProviderSystem.OnResizeEditor(Screen.Resolution);
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        _engineInfoProviderSystem.OnTextInput(e);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        _engineInfoProviderSystem.OnMouseWheel(e);
    }

    #endregion
}