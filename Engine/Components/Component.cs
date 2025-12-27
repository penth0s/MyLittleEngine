using Engine.Core;
using Newtonsoft.Json;

namespace Engine.Components;

/// <summary>
/// Base class for all components that can be attached to GameObjects.
/// Components define the behavior and properties of GameObjects in the scene.
/// </summary>
public abstract class Component
{
    #region Fields

    /// <summary>
    /// The GameObject this component is attached to.
    /// </summary>
    [JsonIgnore]
    public GameObject GameObject { get; private set; } = null!;

    #endregion

    #region Properties

    /// <summary>
    /// Shorthand access to the Transform component of the GameObject this component is attached to.
    /// </summary>
    [JsonIgnore]
    public Transform Transform => GameObject.Transform;

    /// <summary>
    /// Controls whether this behaviour is active and should receive update calls.
    /// </summary>
    public bool Enabled = true;

    /// <summary>
    /// Determines the order in which components are saved during serialization.
    /// Lower values are saved first. Components with negative values are typically not saved.
    /// </summary>
    [JsonIgnore]
    public virtual int SavePriority => 0;

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes a new instance of the Component class.
    /// </summary>
    protected Component()
    {
    }

    /// <summary>
    /// Initializes this component with its owning GameObject.
    /// This method is called internally when the component is added to a GameObject.
    /// </summary>
    /// <param name="owner">The GameObject that owns this component.</param>
    /// <exception cref="InvalidOperationException">Thrown if the component is already attached to a GameObject.</exception>
    /// <exception cref="ArgumentNullException">Thrown if the owner is null.</exception>
    internal virtual void Initialize(GameObject owner)
    {
        ValidateNotAlreadyAttached();
        ValidateOwnerNotNull(owner);

        GameObject = owner;
    }

    private void ValidateNotAlreadyAttached()
    {
        if (GameObject != null)
            throw new InvalidOperationException(
                "Component is already attached to a GameObject. " +
                "A component cannot be attached to multiple GameObjects."
            );
    }

    private void ValidateOwnerNotNull(GameObject owner)
    {
        if (owner == null)
            throw new ArgumentNullException(
                nameof(owner),
                "Cannot initialize component with a null GameObject."
            );
    }

    #endregion

    #region Lifecycle

    /// <summary>
    /// Called before the component is serialized to prepare any data for saving.
    /// Override this method to implement custom save preparation logic.
    /// </summary>
    public virtual void PrepareSaveData()
    {
    }

    /// <summary>
    /// Called when the component is being destroyed.
    /// Override this method to implement custom cleanup logic.
    /// </summary>
    public virtual void Destroy()
    {
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets a component of the specified type from the GameObject this component is attached to.
    /// </summary>
    /// <typeparam name="T">The type of component to retrieve.</typeparam>
    /// <returns>The component if found; otherwise, null.</returns>
    protected T GetComponent<T>() where T : Component
    {
        return GameObject.GetComponent<T>();
    }

    #endregion
}