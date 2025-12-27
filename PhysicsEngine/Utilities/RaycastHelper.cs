using System.Reflection;
using Jitter2;
using Jitter2.Collision;
using Jitter2.Dynamics;
using Jitter2.LinearMath;

namespace PhysicsEngine.Utilities;

/// <summary>
/// Helper class for performing raycasts in the physics world
/// </summary>
public static class RaycastHelper
{
    #region Constants

    private static readonly string[] RIGIDBODY_PROPERTY_CANDIDATES =
    {
        "RigidBody",
        "Body",
        "Owner",
        "Parent",
        "UserData",
        "Tag"
    };

    private static readonly string[] NESTED_RIGIDBODY_PROPERTIES =
    {
        "RigidBody",
        "Body"
    };

    #endregion

    #region Fields

    private static World _world;
    private static bool _isInitialized;
    private static readonly Dictionary<Type, PropertyInfo> _propertyCache = new();
    private static readonly object _cacheLock = new();

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the raycast helper with a physics world
    /// </summary>
    /// <param name="world">The Jitter2 physics world</param>
    public static void Initialize(World world)
    {
        if (world == null) throw new ArgumentNullException(nameof(world), "World cannot be null");

        _world = world;
        _isInitialized = true;
        ClearPropertyCache();
    }

    /// <summary>
    /// Checks if the helper is initialized
    /// </summary>
    public static bool IsInitialized => _isInitialized;

    #endregion

    #region Raycast

    /// <summary>
    /// Performs a raycast from origin to direction
    /// </summary>
    /// <param name="origin">Starting point of the ray</param>
    /// <param name="direction">Direction vector of the ray</param>
    /// <returns>The hit RigidBody, or null if nothing was hit</returns>
    public static RigidBody Cast(JVector origin, JVector direction)
    {
        ValidateInitialization();

        if (!PerformRaycast(origin, direction, out var proxy)) return null;

        return ExtractRigidBody(proxy);
    }

    private static bool PerformRaycast(JVector origin, JVector direction, out IDynamicTreeProxy proxy)
    {
        var hit = _world.DynamicTree.RayCast(
            origin,
            direction,
            null,
            null,
            out proxy,
            out _,
            out _
        );

        return hit && proxy != null;
    }

    #endregion

    #region RigidBody Extraction

    private static RigidBody ExtractRigidBody(IDynamicTreeProxy proxy)
    {
        if (proxy == null) return null;

        var proxyType = proxy.GetType();

        // Try direct property access
        var rigidBody = TryGetDirectRigidBody(proxy, proxyType);
        if (rigidBody != null) return rigidBody;

        // Try nested property access
        return TryGetNestedRigidBody(proxy, proxyType);
    }

    private static RigidBody TryGetDirectRigidBody(IDynamicTreeProxy proxy, Type proxyType)
    {
        foreach (var propertyName in RIGIDBODY_PROPERTY_CANDIDATES)
        {
            var rigidBody = GetRigidBodyFromProperty(proxy, proxyType, propertyName);
            if (rigidBody != null) return rigidBody;
        }

        return null;
    }

    private static RigidBody TryGetNestedRigidBody(IDynamicTreeProxy proxy, Type proxyType)
    {
        foreach (var propertyName in RIGIDBODY_PROPERTY_CANDIDATES)
        {
            var rigidBody = GetNestedRigidBody(proxy, proxyType, propertyName);
            if (rigidBody != null) return rigidBody;
        }

        return null;
    }

    private static RigidBody GetRigidBodyFromProperty(object obj, Type type, string propertyName)
    {
        var property = GetCachedProperty(type, propertyName);
        if (property == null) return null;

        try
        {
            var value = property.GetValue(obj);
            return value as RigidBody;
        }
        catch
        {
            return null;
        }
    }

    private static RigidBody GetNestedRigidBody(object obj, Type type, string propertyName)
    {
        var property = GetCachedProperty(type, propertyName);
        if (property == null) return null;

        try
        {
            var value = property.GetValue(obj);
            if (value == null) return null;

            // Try to find RigidBody in nested object
            foreach (var nestedPropertyName in NESTED_RIGIDBODY_PROPERTIES)
            {
                var rigidBody = GetRigidBodyFromProperty(value, value.GetType(), nestedPropertyName);
                if (rigidBody != null) return rigidBody;
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    #endregion

    #region Property Cache

    private static PropertyInfo GetCachedProperty(Type type, string propertyName)
    {
        lock (_cacheLock)
        {
            if (_propertyCache.TryGetValue(type, out var cachedProperty)) return cachedProperty;
        }

        var property = type.GetProperty(propertyName);

        if (property != null)
            lock (_cacheLock)
            {
                _propertyCache[type] = property;
            }

        return property;
    }

    private static void ClearPropertyCache()
    {
        lock (_cacheLock)
        {
            _propertyCache.Clear();
        }
    }

    #endregion

    #region Validation

    private static void ValidateInitialization()
    {
        if (!_isInitialized || _world == null)
            throw new InvalidOperationException(
                "RaycastHelper not initialized. Call Initialize(World) first."
            );
    }

    #endregion

    #region Public Utility Methods

    /// <summary>
    /// Performs a raycast and returns hit information
    /// </summary>
    public static bool CastWithInfo(
        JVector origin,
        JVector direction,
        out RigidBody hitBody,
        out JVector hitNormal,
        out float hitFraction)
    {
        ValidateInitialization();

        var hit = _world.DynamicTree.RayCast(
            origin,
            direction,
            null,
            null,
            out var proxy,
            out hitNormal,
            out hitFraction
        );

        if (hit && proxy != null)
        {
            hitBody = ExtractRigidBody(proxy);
            return hitBody != null;
        }

        hitBody = null;
        return false;
    }

    /// <summary>
    /// Checks if a ray hits anything
    /// </summary>
    public static bool HasHit(JVector origin, JVector direction)
    {
        ValidateInitialization();

        return _world.DynamicTree.RayCast(
            origin,
            direction,
            null,
            null,
            out var proxy,
            out _,
            out _
        ) && proxy != null;
    }

    /// <summary>
    /// Resets the helper (clears cache and world reference)
    /// </summary>
    public static void Reset()
    {
        _world = null;
        _isInitialized = false;
        ClearPropertyCache();
    }

    #endregion
}