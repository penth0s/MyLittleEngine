using Adapters;
using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;
using OpenTK.Mathematics;

namespace PhysicsEngine.Utilities;

/// <summary>
/// Helper methods for physics shape creation and manipulation
/// </summary>
public static class ShapeHelper
{
    #region Constants

    private const float DEFAULT_MIN_THICKNESS = 0.1f;

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a rigid body shape from a list of vertices
    /// </summary>
    /// <param name="vertices">List of vertices in OpenTK format</param>
    /// <returns>A point cloud shape with centered mass</returns>
    public static RigidBodyShape GetShape(List<Vector3> vertices)
    {
        if (vertices == null || vertices.Count == 0)
            throw new ArgumentException("Vertices list cannot be null or empty", nameof(vertices));

        var jitterVertices = ConvertToJitterVertices(vertices);
        var sampledVertices = SampleAndEnsureThickness(jitterVertices);

        return CreateCenteredPointCloudShape(sampledVertices);
    }

    #endregion

    #region Private Methods - Conversion

    private static JVector[] ConvertToJitterVertices(List<Vector3> vertices)
    {
        var jitterVertices = new JVector[vertices.Count];

        for (var i = 0; i < vertices.Count; i++) jitterVertices[i] = vertices[i].ToJitter();

        return jitterVertices;
    }

    #endregion

    #region Private Methods - Shape Creation

    private static List<JVector> SampleAndEnsureThickness(JVector[] jitterVertices)
    {
        var sampledVertices = Jitter2.Collision.Shapes.ShapeHelper.SampleHull(jitterVertices, 7);
        return EnsureMinimumThickness(sampledVertices, DEFAULT_MIN_THICKNESS);
    }

    private static RigidBodyShape CreateCenteredPointCloudShape(List<JVector> vertices)
    {
        var pointCloudShape = new PointCloudShape(vertices);

        pointCloudShape.GetCenter(out var centerOfMass);
        pointCloudShape.Shift = -centerOfMass;

        return pointCloudShape;
    }

    #endregion

    #region Private Methods - Thickness Adjustment

    /// <summary>
    /// Ensures the shape has minimum thickness in all dimensions
    /// by extruding vertices if necessary
    /// </summary>
    private static List<JVector> EnsureMinimumThickness(List<JVector> vertices, float minThickness)
    {
        var bounds = CalculateBounds(vertices);
        var adjustedVertices = new List<JVector>(vertices);

        ExtrudeIfNeeded(adjustedVertices, vertices, bounds, minThickness);

        return adjustedVertices;
    }

    private static (float minX, float minY, float minZ, float maxX, float maxY, float maxZ) CalculateBounds(
        List<JVector> vertices)
    {
        float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;

        foreach (var vertex in vertices)
        {
            minX = Math.Min(minX, vertex.X);
            minY = Math.Min(minY, vertex.Y);
            minZ = Math.Min(minZ, vertex.Z);

            maxX = Math.Max(maxX, vertex.X);
            maxY = Math.Max(maxY, vertex.Y);
            maxZ = Math.Max(maxZ, vertex.Z);
        }

        return (minX, minY, minZ, maxX, maxY, maxZ);
    }

    private static void ExtrudeIfNeeded(
        List<JVector> adjustedVertices,
        List<JVector> originalVertices,
        (float minX, float minY, float minZ, float maxX, float maxY, float maxZ) bounds,
        float minThickness)
    {
        var sizeX = bounds.maxX - bounds.minX;
        var sizeY = bounds.maxY - bounds.minY;
        var sizeZ = bounds.maxZ - bounds.minZ;

        if (sizeX < minThickness) ExtrudeAlongAxis(adjustedVertices, originalVertices, minThickness, Axis.X);

        if (sizeY < minThickness) ExtrudeAlongAxis(adjustedVertices, originalVertices, minThickness, Axis.Y);

        if (sizeZ < minThickness) ExtrudeAlongAxis(adjustedVertices, originalVertices, minThickness, Axis.Z);
    }

    private static void ExtrudeAlongAxis(
        List<JVector> adjustedVertices,
        List<JVector> originalVertices,
        float minThickness,
        Axis axis)
    {
        var halfThickness = minThickness * 0.5f;

        switch (axis)
        {
            case Axis.X:
                adjustedVertices.AddRange(
                    originalVertices.Select(v => new JVector(v.X - halfThickness, v.Y, v.Z)));
                adjustedVertices.AddRange(
                    originalVertices.Select(v => new JVector(v.X + halfThickness, v.Y, v.Z)));
                break;

            case Axis.Y:
                adjustedVertices.AddRange(
                    originalVertices.Select(v => new JVector(v.X, v.Y - halfThickness, v.Z)));
                adjustedVertices.AddRange(
                    originalVertices.Select(v => new JVector(v.X, v.Y + halfThickness, v.Z)));
                break;

            case Axis.Z:
                adjustedVertices.AddRange(
                    originalVertices.Select(v => new JVector(v.X, v.Y, v.Z - halfThickness)));
                adjustedVertices.AddRange(
                    originalVertices.Select(v => new JVector(v.X, v.Y, v.Z + halfThickness)));
                break;
        }
    }

    #endregion

    #region Helper Enum

    private enum Axis
    {
        X,
        Y,
        Z
    }

    #endregion
}