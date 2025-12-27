using Adapters;
using Engine.Core;
using Engine.Systems;
using Newtonsoft.Json;
using OpenTK.Mathematics;

namespace Engine.Components;

/// <summary>
/// Component that adds physics simulation to a GameObject.
/// Provides properties for controlling physics behavior such as velocity, friction, and static/dynamic state.
/// </summary>
public sealed class Rigidbody : Component, IPhysicsBodyListener
{
    #region Properties

    /// <summary>
    /// Gets the unique identifier for this physics body.
    /// </summary>
    public Guid Id => GameObject.ID;

    /// <summary>
    /// Gets or sets whether this rigidbody is static (immovable by physics).
    /// </summary>
    public bool IsStatic { get; set; } = true;

    /// <summary>
    /// Gets or sets the world-space position of this rigidbody.
    /// </summary>
    [JsonIgnore]
    public Vector3 Position
    {
        get => PhysicsBody?.Position ?? Vector3.Zero;
        set
        {
            if (PhysicsBody != null)
                PhysicsBody.Position = value;
        }
    }

    /// <summary>
    /// Gets or sets the linear velocity of this rigidbody.
    /// </summary>
    [JsonIgnore]
    public Vector3 Velocity
    {
        get => PhysicsBody?.Velocity ?? Vector3.Zero;
        set
        {
            if (PhysicsBody != null)
                PhysicsBody.Velocity = value;
        }
    }

    /// <summary>
    /// Gets or sets the angular velocity of this rigidbody.
    /// </summary>
    [JsonIgnore]
    public Vector3 AngularVelocity
    {
        get => PhysicsBody?.AngularVelocity ?? Vector3.Zero;
        set
        {
            if (PhysicsBody != null)
                PhysicsBody.AngularVelocity = value;
        }
    }

    /// <summary>
    /// Gets or sets the friction coefficient for this rigidbody.
    /// </summary>
    [JsonIgnore]
    public float Friction
    {
        get => PhysicsBody?.Friction ?? 0f;
        set
        {
            if (PhysicsBody != null)
                PhysicsBody.Friction = value;
        }
    }

    #endregion

    #region Fields

    /// <summary>
    /// The underlying physics body implementation.
    /// </summary>
    [JsonIgnore]
    public IPhysicsBody PhysicsBody { get; private set; }

    private EngineInfoProviderSystem _engineInfoProviderSystem;
    private bool _hasColliderBeenSet;

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the rigidbody component and registers it with the physics system.
    /// </summary>
    internal override void Initialize(GameObject owner)
    {
        base.Initialize(owner);

        InitializeEngineInfoProvider();
        RegisterWithPhysicsSystem();
    }

    private void InitializeEngineInfoProvider()
    {
        if (_engineInfoProviderSystem == null)
            _engineInfoProviderSystem = SystemManager.GetSystem<EngineInfoProviderSystem>();
    }

    private void RegisterWithPhysicsSystem()
    {
        _engineInfoProviderSystem.OnRigidBodyCreated(this);
    }

    /// <summary>
    /// Sets the physics body implementation for this rigidbody.
    /// </summary>
    /// <param name="physicsBody">The physics body to use.</param>
    public void SetPhysicsBody(IPhysicsBody physicsBody)
    {
        ValidatePhysicsBody(physicsBody);

        PhysicsBody = physicsBody;
        ConfigurePhysicsBody();
        SubscribeToPhysicsUpdates();
    }

    private void ValidatePhysicsBody(IPhysicsBody physicsBody)
    {
        if (physicsBody == null)
            throw new ArgumentNullException(
                nameof(physicsBody),
                "Physics body cannot be null."
            );
    }

    private void ConfigurePhysicsBody()
    {
        PhysicsBody.Id = GameObject.ID;
    }

    private void SubscribeToPhysicsUpdates()
    {
        PhysicsBody.BodyUpdated += OnPhysicsBodyUpdated;
    }

    #endregion

    #region Collider Setup

    private void SetupColliderIfNeeded()
    {
        if (_hasColliderBeenSet)
            return;

        var meshRenderer = GameObject.GetComponent<MeshRenderer>();

        if (meshRenderer != null) TryCreateColliderFromMesh(meshRenderer);
    }

    private void TryCreateColliderFromMesh(MeshRenderer meshRenderer)
    {
        var vertices = ExtractScaledVertices(meshRenderer);

        if (vertices.Count == 0)
            return;

        PhysicsBody.AddShape(vertices);
        _hasColliderBeenSet = true;
    }

    private List<Vector3> ExtractScaledVertices(MeshRenderer meshRenderer)
    {
        var localVertices = meshRenderer.Mesh.GetVertices();

        if (localVertices.Count == 0)
            return localVertices;

        var matrix = meshRenderer.GameObject.Transform.GetWorldTransformMatrix();
        return ApplyScaleToVertices(localVertices, matrix);
    }

    private List<Vector3> ApplyScaleToVertices(List<Vector3> vertices, Matrix4 transform)
    {
        var worldVertices = new List<Vector3>(vertices.Count);

        for (var i = 0; i < vertices.Count; i++)
        {
            var localVertex = new Vector4(vertices[i], 1.0f);
            var worldVertex = localVertex * transform;

            worldVertices.Add(worldVertex.Xyz);
        }

        return worldVertices;
    }

    #endregion

    #region Physics Synchronization

    private void OnPhysicsBodyUpdated(object sender, EventArgs e)
    {
        SetupColliderIfNeeded();
        SynchronizeWithPhysicsBody();
    }

    private void SynchronizeWithPhysicsBody()
    {
        UpdatePhysicsBodyStaticState();

        if (IsStatic)
            SyncStaticBodyToTransform();
        else
            SyncTransformToPhysicsBody();
    }

    private void UpdatePhysicsBodyStaticState()
    {
        PhysicsBody.IsStatic = IsStatic;
    }

    private void SyncStaticBodyToTransform()
    {
        PhysicsBody.Position = Transform.PivotPosition;
        PhysicsBody.Orientation = Transform.WorldRotation;
        Velocity = Vector3.Zero;
    }

    private void SyncTransformToPhysicsBody()
    {
        Transform.PivotPosition = PhysicsBody.Position;
        Transform.WorldRotation = PhysicsBody.Orientation;
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Cleans up the rigidbody and unregisters it from the physics system.
    /// </summary>
    public override void Destroy()
    {
        base.Destroy();
        UnregisterFromPhysicsSystem();
    }

    private void UnregisterFromPhysicsSystem()
    {
        if (PhysicsBody != null)
        {
            _engineInfoProviderSystem.OnRigidBodyDestroyed(PhysicsBody);
            PhysicsBody.BodyUpdated -= OnPhysicsBodyUpdated;
        }
    }

    #endregion
}