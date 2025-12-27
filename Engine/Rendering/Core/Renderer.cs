using Engine.Components;
using Engine.Shaders;
using OpenTK.Mathematics;

namespace Engine.Rendering;

/// <summary>
/// Base class for all renderable components in the engine.
/// Defines core rendering behavior and shared utility methods.
/// </summary>
public abstract class Renderer : Component
{
    #region Abstract Properties

    /// <summary>
    /// Total number of vertices used by the mesh.
    /// </summary>
    public abstract int VertexCount { get; }

    /// <summary>
    /// The display name or model name of the mesh.
    /// </summary>
    public abstract string MeshName { get; }

    /// <summary>
    /// The material assigned to this renderer.
    /// </summary>
    public abstract Material Material { get; set; }

    /// <summary>
    /// The mesh data this renderer is responsible for drawing.
    /// </summary>
    public abstract Mesh Mesh { get; set; }

    #endregion

    #region Abstract Methods

    /// <summary>
    /// Draws the object using the given camera as the render target.
    /// </summary>
    public abstract void Render(Camera targetCamera);

    /// <summary>
    /// Draws the object for shadow casting passes.
    /// </summary>
    public abstract void RenderShadow(ShaderBase shadowShader);

    #endregion

    #region Virtual Lifecycle

    /// <summary>
    /// Called when the renderer is destroyed or removed.
    /// </summary>
    public override void Destroy()
    {
    }

    #endregion

    #region Helper Methods

    public Vector3 GetWorldCenter()
    {
        return Mesh.GetWorldCenter(Transform.GetWorldTransformMatrix());
    }

    #endregion
}