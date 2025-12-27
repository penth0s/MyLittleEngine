using System.Runtime.InteropServices;
using OpenTK.Mathematics;

namespace Engine.Rendering;

#region Vertex Structures

/// <summary>
/// Standard vertex structure with position, normal, texture coordinates, and color
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct Vertex
{
    public Vector3 Position;
    public Vector3 Normal;
    public Vector2 TexCoords;
    public Vector4 Color;

    public Vertex(Vector3 position, Vector3 normal, Vector2 texCoords, Vector4 color)
    {
        Position = position;
        Normal = normal;
        TexCoords = texCoords;
        Color = color;
    }
}

/// <summary>
/// Skinned vertex structure with bone indices and weights for skeletal animation
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct SkinnedVertex
{
    public Vector3 Position;
    public Vector3 Normal;
    public Vector2 TexCoord;
    public Vector4i BoneIndices;
    public Vector4 BoneWeights;

    public SkinnedVertex(Vector3 position, Vector3 normal, Vector2 texCoord)
    {
        Position = position;
        Normal = normal;
        TexCoord = texCoord;
        BoneIndices = Vector4i.Zero;
        BoneWeights = Vector4.Zero;
    }
}

#endregion

#region Skinned Vertex Builder

/// <summary>
/// Builder class for constructing skinned vertices with proper bone weight normalization
/// </summary>
public class SkinnedVertexBuilder
{
    #region Constants

    private const int MAX_BONES_PER_VERTEX = 4;
    private const float WEIGHT_EPSILON = 1e-6f;

    #endregion

    #region Properties

    public Dictionary<int, string> BoneIndexToName { get; }

    #endregion

    #region Fields

    private readonly List<SkinnedVertex> _vertices;
    private readonly Dictionary<int, List<BoneWeight>> _vertexWeights;

    #endregion

    #region Constructor

    public SkinnedVertexBuilder(int vertexCount)
    {
        if (vertexCount < 0) throw new ArgumentException("Vertex count cannot be negative", nameof(vertexCount));

        _vertices = new List<SkinnedVertex>(vertexCount);
        _vertexWeights = new Dictionary<int, List<BoneWeight>>();
        BoneIndexToName = new Dictionary<int, string>();
    }

    #endregion

    #region Public Methods

    public void AddVertex(Vector3 position, Vector3 normal, Vector2 texCoord)
    {
        _vertices.Add(new SkinnedVertex(position, normal, texCoord));
    }

    public void AddVertexWeight(int vertexIndex, int boneIndex, string name, float weight)
    {
        ValidateVertexIndex(vertexIndex);
        ValidateWeight(weight);

        if (!_vertexWeights.ContainsKey(vertexIndex)) _vertexWeights[vertexIndex] = new List<BoneWeight>();

        _vertexWeights[vertexIndex].Add(new BoneWeight(boneIndex, name, weight));
    }

    public SkinnedVertex[] BuildVertices()
    {
        var result = _vertices.ToArray();

        foreach (var kvp in _vertexWeights)
        {
            var vertexIndex = kvp.Key;
            var weights = kvp.Value;

            ProcessVertexWeights(result, vertexIndex, weights);
        }

        return result;
    }

    #endregion

    #region Private Methods - Validation

    private void ValidateVertexIndex(int vertexIndex)
    {
        if (vertexIndex < 0 || vertexIndex >= _vertices.Count)
            throw new ArgumentOutOfRangeException(
                nameof(vertexIndex),
                $"Vertex index {vertexIndex} is out of range [0, {_vertices.Count - 1}]"
            );
    }

    private void ValidateWeight(float weight)
    {
        if (weight < 0f) throw new ArgumentException("Bone weight cannot be negative", nameof(weight));
    }

    #endregion

    #region Private Methods - Weight Processing

    private void ProcessVertexWeights(SkinnedVertex[] result, int vertexIndex, List<BoneWeight> weights)
    {
        // Sort by weight (highest first) and take top 4
        var topWeights = GetTopWeights(weights);

        // Normalize weights to sum to 1.0
        var normalizedWeights = NormalizeWeights(topWeights);

        // Assign to vertex
        AssignWeightsToVertex(ref result[vertexIndex], normalizedWeights);
    }

    private List<BoneWeight> GetTopWeights(List<BoneWeight> weights)
    {
        return weights
            .OrderByDescending(w => w.Weight)
            .Take(MAX_BONES_PER_VERTEX)
            .ToList();
    }

    private List<BoneWeight> NormalizeWeights(List<BoneWeight> weights)
    {
        var totalWeight = weights.Sum(w => w.Weight);

        if (totalWeight < WEIGHT_EPSILON) return weights;

        var normalized = new List<BoneWeight>();
        foreach (var weight in weights)
        {
            var normalizedWeight = weight.Weight / totalWeight;
            normalizedWeight = Math.Clamp(normalizedWeight, 0.0f, 1.0f);
            normalized.Add(new BoneWeight(weight.BoneIndex, weight.Name, normalizedWeight));
        }

        return normalized;
    }

    private void AssignWeightsToVertex(ref SkinnedVertex vertex, List<BoneWeight> weights)
    {
        for (var i = 0; i < weights.Count && i < MAX_BONES_PER_VERTEX; i++)
        {
            var weight = weights[i];
            BoneIndexToName[weight.BoneIndex] = weight.Name;

            switch (i)
            {
                case 0:
                    vertex.BoneIndices.X = weight.BoneIndex;
                    vertex.BoneWeights.X = weight.Weight;
                    break;
                case 1:
                    vertex.BoneIndices.Y = weight.BoneIndex;
                    vertex.BoneWeights.Y = weight.Weight;
                    break;
                case 2:
                    vertex.BoneIndices.Z = weight.BoneIndex;
                    vertex.BoneWeights.Z = weight.Weight;
                    break;
                case 3:
                    vertex.BoneIndices.W = weight.BoneIndex;
                    vertex.BoneWeights.W = weight.Weight;
                    break;
            }
        }
    }

    #endregion

    #region Helper Struct

    private struct BoneWeight
    {
        public int BoneIndex { get; }
        public string Name { get; }
        public float Weight { get; }

        public BoneWeight(int boneIndex, string name, float weight)
        {
            BoneIndex = boneIndex;
            Name = name;
            Weight = weight;
        }
    }

    #endregion
}

#endregion