#nullable disable

using System.Runtime.Serialization;
using Engine.Components;
using Newtonsoft.Json;
using OpenTK.Mathematics;
using static System.Activator;

namespace Engine.Core;

/// <summary>
/// Represents an entity in the game world that can have components attached to it.
/// GameObjects are the fundamental building blocks of scenes and provide a container for components.
/// </summary>
public sealed class GameObject
{
    #region Constants

    private const string DEFAULT_GAMEOBJECT_NAME = "GameObject";

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the unique identifier for this GameObject.
    /// </summary>
    public Guid ID { get; set; }

    /// <summary>
    /// Gets or sets whether this GameObject and its components are active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the name of this GameObject.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets the Transform component attached to this GameObject.
    /// Every GameObject must have a Transform component.
    /// </summary>
    [JsonIgnore]
    public Transform Transform => GetComponent<Transform>()
                                  ?? throw new InvalidOperationException(
                                      "Transform component not found on GameObject.");

    #endregion

    #region Fields

    [JsonProperty] private List<Component> _components;

    #endregion

    #region Events

    /// <summary>
    /// Event raised when this GameObject is destroyed.
    /// </summary>
    public event Action<GameObject> GameObjectDestroyed;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the GameObject class with default transform.
    /// </summary>
    public GameObject()
    {
        InitializeGameObject();
        AddComponent<Transform>();
    }

    /// <summary>
    /// Initializes a new instance of the GameObject class with the specified transform matrix.
    /// </summary>
    /// <param name="transformMatrix">The initial transform matrix for this GameObject.</param>
    public GameObject(Matrix4 transformMatrix)
    {
        InitializeGameObject();
        AddComponent<Transform>(transformMatrix);
    }

    private void InitializeGameObject()
    {
        _components = new List<Component>();
        Name = DEFAULT_GAMEOBJECT_NAME;
        ID = Guid.NewGuid();
    }

    #endregion

    #region Serialization

    /// <summary>
    /// Called after deserialization to reinitialize component references.
    /// </summary>
    [OnDeserialized]
    internal void OnDeserialized(StreamingContext context)
    {
        InitializeComponentsAfterDeserialization();
    }

    private void InitializeComponentsAfterDeserialization()
    {
        foreach (var component in _components) component.Initialize(this);
    }

    /// <summary>
    /// Serializes this GameObject and its components to JSON format.
    /// </summary>
    /// <returns>A JSON string representation of this GameObject.</returns>
    public string GetSaveData()
    {
        var settings = CreateSerializationSettings();
        PrepareComponentsForSerialization();

        return JsonConvert.SerializeObject(this, settings);
    }

    private JsonSerializerSettings CreateSerializationSettings()
    {
        return new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
    }

    private void PrepareComponentsForSerialization()
    {
        foreach (var component in _components.OrderBy(c => c.SavePriority)) component.PrepareSaveData();
    }

    #endregion

    #region Lifecycle Management

