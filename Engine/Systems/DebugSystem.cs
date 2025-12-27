using Engine.Animation;
using Engine.Components;
using Engine.Core;
using Engine.Rendering;
using OpenTK.Mathematics;

namespace Engine.Systems;

/// <summary>
/// System responsible for rendering debug visualizations in the scene.
/// Displays physics shapes and skeletal rig bones when debug view is enabled.
/// </summary>
internal sealed class DebugSystem : ISystem
{
    #region Constants

    private const float BONE_SPHERE_RADIUS = 0.1f;
    private const float DEBUG_SHAPE_ALPHA = 1;
    private const int VERTICES_PER_TRIANGLE = 3;

    #endregion

    #region Fields

    private SceneSystem _sceneSystem;

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the debug system and subscribes to engine events.
    /// </summary>
    public void Initialize()
    {
        EngineWindow.EngineInitialized += OnEngineInitialized;
    }

    private void OnEngineInitialized()
    {
        InitializeSystems();
        SubscribeToRenderEvents();
    }

    private void InitializeSystems()
    {
        _sceneSystem = SystemManager.GetSystem<SceneSystem>();
    }

    private void SubscribeToRenderEvents()
    {
        var renderSystem = SystemManager.GetSystem<RenderSystem>();
        renderSystem.DebugRenderPass += OnDebugRenderPass;
    }

    #endregion

    #region Debug Rendering

    private void OnDebugRenderPass()
    {
        RenderDebugVisualization();
    }

    private void RenderDebugVisualization()
    {
        ConfigureDebugCamera();
        RenderPhysicsShapes();
        RenderSkeletalRigs();
        FinalizeDebugRender();
    }

    private void ConfigureDebugCamera()
    {
        if (_sceneSystem == null)
            return;

        var currentScene = _sceneSystem.CurrentScene;
        DebugRenderer.SetCamera(
            currentScene.CameraViewMatrix,
            currentScene.CameraProjectionMatrix
        );
    }

    private void FinalizeDebugRender()
    {
        DebugRenderer.Render();
        DebugRenderer.Clear();
    }

    #endregion

    #region Physics Debug Rendering

    private void RenderPhysicsShapes()
    {
        if (_sceneSystem == null)
            return;

        var rigidbodies = _sceneSystem.CurrentScene.GetComponents<Rigidbody>();

        foreach (var rigidbody in rigidbodies) RenderRigidbodyShape(rigidbody);
    }

    private void RenderRigidbodyShape(Rigidbody rigidbody)
    {
        var debugTriangles = rigidbody.PhysicsBody.GetDebugShape();

        if (debugTriangles == null || debugTriangles.Count == 0)
            return;

        var shapeColor = CreateDebugShapeColor();
        RenderTriangleMesh(debugTriangles, shapeColor);
    }

    private Color4 CreateDebugShapeColor()
    {
        var color = new Color4(0, 1, 0, 1);
        color.A = DEBUG_SHAPE_ALPHA;
        return color;
    }

    private void RenderTriangleMesh(List<Vector3> triangles, Color4 color)
    {
        for (var i = 0; i < triangles.Count; i += VERTICES_PER_TRIANGLE)
        {
            var vertex1 = triangles[i];
            var vertex2 = triangles[i + 1];
            var vertex3 = triangles[i + 2];

            DrawDebugTriangle(vertex1, vertex2, vertex3, color);
        }
    }

    #endregion

    #region Skeletal Debug Rendering

    private void RenderSkeletalRigs()
    {
        if (_sceneSystem == null)
            return;

        var skinnedMeshRenderers = _sceneSystem.CurrentScene.GetComponents<SkinnedMeshRenderer>();

        foreach (var skinnedMesh in skinnedMeshRenderers) RenderSkinnedMeshBones(skinnedMesh);
    }

    private void RenderSkinnedMeshBones(SkinnedMeshRenderer skinnedMesh)
    {
        foreach (var bone in skinnedMesh.Rig) RenderBoneVisualization(bone);
    }

    private void RenderBoneVisualization(Bone bone)
    {
        var bonePosition = bone.Transform.WorldPosition;
        DrawDebugWireSphere(bonePosition, BONE_SPHERE_RADIUS, Color4.Yellow);
    }

    #endregion

    #region Debug Drawing API

    /// <summary>
    /// Draws a wireframe sphere for debug visualization.
    /// </summary>
    /// <param name="position">The center position of the sphere.</param>
    /// <param name="radius">The radius of the sphere.</param>
    /// <param name="color">The color of the wireframe.</param>
    public void DrawDebugWireSphere(Vector3 position, float radius, Color4 color)
    {
        DebugRenderer.DrawWireSphere(position, radius, color);
    }

    /// <summary>
    /// Draws a filled triangle for debug visualization.
    /// </summary>
    /// <param name="vertex1">The first vertex of the triangle.</param>
    /// <param name="vertex2">The second vertex of the triangle.</param>
    /// <param name="vertex3">The third vertex of the triangle.</param>
    /// <param name="color">The color of the triangle.</param>
    public void DrawDebugTriangle(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3, Color4 color)
    {
        DebugRenderer.DrawTriangle(vertex1, vertex2, vertex3, color);
    }

    #endregion
}