namespace Editor.GUI;

/// <summary>
/// Single source of truth for DPI dependent editor UI sizing.
/// <para>
/// ImGui here runs with <c>io.DisplaySize</c> in physical framebuffer pixels, so every
/// pixel constant used for fonts, window sizes and paddings has to be multiplied by the
/// display's DPI scale. That scale is read from the window each frame (see
/// <see cref="ImGuiController.DpiScale"/>) - it must never be hardcoded to 2, otherwise
/// the editor only lays out correctly on Retina displays and breaks on every other one.
/// </para>
/// </summary>
internal static class EditorUI
{
    /// <summary>
    /// Physical pixels per logical point. Updated every frame from the live window.
    /// </summary>
    public static float DpiScale { get; internal set; } = 1.0f;

    /// <summary>
    /// Font scale to pass to <c>ImGui.SetWindowFontScale</c>.
    /// </summary>
    public static float FontScale => DpiScale;

    /// <summary>
    /// Converts a size authored in logical points into framebuffer pixels.
    /// </summary>
    public static float Scaled(float logicalPixels)
    {
        return logicalPixels * DpiScale;
    }

    /// <summary>
    /// Converts a 2D size authored in logical points into framebuffer pixels.
    /// </summary>
    public static System.Numerics.Vector2 Scaled(float logicalWidth, float logicalHeight)
    {
        return new System.Numerics.Vector2(logicalWidth * DpiScale, logicalHeight * DpiScale);
    }
}
