using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Engine.Core;
using Engine.Database.Implementations;
using Engine.Systems;
using Newtonsoft.Json;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using PrimitiveType = OpenTK.Graphics.OpenGL4.PrimitiveType;

namespace Engine.Rendering;

/// <summary>
/// Represents a 3D mesh with vertex data, indices, and rendering capabilities.
/// Supports both regular and skinned (skeletal animation) meshes, as well as runtime-generated meshes.
/// </summary>
public sealed class Mesh : IDisposable
{
    #region Constants

    private const int POSITION_ATTRIBUTE_LOCATION = 0;
    private const int NORMAL_ATTRIBUTE_LOCATION = 1;
    private const int TEXCOORD_ATTRIBUTE_LOCATION = 2;
    private const int COLOR_ATTRIBUTE_LOCATION = 3;
    private const int BONE_INDICES_ATTRIBUTE_LOCATION = 3;
    private const int BONE_WEIGHTS_ATTRIBUTE_LOCATION = 4;
    private const int MAX_BONE_INFLUENCES = 4;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the number of vertices in this mesh.
    /// </summary>
    [JsonIgnore]
    public int VertexCount => IsSkinned ? _skinnedVertices?.Length ?? 0 : _vertices?.Length ?? 0;

    /// <summary>
    /// Gets the number of indices in this mesh.
    /// </summary>
    [JsonIgnore]
    public int IndexCount => _indices?.Length ?? 0;

    /// <summary>
    /// Gets the name of the model this mesh belongs to.
    /// </summary>
    [JsonIgnore]
    public string ModelName => _modelName;

    /// <summary>
    /// Gets whether this mesh has skeletal animation data.
    /// </summary>
    [JsonIgnore]
    public bool IsSkinned => _meshData?.HasBones ?? false;

    /// <summary>
    /// Gets the vertex builder used for skinned mesh construction.
    /// </summary>
    [JsonIgnore]
    public SkinnedVertexBuilder VertexBuilder => _vertexBuilder;

    #endregion

    #region Fields - OpenGL Handles

    private int _vertexBufferObject;
    private int _vertexArrayObject;
    private int _elementBufferObject;

    #endregion

    #region Fields - Vertex Data

    private Vertex[] _vertices;
    private SkinnedVertex[] _skinnedVertices;
    private int[] _indices;

    #endregion

    #region Fields - Mesh Data

    private Assimp.Mesh _meshData;
    private SkinnedVertexBuilder _vertexBuilder;

    [JsonProperty] private string _modelName = string.Empty;

    [JsonProperty] private int _modelMeshIndex;

    #endregion

    #region Fields - State

    private bool _isDisposed;
    private bool _isInitialized;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the Mesh class for runtime generation.
    /// </summary>
    public Mesh()
    {
        _isInitialized = false;
    }

    /// <summary>
    /// Initializes a new instance of the Mesh class from a model database entry.
    /// </summary>
    /// <param name="modelName">The name of the model.</param>
    /// <param name="modelMeshIndex">The index of this mesh within the model.</param>
    public Mesh(string modelName, int modelMeshIndex)
    {
        _modelName = modelName;
        _modelMeshIndex = modelMeshIndex;

        LoadMeshDataFromDatabase();
        InitializeMeshData();
    }

    private void LoadMeshDataFromDatabase()
    {
        var modelDatabase = GetModelDatabase();
        _meshData = modelDatabase.GetMeshFromSaveData(_modelName, _modelMeshIndex);
    }

    private void InitializeMeshData()
    {
        if (_meshData.HasBones)
            InitializeSkinnedMesh();
        else
            InitializeStaticMesh();

        _isInitialized = true;
    }

    #endregion

    #region Serialization

    /// <summary>
    /// Called after deserialization to reload mesh data from the database.
    /// </summary>
    [OnDeserialized]
    internal void OnDeserialized(StreamingContext context)
    {
        if (!string.IsNullOrEmpty(_modelName))
        {
            LoadMeshDataFromDatabase();
            InitializeMeshData();
        }
    }

    #endregion

    #region Mesh Data Retrieval

    /// <summary>
    /// Gets the scene data for the model containing this mesh.
    /// </summary>
    /// <returns>The Assimp scene data.</returns>
    public Assimp.Scene GetSceneData()
    {
        var modelDatabase = GetModelDatabase();
        return modelDatabase.GetModel(_modelName);
    }

