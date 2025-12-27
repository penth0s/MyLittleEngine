using Engine.Rendering;
using Engine.Shaders;

namespace Engine.Components;

/// <summary>
/// Responsible for rendering a single mesh with an assigned material.
/// Handles both regular and shadow rendering passes.
/// </summary>
public sealed class MeshRenderer : Renderer, IDisposable
{
    #region Properties

    public override int VertexCount => Mesh?.VertexCount ?? 0;
    public override string MeshName => Mesh?.ModelName ?? string.Empty;

    public override Material Material { get; set; }
    public override Mesh Mesh { get; set; }

    #endregion

    #region Constructors

    public MeshRenderer()
    {
    }

    public MeshRenderer(Material material, string modelName, int modelMeshIndex)
    {
        Mesh = new Mesh(modelName, modelMeshIndex);
        Material = material;
    }

    #endregion

    #region Rendering

    public override void Render(Camera camera)
    {
        if (Mesh == null || Material == null)
            return;

        Material.BindShader();

        var passCount = Material.Shader.PassCount;
        var worldMatrix = Transform.GetWorldTransformMatrix();

        for (var pass = 0; pass < passCount; pass++)
        {
            Material.UpdateProperties(camera, worldMatrix, pass);
            Mesh.Render();
        }
    }

    public override void RenderShadow(ShaderBase shadowShader)
    {
        if (Mesh == null)
            return;

        var hasBones = Mesh.IsSkinned;
        shadowShader.SetBool("useBones", hasBones);

        Mesh.Render();
    }

    #endregion

    #region Cleanup

    public override void Destroy()
    {
        base.Destroy();
        Dispose();
    }

    public void Dispose()
    {
        Mesh?.Dispose();
        Mesh = null;

        Material?.Dispose();
        Material = null;
    }

    #endregion
}