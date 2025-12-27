using Adapters;
using Assimp;
using Engine.Rendering;
using Engine.Shaders;
using OpenTK.Mathematics;
using Bone = Engine.Animation.Bone;
using Material = Engine.Rendering.Material;
using Mesh = Engine.Rendering.Mesh;

namespace Engine.Components;

/// <summary>
/// Renderer component for meshes with skeletal animation support.
/// Manages bone transformations and uploads bone matrices to shaders for skinned mesh rendering.
/// </summary>
public sealed class SkinnedMeshRenderer : Renderer
{
    #region Constants

    private const int MAX_BONE_COUNT = 100;

    #endregion

    #region Properties

    public override int VertexCount => Mesh?.VertexCount ?? 0;
    public override string MeshName => Mesh?.ModelName ?? string.Empty;
    public override Material Material { get; set; }
    public override Mesh Mesh { get; set; }
    internal List<Bone> Rig => _rig;
    public Node RootNode => Mesh.GetSceneData().RootNode;

    #endregion

    #region Fields

    private readonly List<Bone> _rig = new();
    private readonly Matrix4[] _boneMatrices = new Matrix4[MAX_BONE_COUNT];
    private bool _isDisposed;

    #endregion

    #region Constructors

    public SkinnedMeshRenderer()
    {
    }

    public SkinnedMeshRenderer(Material material, string modelName, int modelMeshIndex)
    {
        Mesh = new Mesh(modelName, modelMeshIndex);
        Material = material;
    }

    #endregion

    #region Rig Management

    private void CheckRig()
    {
        if (_rig.Count != 0) return;

        foreach (var bone in Transform.Parent.GameObject.GetComponentsInChildren<Bone>())
        {
            if (bone == null) return;

            if (!_rig.Contains(bone)) _rig.Add(bone);
        }
    }

    #endregion

    #region Bone Transformation

    private void UpdateBoneMatrices()
    {
        if (!HasValidRig())
            return;

        ResetBoneMatricesToIdentity();
        CalculateBoneTransformations();
    }

    private bool HasValidRig()
    {
        return _rig.Count > 0;
    }

    private void ResetBoneMatricesToIdentity()
    {
        for (var i = 0; i < MAX_BONE_COUNT; i++) _boneMatrices[i] = Matrix4.Identity;
    }

    private void CalculateBoneTransformations()
    {
        if (Mesh?.VertexBuilder?.BoneIndexToName == null)
            return;

        foreach (var boneMapping in Mesh.VertexBuilder.BoneIndexToName)
        {
            var boneIndex = boneMapping.Key;
            var boneName = boneMapping.Value;

            UpdateBoneMatrix(boneIndex, boneName);
        }
    }

    private void UpdateBoneMatrix(int boneIndex, string boneName)
    {
        var bone = FindBoneByName(boneName);

        if (bone == null)
            return;

        var finalBoneTransform = CalculateFinalBoneTransform(bone, boneIndex);
        _boneMatrices[boneIndex] = finalBoneTransform;
    }

    private Bone FindBoneByName(string boneName)
    {
        return _rig.Find(b => b.BoneName == boneName);
    }

    private Matrix4 CalculateFinalBoneTransform(Bone bone, int boneIndex)
    {
        var boneWorldTransform = bone.Transform.GetWorldTransformMatrix();

        var rendererWorldTransform = Transform.GetWorldTransformMatrix();
        var inverseRendererTransform = Matrix4.Invert(rendererWorldTransform);

        var boneInRendererLocalSpace = boneWorldTransform * inverseRendererTransform;

        var transposedTransform = Matrix4.Transpose(boneInRendererLocalSpace);

        var numericsTransform = transposedTransform.ToNumerics();
        var boneOffset = Mesh.GetBoneOffset(boneIndex);

        var finalTransform = numericsTransform * boneOffset;

        return EngineExtensions.ToOpenTK(finalTransform);
    }

    #endregion

    #region Rendering

    public override void Render(Camera targetCamera)
    {
        if (Mesh == null || Material == null) return;

        CheckRig();

        UpdateBoneMatrices();

        Material.BindShader();
        Material.Shader.SetBool("useBones", true);
        Material.UpdateProperties(targetCamera, Transform.GetWorldTransformMatrix());

        UploadBoneMatricesToShader();

        Mesh?.Render();
    }

    private void UploadBoneMatricesToShader()
    {
        Material.Shader.UploadBoneMatrices(_boneMatrices);
    }

    #endregion

    #region Shadow Rendering

    public override void RenderShadow(ShaderBase shadowShader)
    {
        if (Mesh == null) return;

        CheckRig();

        UpdateBoneMatrices();

        shadowShader.SetBool("useBones", true);
        shadowShader.UploadBoneMatrices(_boneMatrices);

        Mesh.Render();
    }

    #endregion

    #region Cleanup

    public override void Destroy()
    {
        base.Destroy();
        Dispose();
    }

    private void Dispose()
    {
        if (_isDisposed)
            return;

        DisposeResources();
        _isDisposed = true;
    }

    private void DisposeResources()
    {
        Mesh?.Dispose();
        Material?.Dispose();
    }

    #endregion
}