    /// <summary>
    /// Gets all vertex positions in this mesh.
    /// </summary>
    /// <returns>A list of vertex positions.</returns>
    public List<Vector3> GetVertices()
    {
        if (IsSkinned)
            return _skinnedVertices.Select(v => v.Position).ToList();
        return _vertices.Select(v => v.Position).ToList();
    }

    /// <summary>
    /// Gets the offset matrix for a bone at the specified index.
    /// </summary>
    /// <param name="boneIndex">The index of the bone.</param>
    /// <returns>The bone offset matrix, or identity if not found.</returns>
    public System.Numerics.Matrix4x4 GetBoneOffset(int boneIndex)
    {
        if (!IsSkinned || !IsBoneIndexValid(boneIndex)) return System.Numerics.Matrix4x4.Identity;

        return GetScaledBoneOffsetMatrix(boneIndex);
    }

    private bool IsBoneIndexValid(int boneIndex)
    {
        return boneIndex >= 0 && boneIndex < _meshData.BoneCount;
    }

    private System.Numerics.Matrix4x4 GetScaledBoneOffsetMatrix(int boneIndex)
    {
        var bone = _meshData.Bones[boneIndex];
        var matrix = bone.OffsetMatrix;

        ApplyScaleFactorToTranslation(ref matrix);

        return matrix;
    }

    private void ApplyScaleFactorToTranslation(ref System.Numerics.Matrix4x4 matrix)
    {
        matrix.M14 *= EngineWindow.ScaleFactor;
        matrix.M24 *= EngineWindow.ScaleFactor;
        matrix.M34 *= EngineWindow.ScaleFactor;
    }

    #endregion

    #region Skinned Mesh Initialization

    private void InitializeSkinnedMesh()
    {
        CreateVertexBuilder();
        AddVerticesFromMeshData();
        ProcessBoneWeights();
        BuildSkinnedVertices();
        ExtractIndices();
        CreateSkinnedVertexBuffer();
    }

    private void CreateVertexBuilder()
    {
        _vertexBuilder = new SkinnedVertexBuilder(_meshData.VertexCount);
    }

    private void AddVerticesFromMeshData()
    {
        for (var i = 0; i < _meshData.VertexCount; i++)
        {
            var vertexData = ExtractVertexData(i);
            _vertexBuilder.AddVertex(
                vertexData.Position * EngineWindow.ScaleFactor,
                vertexData.Normal,
                vertexData.TexCoord
            );
        }
    }

    private void ProcessBoneWeights()
    {
        for (var boneIndex = 0; boneIndex < _meshData.BoneCount; boneIndex++) AddBoneWeightsToBuilder(boneIndex);
    }

    private void AddBoneWeightsToBuilder(int boneIndex)
    {
        var bone = _meshData.Bones[boneIndex];

        foreach (var weight in bone.VertexWeights)
            _vertexBuilder.AddVertexWeight(
                weight.VertexID,
                boneIndex,
                bone.Name,
                weight.Weight
            );
    }

    private void BuildSkinnedVertices()
    {
        _skinnedVertices = _vertexBuilder.BuildVertices();
    }

    private void CreateSkinnedVertexBuffer()
    {
        CreateVertexArrayObject();
        CreateSkinnedVertexBufferObject();
        CreateElementBufferObject();
        ConfigureSkinnedVertexAttributes();
        UnbindBuffers();
    }

    private void CreateSkinnedVertexBufferObject()
    {
        _vertexBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);

