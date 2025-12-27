using Engine.Components;
using Engine.Core;
using Engine.Rendering;
using Engine.Systems;
using OpenTK.Mathematics;

namespace Project.Assets.Scripts.ToonScene;

/// <summary>
/// Handles real-time planar reflections using an offscreen framebuffer and a mirrored camera.
/// </summary>
public sealed class PlanarReflection : IDisposable
{
    public float PlaneHeight { get; set; }

    private FrameBuffer _reflectionBuffer;
    private Camera _reflectionCamera;
    private readonly HashSet<Renderer> _reflectionObjects = new();

    private RenderSystem _renderSystem;
    private SceneSystem _sceneSystem;
    private bool RenderReflection { get; set; } = true;
    private bool _isInitialized;

    #region Initialization

    private void EnsureInitialized()
    {
        if (_isInitialized)
            return;

        _isInitialized = true;

        _renderSystem = SystemManager.GetSystem<RenderSystem>();
        _sceneSystem = SystemManager.GetSystem<SceneSystem>();

        var viewportSize = Screen.GetViewportSize();
        _reflectionBuffer = new FrameBuffer(viewportSize.X, viewportSize.Y);

        CreateReflectionCamera();
        InitializeReflectionObjects();
    }

    private void CreateReflectionCamera()
    {
        var mainCamera = _sceneSystem.CurrentScene.Camera;

        var reflectionCamObject = new GameObject
        {
            Name = "ReflectionCamera"
        };

        _reflectionCamera = reflectionCamObject.AddComponent<Camera>();
        _reflectionCamera.Projection = Camera.ProjectionType.Perspective;
        _reflectionCamera.FieldOfView = mainCamera.FieldOfView;
    }

    private void InitializeReflectionObjects()
    {
        _reflectionObjects.Clear();

        foreach (var obj in _sceneSystem.CurrentScene.ActiveSceneObjects)
        {
            if (!IsReflectiveObject(obj))
                continue;

            var renderer = obj.GetComponent<Renderer>();
            if (renderer == null)
                continue;

            _reflectionObjects.Add(renderer);
            obj.GameObjectDestroyed += OnRefObjectDestroyed;
        }
    }

    private void OnRefObjectDestroyed(GameObject obj)
    {
        _reflectionObjects.Remove(obj.GetComponent<Renderer>());
        obj.GameObjectDestroyed -= OnRefObjectDestroyed;
    }

    /// <summary>
    /// Determines whether an object should be excluded from reflection rendering.
    /// </summary>
    private static bool IsReflectiveObject(GameObject obj)
    {
        // You can later add tags/layers instead of relying on names.
        var name = obj.Name.ToLowerInvariant();
        return name is "planarplane" or "terrain" or "rocks" or "prop1";
    }

    #endregion

    #region Rendering

    public int GetReflectionTextureId()
    {
        return _reflectionBuffer?.ColorTexture ?? -1;
    }

    public void Render()
    {
        EnsureInitialized();
        if (!RenderReflection)
            return;

        var mainCamera = _sceneSystem.CurrentScene.Camera;

        DisableReflectionObjects();

        SetupReflectionCamera(mainCamera);

        try
        {
            _reflectionBuffer.Bind();
            _renderSystem.RenderOpaqueObjects(_reflectionCamera);
        }
        finally
        {
            _reflectionBuffer.Unbind();
            EnableReflectionObjects();
        }
    }

    private void SetupReflectionCamera(Camera mainCamera)
    {
        var camPos = mainCamera.Transform.WorldPosition;
        var camRot = mainCamera.Transform.WorldRotation;

        var distanceToPlane = camPos.Y - PlaneHeight;
        var reflectedPos = new Vector3(camPos.X, PlaneHeight - distanceToPlane, camPos.Z);
        var reflectedRot = ReflectQuaternionY(camRot);

        _reflectionCamera.Transform.WorldPosition = reflectedPos;
        _reflectionCamera.Transform.WorldRotation = reflectedRot;

        _reflectionCamera.FieldOfView = mainCamera.FieldOfView;
        _reflectionCamera.AspectRatio = mainCamera.AspectRatio;
    }

    private static Quaternion ReflectQuaternionY(Quaternion q)
    {
        return new Quaternion(-q.X, q.Y, -q.Z, q.W);
    }

    private void DisableReflectionObjects()
    {
        foreach (var obj in _reflectionObjects)
            obj.Enabled = false;
    }

    private void EnableReflectionObjects()
    {
        foreach (var obj in _reflectionObjects)
            obj.Enabled = true;
    }

    #endregion

    #region Cleanup

    public void Dispose()
    {
        _reflectionBuffer?.Dispose();
        _reflectionObjects.Clear();
        _isInitialized = false;
    }

    #endregion
}