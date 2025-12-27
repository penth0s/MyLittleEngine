using Engine.Components;
using Engine.Core;
using Engine.Systems;
using OpenTK.Mathematics;

namespace Engine.Physics;

/// <summary>
/// Handles core physics operations such as raycasting and rigidbody lookups.
/// Integrates with the scene and engine information systems.
/// </summary>
public sealed class Physics : ISystem
{
    #region Fields

    private static SceneSystem _sceneSystem;
    private static EngineInfoProviderSystem _engineInfoProviderSystem;
    private static bool _isInitialized;

    #endregion

    #region Initialization

    public void Initialize()
    {
        _engineInfoProviderSystem = SystemManager.GetSystem<EngineInfoProviderSystem>();
        _engineInfoProviderSystem.EngineInitialized += OnEngineInitialized;
    }

    private static void OnEngineInitialized(object sender, EventArgs e)
    {
        _sceneSystem = SystemManager.GetSystem<SceneSystem>();
        _engineInfoProviderSystem = SystemManager.GetSystem<EngineInfoProviderSystem>();
        _isInitialized = true;
    }

    private static bool IsReady => _isInitialized && _sceneSystem != null && _engineInfoProviderSystem != null;

    #endregion

    #region Raycasting

    /// <summary>
    /// Casts a ray into the current scene and returns the first hit rigidbody, if any.
    /// </summary>
    /// <param name="origin">The ray's world-space origin.</param>
    /// <param name="direction">The normalized direction vector.</param>
    /// <returns>The hit Rigidbody component, or null if nothing was hit.</returns>
    internal static Rigidbody RayCast(Vector3 origin, Vector3 direction)
    {
        if (!IsReady)
        {
            Console.WriteLine("[Physics] RayCast called before Physics system was initialized.");
            return null;
        }

        var hitId = _engineInfoProviderSystem!.OnRaycast(origin, direction);

        if (_sceneSystem!.CurrentScene == null)
            return null;

        foreach (var sceneObject in _sceneSystem.CurrentScene.ActiveSceneObjects)
            if (sceneObject.ID == hitId)
                return sceneObject.GetComponent<Rigidbody>();

        return null;
    }

    #endregion
}