        var bufferSize = _skinnedVertices.Length * Marshal.SizeOf(typeof(SkinnedVertex));
        GL.BufferData(
            BufferTarget.ArrayBuffer,
            bufferSize,
            _skinnedVertices,
            BufferUsageHint.StaticDraw
        );
    }

    private void ConfigureSkinnedVertexAttributes()
    {
        var stride = Marshal.SizeOf(typeof(SkinnedVertex));
        var offset = 0;

        ConfigurePositionAttribute(stride, offset);
        offset += Vector3.SizeInBytes;

        ConfigureNormalAttribute(stride, offset);
        offset += Vector3.SizeInBytes;

        ConfigureTexCoordAttribute(stride, offset);
        offset += Vector2.SizeInBytes;

        ConfigureBoneIndicesAttribute(stride, offset);
        offset += MAX_BONE_INFLUENCES * sizeof(int);

        ConfigureBoneWeightsAttribute(stride, offset);
    }

    private void ConfigureBoneIndicesAttribute(int stride, int offset)
    {
        GL.VertexAttribIPointer(
            BONE_INDICES_ATTRIBUTE_LOCATION,
            MAX_BONE_INFLUENCES,
            VertexAttribIntegerType.Int,
            stride,
            offset
        );
        GL.EnableVertexAttribArray(BONE_INDICES_ATTRIBUTE_LOCATION);
    }

    private void ConfigureBoneWeightsAttribute(int stride, int offset)
    {
        GL.VertexAttribPointer(
            BONE_WEIGHTS_ATTRIBUTE_LOCATION,
            MAX_BONE_INFLUENCES,
            VertexAttribPointerType.Float,
            false,
            stride,
            offset
        );
        GL.EnableVertexAttribArray(BONE_WEIGHTS_ATTRIBUTE_LOCATION);
    }

    #endregion

    #region Static Mesh Initialization

    private void InitializeStaticMesh()
    {
        ExtractVertexData();
        ExtractIndices();
        CreateStaticVertexBuffer();
    }

    private void ExtractVertexData()
    {
        _vertices = new Vertex[_meshData.VertexCount];

        for (var i = 0; i < _meshData.VertexCount; i++) _vertices[i] = CreateVertex(i);
    }

    private Vertex CreateVertex(int vertexIndex)
    {
        var vertexData = ExtractVertexData(vertexIndex);
        var vertexColor = ExtractVertexColor(vertexIndex);

        return new Vertex(
            vertexData.Position * EngineWindow.ScaleFactor,
            vertexData.Normal,
            vertexData.TexCoord,
            vertexColor
        );
    }

    private (Vector3 Position, Vector3 Normal, Vector2 TexCoord) ExtractVertexData(int vertexIndex)
    {
        var position = ConvertToVector3(_meshData.Vertices[vertexIndex]);

        var normal = _meshData.HasNormals
            ? ConvertToVector3(_meshData.Normals[vertexIndex])
            : Vector3.Zero;

        var texCoord = _meshData.HasTextureCoords(0)
            ? new Vector2(
                _meshData.TextureCoordinateChannels[0][vertexIndex].X,
                _meshData.TextureCoordinateChannels[0][vertexIndex].Y
            )
            : Vector2.Zero;

        return (position, normal, texCoord);
    }

    private Vector4 ExtractVertexColor(int vertexIndex)
    {
        if (!HasVertexColors() || vertexIndex >= _meshData.VertexColorChannels[0].Count) return Vector4.One;

        var color = _meshData.VertexColorChannels[0][vertexIndex];
        return new Vector4(color.X, color.Y, color.Z, color.W);
    }

    private bool HasVertexColors()
    {
        return _meshData.VertexColorChannelCount > 0;
    }

    private Vector3 ConvertToVector3(System.Numerics.Vector3 vector)
    {
        return new Vector3(vector.X, vector.Y, vector.Z);
    }

    private void ExtractIndices()
    {
        _indices = _meshData.GetIndices().ToArray();
    }

    private void CreateStaticVertexBuffer()
    {
        CreateVertexArrayObject();
        CreateStaticVertexBufferObject();
        CreateElementBufferObject();
        ConfigureStaticVertexAttributes();
        UnbindBuffers();
    }

    private void CreateStaticVertexBufferObject()
    {
        _vertexBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);

        var bufferSize = _vertices.Length * Marshal.SizeOf(typeof(Vertex));
        GL.BufferData(
            BufferTarget.ArrayBuffer,
            bufferSize,
            _vertices,
            BufferUsageHint.DynamicDraw // Changed to DynamicDraw for runtime updates
        );
    }

    private void ConfigureStaticVertexAttributes()
    {
        var stride = Marshal.SizeOf(typeof(Vertex));
        var offset = 0;

        ConfigurePositionAttribute(stride, offset);
        offset += Vector3.SizeInBytes;

        ConfigureNormalAttribute(stride, offset);
        offset += Vector3.SizeInBytes;

        ConfigureTexCoordAttribute(stride, offset);
        offset += Vector2.SizeInBytes;

        ConfigureColorAttribute(stride, offset);
    }

    #endregion

    #region Vertex Attribute Configuration

    private void CreateVertexArrayObject()
    {
        _vertexArrayObject = GL.GenVertexArray();
        GL.BindVertexArray(_vertexArrayObject);
    }

    private void CreateElementBufferObject()
    {
        _elementBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBufferObject);
        GL.BufferData(
            BufferTarget.ElementArrayBuffer,
            _indices.Length * sizeof(int),
            _indices,
            BufferUsageHint.DynamicDraw // Changed to DynamicDraw for runtime updates
        );
    }

    private void ConfigurePositionAttribute(int stride, int offset)
    {
        GL.VertexAttribPointer(
            POSITION_ATTRIBUTE_LOCATION,
            3,
            VertexAttribPointerType.Float,
            false,
            stride,
            offset
        );
        GL.EnableVertexAttribArray(POSITION_ATTRIBUTE_LOCATION);
    }

    private void ConfigureNormalAttribute(int stride, int offset)
    {
        GL.VertexAttribPointer(
            NORMAL_ATTRIBUTE_LOCATION,
            3,
            VertexAttribPointerType.Float,
            false,
            stride,
            offset
        );
        GL.EnableVertexAttribArray(NORMAL_ATTRIBUTE_LOCATION);
    }

    private void ConfigureTexCoordAttribute(int stride, int offset)
    {
        GL.VertexAttribPointer(
            TEXCOORD_ATTRIBUTE_LOCATION,
            2,
            VertexAttribPointerType.Float,
            false,
            stride,
            offset
        );
        GL.EnableVertexAttribArray(TEXCOORD_ATTRIBUTE_LOCATION);
    }

    private void ConfigureColorAttribute(int stride, int offset)
    {
        GL.VertexAttribPointer(
            COLOR_ATTRIBUTE_LOCATION,
            4,
            VertexAttribPointerType.Float,
            false,
            stride,
            offset
        );
        GL.EnableVertexAttribArray(COLOR_ATTRIBUTE_LOCATION);
    }

    private void UnbindBuffers()
    {
        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
    }

    #endregion

    #region Rendering

    /// <summary>
    /// Renders this mesh using the currently bound shader.
    /// </summary>
    public void Render()
    {
        if (!_isInitialized)
            return;

        GL.BindVertexArray(_vertexArrayObject);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBufferObject);
        GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);
    }

    #endregion

    #region Bounds Calculation

    /// <summary>
    /// Calculates the world-space center point of this mesh.
    /// </summary>
    /// <param name="worldTransform">The world transformation matrix.</param>
    /// <returns>The center point in world space.</returns>
    public Vector3 GetWorldCenter(Matrix4 worldTransform)
    {
        var bounds = CalculateLocalBounds();
        var localCenter = (bounds.Min + bounds.Max) * 0.5f;

        return Vector3.TransformPosition(localCenter, worldTransform);
    }

    private (Vector3 Min, Vector3 Max) CalculateLocalBounds()
    {
        var positions = IsSkinned
            ? _skinnedVertices.Select(v => v.Position)
            : _vertices.Select(v => v.Position);

        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);

        foreach (var position in positions)
        {
            min = Vector3.ComponentMin(min, position);
            max = Vector3.ComponentMax(max, position);
        }

        return (min, max);
    }

    #endregion

    #region Helper Methods

    private ModelDataBase GetModelDatabase()
    {
        return SystemManager.GetSystem<DatabaseSystem>().GetDatabase<ModelDataBase>();
    }

    #endregion

    #region Disposal

    /// <summary>
    /// Releases OpenGL resources used by this mesh.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        DeleteOpenGLResources();

        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    private void DeleteOpenGLResources()
    {
        if (_vertexBufferObject != 0)
            GL.DeleteBuffer(_vertexBufferObject);

        if (_elementBufferObject != 0)
            GL.DeleteBuffer(_elementBufferObject);

        if (_vertexArrayObject != 0)
            GL.DeleteVertexArray(_vertexArrayObject);
    }

    #endregion
}