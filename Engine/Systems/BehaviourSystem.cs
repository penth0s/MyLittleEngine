using Engine.Components;

namespace Engine.Systems;

/// <summary>
/// Manages the lifecycle and update cycles of all BeroBehaviour components in the game.
/// Handles initialization, frame updates, render updates, and shadow pass updates.
/// </summary>
internal sealed class BehaviourSystem : IRenderSystem, IGameUpdateSystem
{
    #region Fields

    private readonly List<BeroBehaviour> _registeredBehaviours = new();
    private RenderSystem _renderSystem;

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the behaviour system and subscribes to system initialization events.
    /// </summary>
    public void Initialize()
    {
        SystemManager.SystemInitializeCompleted += OnSystemInitializationCompleted;
    }

    private void OnSystemInitializationCompleted()
    {
        _renderSystem = SystemManager.GetSystem<RenderSystem>();
        _renderSystem.RenderShadowPass += OnShadowPassRender;
    }

    #endregion

    #region Registration

    /// <summary>
    /// Registers a behaviour to be managed by this system.
    /// </summary>
    /// <param name="behaviour">The behaviour to register.</param>
    public void Register(BeroBehaviour behaviour)
    {
        if (!_registeredBehaviours.Contains(behaviour)) _registeredBehaviours.Add(behaviour);
    }

    /// <summary>
    /// Unregisters a behaviour from this system.
    /// </summary>
    /// <param name="behaviour">The behaviour to unregister.</param>
    public void Unregister(BeroBehaviour behaviour)
    {
        _registeredBehaviours.Remove(behaviour);
    }

    #endregion

    #region Shadow Pass

    private void OnShadowPassRender()
    {
        foreach (var behaviour in _registeredBehaviours)
        {
            EnsureBehaviourInitialized(behaviour);

            if (behaviour.Enabled) behaviour.ShadowPassUpdate();
        }
    }

    #endregion

    #region IRenderSystem Implementation

    /// <summary>
    /// Called once at the start of rendering initialization.
    /// </summary>
    public void InitRender()
    {
        // No initialization needed for behaviours during render init
    }

    /// <summary>
    /// Called during the render update phase for all behaviours.
    /// </summary>
    public void RenderUpdate()
    {
        foreach (var behaviour in _registeredBehaviours)
        {
            EnsureBehaviourInitialized(behaviour);

            if (behaviour.Enabled) behaviour.RenderUpdate();
        }
    }

    /// <summary>
    /// Called after the main render update phase.
    /// </summary>
    public void PostRenderUpdate()
    {
        // No post-render logic needed for behaviours
    }

    /// <summary>
    /// Called during the post-processing phase.
    /// </summary>
    public void PostProcessUpdate()
    {
        // No post-process logic needed for behaviours
    }

    #endregion

    #region IGameUpdateSystem Implementation

    /// <summary>
    /// Called every frame to update all registered behaviours.
    /// </summary>
    public void FrameUpdate()
    {
        // Using indexed for loop to safely handle behaviours that might be removed during iteration
        for (var i = 0; i < _registeredBehaviours.Count; i++)
        {
            var behaviour = _registeredBehaviours[i];

            EnsureBehaviourInitialized(behaviour);

            if (behaviour.Enabled) behaviour.Update();
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Ensures that a behaviour has been properly initialized by calling Awake and Start if needed.
    /// </summary>
    /// <param name="behaviour">The behaviour to initialize.</param>
    private void EnsureBehaviourInitialized(BeroBehaviour behaviour)
    {
        behaviour.TryCallAwake();
        behaviour.TryCallStart();
    }

    #endregion
}