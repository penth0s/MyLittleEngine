using Engine.Configs;

namespace Engine.Systems;

/// <summary>
/// Base interface for all rendering systems in the engine.
/// Defines the core rendering pipeline stages and rendering priority.
/// </summary>
internal interface IRenderSystem
{
    /// <summary>
    /// Gets the priority order for this rendering system.
    /// Lower values are rendered first. Default is 0.
    /// </summary>
    int RenderPriority => 0;

    /// <summary>
    /// Called once during initialization to set up rendering resources.
    /// </summary>
    void InitRender();

    /// <summary>
    /// Called every frame during the main rendering phase.
    /// </summary>
    void RenderUpdate();

    /// <summary>
    /// Called after the main rendering phase for additional rendering operations.
    /// </summary>
    void PostRenderUpdate();

    /// <summary>
    /// Called during the post-processing phase for screen-space effects.
    /// </summary>
    void PostProcessUpdate();
}

/// <summary>
/// Generic interface for rendering systems that require configuration.
/// Combines rendering system capabilities with configurable initialization.
/// </summary>
/// <typeparam name="TConfig">The type of render configuration this system uses.</typeparam>
internal interface IRenderSystem<TConfig> : IRenderSystem, ISystem<TConfig>
    where TConfig : RenderConfig
{
}