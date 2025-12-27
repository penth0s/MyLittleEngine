using Adapters;
using OpenTK.Mathematics;

namespace Engine.Core;

/// <summary>
/// Provides static access to screen and viewport information.
/// Acts as a centralized interface for querying display properties and editor state.
/// </summary>
public static class Screen
{
    #region Constants

    private const int DPI_SCALE_FACTOR = 2;

    #endregion

    #region Fields

    /// <summary>
    /// The current screen resolution scaled by DPI factor.
    /// </summary>
    public static Vector2i Resolution { get; internal set; }

    private static IEditorInfoProvier _editorInfoProvider;

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the Screen utility with editor information and base resolution.
    /// </summary>
    /// <param name="editorInfoProvider">Provider for editor state and viewport information.</param>
    /// <param name="baseResolution">The base resolution before DPI scaling.</param>
    internal static void Initialize(IEditorInfoProvier editorInfoProvider, Vector2i baseResolution)
    {
        ValidateEditorInfoProvider(editorInfoProvider);

        _editorInfoProvider = editorInfoProvider;
        Resolution = CalculateScaledResolution(baseResolution);
    }

    private static void ValidateEditorInfoProvider(IEditorInfoProvier editorInfoProvider)
    {
        if (editorInfoProvider == null)
            throw new ArgumentNullException(
                nameof(editorInfoProvider),
                "Editor info provider cannot be null."
            );
    }

    private static Vector2i CalculateScaledResolution(Vector2i baseResolution)
    {
        return baseResolution * DPI_SCALE_FACTOR;
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
    /// </summary>
    /// <returns>The viewport size as a 2D vector (width, height).</returns>
    /// <exception cref="InvalidOperationException">Thrown if Screen has not been initialized.</exception>
    public static Vector2i GetViewportSize()
    {
        ValidateInitialized();
        return _editorInfoProvider!.GetViewportSize();
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