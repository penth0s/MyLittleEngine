using Adapters;
using OpenTK.Mathematics;

namespace Editor.Core;

/// <summary>
/// Provides editor state information and viewport data to the engine.
/// Acts as a bridge between the editor UI and the rendering system.
/// </summary>
public class EditorInfoProvider : IEditorInfoProvier
{
    #region Fields

    private Vector2i _viewportSize;
    private Vector2 _viewportNdc;
    private bool _isDebugViewEnabled;
    private bool _isWireframeEnabled;

    #endregion

    #region Properties

    /// <summary>
    /// Gets whether debug view is currently enabled.
    /// </summary>
    public bool IsDebugView => _isDebugViewEnabled;

    /// <summary>
    /// Gets whether wireframe rendering is currently active.
    /// </summary>
    public bool IsWireFrameActive => _isWireframeEnabled;

    /// <summary>
    /// Gets or sets the viewport size in pixels.
    /// </summary>
    internal Vector2i Viewport
    {
        set => _viewportSize = value;
    }

    /// <summary>
    /// Gets or sets the viewport normalized device coordinates (NDC) of the mouse cursor.
    /// Range: [0, 1] for both X and Y coordinates.
    /// </summary>
    internal Vector2 ViewportNdc
    {
        set => _viewportNdc = value;
    }

    /// <summary>
    /// Gets or sets whether debug view is enabled.
    /// Internal setter allows the Editor to control this state.
    /// </summary>
    internal bool IsDebugViewEnabled
    {
        set => _isDebugViewEnabled = value;
    }

    /// <summary>
    /// Gets or sets whether wireframe rendering is enabled.
    /// Internal setter allows the Editor to control this state.
    /// </summary>
    internal bool IsWireframeEnabled
    {
        set => _isWireframeEnabled = value;
    }

    /// <summary>
    /// Gets or sets whether Grid is enabled.
    /// Internal setter allows the Editor to control this state.
    /// </summary>
    internal bool IsGridEnabled
    {
        set => _isWireframeEnabled = value;
    }

    #endregion

    #region Events

    /// <summary>
    /// Event raised when a raycast is requested from the scene view.
    /// Typically triggered by double-clicking in the viewport.
    /// </summary>
    public EventHandler RaycastRequest { get; set; }

    #endregion

    #region Public Methods

    /// <summary>
    /// Calculates the current viewport aspect ratio (width / height).
    /// </summary>
    /// <returns>The aspect ratio as a float value.</returns>
    public float GetAspectRatio()
    {
        if (_viewportSize.Y == 0)
            return 1.0f; // Prevent division by zero

        return (float)_viewportSize.X / _viewportSize.Y;
    }

    /// <summary>
    /// Gets the current viewport size in pixels.
    /// </summary>
    /// <returns>Vector containing width (X) and height (Y) in pixels.</returns>
    public Vector2i GetViewportSize()
    {
        return _viewportSize;
    }

    /// <summary>
    /// Gets the current mouse position in normalized device coordinates (NDC).
    /// </summary>
    /// <returns>Vector with X and Y coordinates in [0, 1] range.</returns>
    public Vector2 GetViewportNdc()
    {
        return _viewportNdc;
    }

    /// <summary>
    /// Handles double-click events in the scene view.
    /// Triggers a raycast request for object selection.
    /// </summary>
    public void OnSceneViewDoubleClicked()
    {
        RaycastRequest.Invoke(this, EventArgs.Empty);
    }

    #endregion
}