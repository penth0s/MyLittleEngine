using Adapters;
using OpenTK.Mathematics;

namespace Engine.Core;

/// <summary>
/// Provides static access to screen and viewport information.
/// Acts as a centralized interface for querying display properties and editor state.
/// </summary>
public static class Screen
{
    #region Fields

    /// <summary>
    /// The current framebuffer resolution, in physical pixels.
    /// On HiDPI/Retina displays this is larger than <see cref="LogicalSize"/>;
    /// the ratio between them is exposed as <see cref="DpiScale"/>.
    /// </summary>
    public static Vector2i Resolution { get; internal set; }

    /// <summary>
    /// The logical (device independent) size of the window, in points.
    /// This is the coordinate space mouse input arrives in.
    /// </summary>
    public static Vector2i LogicalSize { get; internal set; }

    /// <summary>
    /// Physical pixels per logical point. 1.0 on a standard display, 2.0 on Retina.
    /// Queried from the OS at runtime - never hardcode this.
    /// </summary>
    public static float DpiScale =>
        LogicalSize.X > 0 ? (float)Resolution.X / LogicalSize.X : 1.0f;

    private static IEditorInfoProvider _editorInfoProvider;

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the Screen utility with editor information and the real window sizes.
    /// </summary>
    /// <param name="editorInfoProvider">Provider for editor state and viewport information.</param>
    /// <param name="framebufferSize">The framebuffer size in physical pixels.</param>
    /// <param name="logicalSize">The window client size in logical points.</param>
    internal static void Initialize(
        IEditorInfoProvider editorInfoProvider,
        Vector2i framebufferSize,
        Vector2i logicalSize)
    {
        ValidateEditorInfoProvider(editorInfoProvider);

        _editorInfoProvider = editorInfoProvider;
        Resolution = framebufferSize;
        LogicalSize = logicalSize;
    }

    private static void ValidateEditorInfoProvider(IEditorInfoProvider editorInfoProvider)
    {
        if (editorInfoProvider == null)
            throw new ArgumentNullException(
                nameof(editorInfoProvider),
                "Editor info provider cannot be null."
            );
    }

    #endregion

    #region Editor State Queries

    /// <summary>
    /// Checks whether debug visualization is currently enabled.
    /// </summary>
    /// <returns>True if debug view is active; otherwise, false.</returns>
    public static bool IsDebugView()
    {
        return _editorInfoProvider?.IsDebugView ?? false;
    }

    /// <summary>
    /// Checks whether wireframe rendering mode is currently enabled.
    /// </summary>
    /// <returns>True if wireframe mode is active; otherwise, false.</returns>
    public static bool IsWireFrameActive()
    {
        return _editorInfoProvider?.IsWireFrameActive ?? false;
    }

    /// <summary>
    /// Checks whether grid rendering mode is currently enabled.
    /// </summary>
    /// <returns>True if grid is active; otherwise, false.</returns>
    public static bool IsGridActive()
    {
        return _editorInfoProvider?.IsWireFrameActive ?? false;
    }

    #endregion

    #region Viewport Information

    /// <summary>
    /// Gets the current viewport aspect ratio (width / height).
    /// </summary>
    /// <returns>The aspect ratio as a float value.</returns>
    /// <exception cref="InvalidOperationException">Thrown if Screen has not been initialized.</exception>
    public static float GetAspectRatio()
    {
        ValidateInitialized();
        return _editorInfoProvider!.GetAspectRatio();
    }

    /// <summary>
    /// Gets the current viewport size in pixels.
    /// Guaranteed to be at least 1x1: during the very first frame the editor has not
    /// laid out its Scene View yet, and a 0-sized OpenGL framebuffer is invalid
    /// (FramebufferIncompleteAttachment). Framebuffers resize themselves once the real
    /// viewport is known, so a small starting size is harmless.
    /// </summary>
    /// <returns>The viewport size as a 2D vector (width, height).</returns>
    /// <exception cref="InvalidOperationException">Thrown if Screen has not been initialized.</exception>
    public static Vector2i GetViewportSize()
    {
        ValidateInitialized();

        var viewportSize = _editorInfoProvider!.GetViewportSize();

        if (viewportSize.X > 0 && viewportSize.Y > 0)
            return viewportSize;

        // Fall back to the full framebuffer while the editor layout is still settling.
        if (Resolution.X > 0 && Resolution.Y > 0)
            return Resolution;

        return Vector2i.One;
    }

    /// <summary>
    /// Gets the current mouse position in normalized device coordinates (NDC).
    /// NDC ranges from [0,0] at the bottom-left to [1,1] at the top-right of the viewport.
    /// </summary>
    /// <returns>The mouse position in NDC space.</returns>
    /// <exception cref="InvalidOperationException">Thrown if Screen has not been initialized.</exception>
    public static Vector2 GetViewportNdc()
    {
        ValidateInitialized();
        return _editorInfoProvider!.GetViewportNdc();
    }

    #endregion

    #region Helper Methods

    private static void ValidateInitialized()
    {
        if (_editorInfoProvider == null)
            throw new InvalidOperationException(
                "Screen has not been initialized. Call Initialize() before accessing viewport information."
            );
    }

    #endregion
}