    /// <summary>
    /// Sets the active state of this GameObject and all its children.
    /// </summary>
    /// <param name="isActive">True to activate, false to deactivate.</param>
    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        PropagateActiveStateToChildren(isActive);
    }

    private void PropagateActiveStateToChildren(bool isActive)
    {
        foreach (var childTransform in Transform.Children) childTransform.GameObject.SetActive(isActive);
    }

    /// <summary>
    /// Destroys this GameObject, all its children, and all attached components.
    /// </summary>
    public void Destroy()
    {
        DetachFromParent();
        DestroyAllChildren();
        DestroyAllComponents();
        ClearGameObject();

        GameObjectDestroyed?.Invoke(this);
    }

    private void DetachFromParent()
    {
        if (Transform.Parent != null) Transform.SetParent(null);
    }

    private void DestroyAllChildren()
    {
        var childrenCopy = new List<Transform>(Transform.Children);

        foreach (var childTransform in childrenCopy) childTransform.GameObject.Destroy();
    }

    private void DestroyAllComponents()
    {
        foreach (var component in _components) component.Destroy();
    }

    private void ClearGameObject()
    {
        IsActive = false;
        _components.Clear();
    }

    #endregion

    #region Component Management - Add

    /// <summary>
    /// Adds a component of the specified type to this GameObject.
    /// </summary>
    /// <typeparam name="T">The type of component to add.</typeparam>
    /// <param name="args">Optional constructor arguments for the component.</param>
    /// <returns>The newly added component instance.</returns>
    public T AddComponent<T>(params object[] args) where T : Component
    {
        var component = CreateComponentInstance<T>(args);
        RegisterComponent(component);

        return component;
    }

    /// <summary>
    /// Adds a component of the specified type to this GameObject.
    /// </summary>
    /// <param name="componentType">The type of component to add.</param>
    /// <param name="args">Optional constructor arguments for the component.</param>
    /// <returns>The newly added component instance.</returns>
    public Component AddComponent(Type componentType, params object[] args)
    {
        ValidateComponentType(componentType);

        var component = CreateComponentInstance(componentType, args);
        RegisterComponent(component);

        return component;
    }

    private T CreateComponentInstance<T>(params object[] args) where T : Component
    {
        var constructorArgs = GetConstructorArguments(args);
        var instance = CreateInstance(typeof(T), constructorArgs);

        if (instance is not T component)
            throw new InvalidOperationException(
                $"Type {typeof(T).FullName} could not be instantiated. " +
                "Ensure it has a valid constructor."
            );

        return component;
    }

    private Component CreateComponentInstance(Type componentType, params object[] args)
    {
        var constructorArgs = GetConstructorArguments(args);
        var instance = CreateInstance(componentType, constructorArgs);

        if (instance is not Component component)
            throw new InvalidOperationException(
                $"Type {componentType.FullName} could not be instantiated as a Component."
            );

        return component;
    }

    private object[] GetConstructorArguments(params object[] args)
    {
        return args is { Length: > 0 } ? args : Array.Empty<object>();
    }

    private void ValidateComponentType(Type componentType)
    {
        if (!typeof(Component).IsAssignableFrom(componentType))
            if (componentType != null)
                throw new ArgumentException(
                    $"Type {componentType.FullName} does not inherit from Component.",
                    nameof(componentType)
                );
    }

    private void RegisterComponent(Component component)
    {
        component.Initialize(this);
        _components.Add(component);
    }

    #endregion

    #region Component Management - Remove

    /// <summary>
    /// Removes the specified component from this GameObject.
    /// </summary>
    /// <typeparam name="T">The type of component to remove.</typeparam>
    /// <param name="component">The component instance to remove.</param>
    public void RemoveComponent<T>(T component) where T : Component
    {
        ValidateComponentNotNull(component);

        if (_components.Remove(component))
            component.Destroy();
        else
            throw new InvalidOperationException(
                $"Component of type {typeof(T).FullName} not found on this GameObject."
            );
    }

    private void ValidateComponentNotNull<T>(T component) where T : Component
    {
        if (component == null)
            throw new ArgumentNullException(
                nameof(component),
                "Cannot remove a null component."
            );
    }

    #endregion

    #region Component Management - Query

    /// <summary>
    /// Gets the first component of the specified type attached to this GameObject.
    /// </summary>
    /// <typeparam name="T">The type of component to find.</typeparam>
    /// <returns>The component if found; otherwise, null.</returns>
    public T GetComponent<T>() where T : Component
    {
        return (T)_components.Find(c => c is T);
    }

    /// <summary>
    /// Gets all components of the specified type attached to this GameObject.
    /// </summary>
    /// <typeparam name="T">The type of components to find.</typeparam>
    /// <returns>A list of all matching components.</returns>
    public List<T> GetComponents<T>() where T : Component
    {
        return _components.OfType<T>().ToList();
    }

    /// <summary>
    /// Gets the first component of the specified type in this GameObject or any of its children.
    /// </summary>
    /// <typeparam name="T">The type of component to find.</typeparam>
    /// <returns>The component if found; otherwise, null.</returns>
    public T GetComponentInChildren<T>() where T : Component
    {
        T foundComponent = null;

        TraverseHierarchyUntilFound(this, ref foundComponent);

        return foundComponent;
    }

    private void TraverseHierarchyUntilFound<T>(GameObject gameObject, ref T foundComponent) where T : Component
    {
        if (foundComponent != null)
            return;

        foundComponent = gameObject.GetComponent<T>();

        if (foundComponent != null)
            return;

        foreach (var childTransform in gameObject.Transform.Children)
        {
            TraverseHierarchyUntilFound(childTransform.GameObject, ref foundComponent);

            if (foundComponent != null)
                return;
        }
    }

    /// <summary>
    /// Gets all components of the specified type in this GameObject and all its children.
    /// </summary>
    /// <typeparam name="T">The type of components to find.</typeparam>
    /// <returns>A list of all matching components in the hierarchy.</returns>
    public List<T> GetComponentsInChildren<T>() where T : Component
    {
        var uniqueComponents = new HashSet<T>();
        CollectComponentsInHierarchy(this, uniqueComponents);

        return uniqueComponents.ToList();
    }

    private void CollectComponentsInHierarchy<T>(GameObject gameObject, HashSet<T> components) where T : Component
    {
        var component = gameObject.GetComponent<T>();

        if (component != null) components.Add(component);

        foreach (var childTransform in gameObject.Transform.Children)
            CollectComponentsInHierarchy(childTransform.GameObject, components);
    }

    /// <summary>
    /// Gets all components attached to this GameObject.
    /// </summary>
    /// <returns>A list of all components.</returns>
    public List<Component> GetAllComponents()
    {
        return _components;
    }

    #endregion
}