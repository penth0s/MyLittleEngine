using System.Runtime.Serialization;
using Adapters;
using Engine.Systems;

namespace Engine.Components;

/// <summary>
/// Base class for all user-defined behaviour scripts in the engine.
/// Provides lifecycle methods (Awake, Start, Update) and integration with the behaviour system.
/// </summary>
public abstract class BeroBehaviour : Component
{
    #region Fields

    private bool _hasAwakeCalled;
    private bool _hasStartCalled;

    private BehaviourSystem _behaviourSystem;
    private EngineInfoProviderSystem _engineInfoProviderSystem;

    #endregion

    #region Initialization

    protected BeroBehaviour()
    {
        InitializeSystems();
        RegisterWithBehaviourSystem();
    }

    private void InitializeSystems()
    {
        _engineInfoProviderSystem = SystemManager.GetSystem<EngineInfoProviderSystem>();
        _behaviourSystem = SystemManager.GetSystem<BehaviourSystem>();
    }

    private void RegisterWithBehaviourSystem()
    {
        _behaviourSystem?.Register(this);
    }

    /// <summary>
    /// Called after deserialization to restore system references.
    /// </summary>
    [OnDeserialized]
    internal void OnDeserializedMethod(StreamingContext context)
    {
        _behaviourSystem = SystemManager.GetSystem<BehaviourSystem>();
    }

    #endregion

    #region Lifecycle Management

    /// <summary>
    /// Attempts to call Awake if it hasn't been called yet.
    /// Called by the BehaviourSystem before the first Update.
    /// </summary>
    internal void TryCallAwake()
    {
        if (_hasAwakeCalled)
            return;

        _hasAwakeCalled = true;
        Awake();
    }

    /// <summary>
    /// Attempts to call Start if Awake has been called and Start hasn't been called yet.
    /// Called by the BehaviourSystem after Awake and before the first Update.
    /// </summary>
    internal void TryCallStart()
    {
        if (!_hasAwakeCalled || _hasStartCalled)
            return;

        _hasStartCalled = true;
        Start();
    }

    #endregion

    #region Destruction

    /// <summary>
    /// Called when the component is being destroyed.
    /// Override this method to implement custom cleanup logic.
    /// </summary>
    public virtual void OnDestroy()
    {
    }

    /// <summary>
    /// Destroys this component and unregisters it from the behaviour system.
    /// </summary>
    public override void Destroy()
    {
        base.Destroy();
        OnDestroy();
        UnregisterFromBehaviourSystem();
    }

    private void UnregisterFromBehaviourSystem()
    {
        _behaviourSystem?.Unregister(this);
    }

    #endregion

    #region Debug Utilities

    /// <summary>
    /// Prints a message to the engine console.
    /// </summary>
    /// <param name="message">The message to print.</param>
    protected void Print(string message)
    {
        var consoleMessage = new ConsoleMessage(message, LogType.Log);
        _engineInfoProviderSystem?.OnConsoleMessage(consoleMessage);
    }

    #endregion

    #region Virtual Lifecycle Methods

    /// <summary>
    /// Called when the behaviour is first initialized, before Start.
    /// Use this for initialization that doesn't depend on other components.
    /// </summary>
    protected virtual void Awake()
    {
    }

    /// <summary>
    /// Called before the first frame update, after Awake.
    /// Use this for initialization that depends on other components being initialized.
    /// </summary>
    protected virtual void Start()
    {
    }

    /// <summary>
    /// Called every frame if the behaviour is enabled.
    /// Use this for game logic that should run every frame.
    /// </summary>
    public virtual void Update()
    {
    }

    /// <summary>
    /// Called during the render update phase if the behaviour is enabled.
    /// Use this for rendering-related logic.
    /// </summary>
    public virtual void RenderUpdate()
    {
    }

    /// <summary>
    /// Called during the shadow pass render phase if the behaviour is enabled.
    /// Use this for shadow-specific rendering logic.
    /// </summary>
    public virtual void ShadowPassUpdate()
    {
    }

    #endregion
}