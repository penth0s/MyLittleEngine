using Adapters;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.Systems;

/// <summary>
/// Central event hub that manages communication between the engine and external systems (like the editor).
/// Provides engine information and coordinates event distribution throughout the application.
/// </summary>
internal sealed class EngineInfoProviderSystem : IGameEngineInfoProvider, ISystem
{
    #region Constants

    private const int DEFAULT_RESOLUTION_WIDTH = 1280;
    private const int DEFAULT_RESOLUTION_HEIGHT = 720;

    #endregion

    #region Events - EngineWindow

    /// <summary>
    /// Invoked every game frame with timing, keyboard, and mouse state information.
    /// </summary>
    public event IGameEngineInfoProvider.GameFrameUpdateHandler GameFrameUpdate = delegate { };

    #endregion

    #region Events - Input

    /// <summary>
    /// Invoked when the mouse wheel is scrolled.
    /// </summary>
    public event IGameEngineInfoProvider.MouseWheelHandler MouseWheel = delegate { };

    /// <summary>
    /// Invoked when text is input via the keyboard.
    /// </summary>
    public event IGameEngineInfoProvider.TextInputHandler TextInput = delegate { };

    #endregion

    #region Events - Editor

    /// <summary>
    /// Invoked when the editor window needs to be resized.
    /// </summary>
    public event IGameEngineInfoProvider.ResizeEditorHandler ResizeEditor = delegate { };

    /// <summary>
    /// Invoked when the editor should be drawn.
    /// </summary>
    public event IGameEngineInfoProvider.DrawEditorHandler DrawEditor = delegate { };

    /// <summary>
    /// Invoked when the inspector panel should be reset.
    /// </summary>
    public event EventHandler ResestInspector;

    /// <summary>
    /// Invoked when the before engine shut down
    /// </summary>
    public event Action EngineShutdown;

    #endregion

    #region Events - Scene Interaction

    /// <summary>
    /// Invoked when a raycast should be performed in the scene.
    /// </summary>
    public event IGameEngineInfoProvider.RayCastHandler RayCast = delegate { return Guid.Empty; };

    /// <summary>
    /// Invoked when an object in the scene is selected.
    /// </summary>
    public event EventHandler<Guid> SelectObject = delegate { };

    #endregion

    #region Events - Physics

    /// <summary>
    /// Invoked when a rigid body is created in the physics simulation.
    /// </summary>
    public event EventHandler<IPhysicsBodyListener> RigidBodyCreated = delegate { };

    /// <summary>
    /// Invoked when a rigid body is destroyed in the physics simulation.
    /// </summary>
    public event EventHandler<IPhysicsBody> RigidBodyDestroyed = delegate { };

    #endregion

    #region Events - Console & Lifecycle

    /// <summary>
    /// Invoked when a message should be logged to the console.
    /// </summary>
    public event EventHandler<ConsoleMessage> ConsoleMessage = delegate { };

    /// <summary>
    /// Invoked when the engine has completed initialization.
    /// </summary>
    public event EventHandler EngineInitialized = delegate { };

    #endregion

    #region Fields

    internal Vector2i Resolution = new(DEFAULT_RESOLUTION_WIDTH, DEFAULT_RESOLUTION_HEIGHT);

    internal bool IsGridEnabled;

    internal bool IsDebugViewEnabled;

    internal bool IsWireframeEnabled;

    #endregion

    #region ISystem Implementation

    /// <summary>
    /// Initializes the engine information provider system.
    /// </summary>
    public void Initialize()
    {
        // No initialization required - this system acts as an event hub
    }

    #endregion

    #region Resolution Information

    /// <summary>
    /// Gets the current rendering resolution.
    /// </summary>
    /// <returns>The resolution as a 2D vector (width, height).</returns>
    public Vector2i GetResolution()
    {
        return Resolution;
    }

    public bool GetIsGridEnabled()
    {
        return IsGridEnabled;
    }

    public bool GetIsDebugViewEnabled()
    {
        return IsDebugViewEnabled;
    }

    public bool GetIsWireframeEnabled()
    {
        return IsWireframeEnabled;
    }

    #endregion

    #region Event Triggers - Editor

    /// <summary>
    /// Triggers editor resize event.
    /// </summary>
    internal void OnResizeEditor(Vector2i newResolution)
    {
        ResizeEditor.Invoke(newResolution);
    }

    /// <summary>
    /// Triggers editor draw event.
    /// </summary>
    internal void OnDrawEditor(GameWindow window, int frameTextureId, FrameEventArgs frameEventArgs)
    {
        DrawEditor.Invoke(window, frameTextureId, frameEventArgs);
    }

    /// <summary>
    /// Requests that the inspector panel be reset.
    /// </summary>
    internal void OnNewSceneLoaded()
    {
        ResestInspector?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Engine ShutDown

    internal void OnEngineShutdown()
    {
        EngineShutdown?.Invoke();
    }

    #endregion

    #region Event Triggers - EngineWindow Loop

    /// <summary>
    /// Triggers engine initialization complete event.
    /// </summary>
    internal void OnEngineInitialized()
    {
        EngineInitialized.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Triggers game frame update event with input state.
    /// </summary>
    internal void OnGameFrameUpdate(
        FrameEventArgs frameEventArgs,
        KeyboardState keyboardState,
        MouseState mouseState)
    {
        GameFrameUpdate.Invoke(frameEventArgs, keyboardState, mouseState);
    }

    #endregion

    #region Event Triggers - Input

    /// <summary>
    /// Triggers mouse wheel scroll event.
    /// </summary>
    internal void OnMouseWheel(MouseWheelEventArgs mouseWheelEventArgs)
    {
        MouseWheel.Invoke(mouseWheelEventArgs);
    }

    /// <summary>
    /// Triggers text input event.
    /// </summary>
    internal void OnTextInput(TextInputEventArgs textInputEventArgs)
    {
        TextInput.Invoke(textInputEventArgs);
    }

    #endregion

    #region Event Triggers - Scene Interaction

    /// <summary>
    /// Performs a raycast in the scene and returns the ID of the hit object.
    /// </summary>
    /// <param name="rayOrigin">The origin point of the ray.</param>
    /// <param name="rayDirection">The direction of the ray.</param>
    /// <returns>The GUID of the hit object, or Guid.Empty if no hit.</returns>
    internal Guid OnRaycast(Vector3 rayOrigin, Vector3 rayDirection)
    {
        return RayCast.Invoke(rayOrigin, rayDirection);
    }

    /// <summary>
    /// Notifies that an object has been selected in the scene.
    /// </summary>
    internal void OnSelectObject(Guid objectId)
    {
        SelectObject.Invoke(this, objectId);
    }

    #endregion

    #region Event Triggers - Physics

    /// <summary>
    /// Notifies that a rigid body has been created.
    /// </summary>
    internal void OnRigidBodyCreated(IPhysicsBodyListener physicsBodyListener)
    {
        RigidBodyCreated.Invoke(this, physicsBodyListener);
    }

    /// <summary>
    /// Notifies that a rigid body has been destroyed.
    /// </summary>
    internal void OnRigidBodyDestroyed(IPhysicsBody physicsBody)
    {
        RigidBodyDestroyed.Invoke(this, physicsBody);
    }

    #endregion

    #region Event Triggers - Console

    /// <summary>
    /// Logs a message to the console.
    /// </summary>
    public void OnConsoleMessage(ConsoleMessage message)
    {
        ConsoleMessage.Invoke(this, message);
    }

    #endregion
}