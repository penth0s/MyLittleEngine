using Engine.Database.Implementations;
using Engine.Shaders.Implementations;
using Engine.Systems;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine.Rendering;

/// <summary>
/// Provides debug visualization rendering capabilities for 3D geometry.
/// Supports drawing lines, triangles, wire shapes, and coordinate axes.
/// </summary>
internal static class DebugRenderer
{
    #region Constants

    private const int DEFAULT_SPHERE_SEGMENTS = 16;
    private const int CUBE_CORNER_COUNT = 8;
    private const int VERTICES_PER_LINE = 2;
    private const int VERTICES_PER_TRIANGLE = 3;
    private const int COMPONENTS_PER_VERTEX = 3;

    private const float DEFAULT_ALPHA = 0.5f;
    private const float AXIS_ALPHA = 0.7f;

    private static readonly Vector4 DEFAULT_COLOR = new(1, 1, 1, DEFAULT_ALPHA);

    #endregion

    #region Fields - OpenGL Resources

    private static uint _vertexArrayObject;
    private static uint _vertexBufferObject;

    // İki ayrı shader program
    private static int _lineShaderProgram; // Lines için (geometry shader yok)
    private static int _wireframeShaderProgram; // Triangles için (geometry shader var)

    // Line shader uniforms
    private static int _lineViewProjectionLocation;
    private static int _lineColorLocation;

    // Wireframe shader uniforms
    private static int _wireframeViewProjectionLocation;
    private static int _wireframeColorLocation;

    #endregion

    #region Fields - Rendering State

    private static bool _isInitialized;
    private static Matrix4 _currentViewProjectionMatrix;

    #endregion

    #region Fields - Geometry Data

    private static readonly List<Vector3> _lineVertices = new();
    private static readonly List<Vector3> _triangleVertices = new();
    private static readonly List<Vector4> _lineColors = new();
    private static readonly List<Vector4> _triangleColors = new();

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the debug renderer by creating OpenGL resources and loading shaders.
    /// </summary>
    public static void Initialize()
    {
        if (_isInitialized)
            return;

        LoadDebugShaders();
        CreateOpenGLResources();
        ConfigureVertexAttributes();
        CacheUniformLocations();

        _isInitialized = true;
    }

    private static void LoadDebugShaders()
    {
        var shaderDatabase = SystemManager.GetSystem<DatabaseSystem>()
            .GetDatabase<ShaderDataBase>();

        _lineShaderProgram = shaderDatabase.Get<DebugLineShader>().GetShaderHandle;
        _wireframeShaderProgram = shaderDatabase.Get<DebugWireFrameShader>().GetShaderHandle;
    }

    private static void CreateOpenGLResources()
    {
        _vertexArrayObject = (uint)GL.GenVertexArray();
        _vertexBufferObject = (uint)GL.GenBuffer();

        GL.BindVertexArray(_vertexArrayObject);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
    }

    private static void ConfigureVertexAttributes()
    {
        // Position attribute (location = 0)
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(
            0,
            COMPONENTS_PER_VERTEX,
            VertexAttribPointerType.Float,
            false,
            COMPONENTS_PER_VERTEX * sizeof(float),
            0
        );

        GL.BindVertexArray(0);
    }

    private static void CacheUniformLocations()
    {
        // Line shader uniforms
        _lineViewProjectionLocation = GL.GetUniformLocation(_lineShaderProgram, "viewProjection");
        _lineColorLocation = GL.GetUniformLocation(_lineShaderProgram, "color");

        // Wireframe shader uniforms
        _wireframeViewProjectionLocation = GL.GetUniformLocation(_wireframeShaderProgram, "viewProjection");
        _wireframeColorLocation = GL.GetUniformLocation(_wireframeShaderProgram, "color");
    }

    #endregion

    #region Camera Setup

    /// <summary>
    /// Sets the view and projection matrices for rendering debug geometry.
    /// </summary>
    /// <param name="viewMatrix">The camera view matrix.</param>
    /// <param name="projectionMatrix">The camera projection matrix.</param>
    public static void SetCamera(Matrix4 viewMatrix, Matrix4 projectionMatrix)
    {
        _currentViewProjectionMatrix = viewMatrix * projectionMatrix;
    }

    #endregion

    #region Line Drawing

    /// <summary>
    /// Draws a line between two points with the specified color.
    /// </summary>
    /// <param name="start">The starting point of the line.</param>
    /// <param name="end">The ending point of the line.</param>
    /// <param name="color">The color of the line.</param>
    public static void DrawLine(Vector3 start, Vector3 end, Color4 color)
    {
        DrawLine(start, end, ColorToVector4(color));
    }

    private static void DrawLine(Vector3 start, Vector3 end, Vector4 color = default)
    {
        ValidateInitialized();

        var lineColor = color == default ? DEFAULT_COLOR : color;

        _lineVertices.Add(start);
        _lineVertices.Add(end);
        _lineColors.Add(lineColor);
        _lineColors.Add(lineColor);
    }

    #endregion

    #region Triangle Drawing

    /// <summary>
    /// Draws a filled triangle with the specified vertices and color.
    /// </summary>
    /// <param name="vertex1">The first vertex of the triangle.</param>
    /// <param name="vertex2">The second vertex of the triangle.</param>
    /// <param name="vertex3">The third vertex of the triangle.</param>
    /// <param name="color">The color of the triangle.</param>
    public static void DrawTriangle(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3, Color4 color)
    {
        DrawTriangle(vertex1, vertex2, vertex3, ColorToVector4(color));
    }

    private static void DrawTriangle(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3, Vector4 color = default)
    {
        ValidateInitialized();

        var triangleColor = color == default ? DEFAULT_COLOR : color;

        _triangleVertices.Add(vertex1);
        _triangleVertices.Add(vertex2);
        _triangleVertices.Add(vertex3);
        _triangleColors.Add(triangleColor);
        _triangleColors.Add(triangleColor);
        _triangleColors.Add(triangleColor);
    }

    #endregion

    #region Shape Drawing - Wire Cube

    /// <summary>
    /// Draws a wireframe cube at the specified position with the given size.
    /// </summary>
    /// <param name="center">The center position of the cube.</param>
    /// <param name="size">The size of the cube along each axis.</param>
    /// <param name="color">The color of the wireframe. Uses default if not specified.</param>
    public static void DrawWireCube(Vector3 center, Vector3 size, Vector4 color = default)
    {
        var cubeColor = color == default ? DEFAULT_COLOR : color;
        var corners = CalculateCubeCorners(center, size);

        DrawCubeFaces(corners, cubeColor);
        DrawCubeEdges(corners, cubeColor);
    }

    private static Vector3[] CalculateCubeCorners(Vector3 center, Vector3 size)
    {
        var halfSize = size * 0.5f;

        return new Vector3[CUBE_CORNER_COUNT]
        {
            center + new Vector3(-halfSize.X, -halfSize.Y, -halfSize.Z), // Bottom-front-left
            center + new Vector3(halfSize.X, -halfSize.Y, -halfSize.Z), // Bottom-front-right
            center + new Vector3(halfSize.X, halfSize.Y, -halfSize.Z), // Top-front-right
            center + new Vector3(-halfSize.X, halfSize.Y, -halfSize.Z), // Top-front-left
            center + new Vector3(-halfSize.X, -halfSize.Y, halfSize.Z), // Bottom-back-left
            center + new Vector3(halfSize.X, -halfSize.Y, halfSize.Z), // Bottom-back-right
            center + new Vector3(halfSize.X, halfSize.Y, halfSize.Z), // Top-back-right
            center + new Vector3(-halfSize.X, halfSize.Y, halfSize.Z) // Top-back-left
        };
    }

    private static void DrawCubeFaces(Vector3[] corners, Vector4 color)
    {
        // Bottom face
        DrawLine(corners[0], corners[1], color);
        DrawLine(corners[1], corners[2], color);
        DrawLine(corners[2], corners[3], color);
        DrawLine(corners[3], corners[0], color);

        // Top face
        DrawLine(corners[4], corners[5], color);
        DrawLine(corners[5], corners[6], color);
        DrawLine(corners[6], corners[7], color);
        DrawLine(corners[7], corners[4], color);
    }

    private static void DrawCubeEdges(Vector3[] corners, Vector4 color)
    {
        // Vertical edges
        DrawLine(corners[0], corners[4], color);
        DrawLine(corners[1], corners[5], color);
        DrawLine(corners[2], corners[6], color);
        DrawLine(corners[3], corners[7], color);
    }

    #endregion

    #region Shape Drawing - Wire Sphere

    /// <summary>
    /// Draws a wireframe sphere at the specified position with the given radius.
    /// </summary>
    /// <param name="center">The center position of the sphere.</param>
    /// <param name="radius">The radius of the sphere.</param>
    /// <param name="color">The color of the wireframe. Uses default if not specified.</param>
    public static void DrawWireSphere(Vector3 center, float radius, Color4 color)
    {
        DrawWireSphere(center, radius, ColorToVector4(color), DEFAULT_SPHERE_SEGMENTS);
    }

    private static void DrawWireSphere(Vector3 center, float radius, Vector4 color = default,
        int segments = DEFAULT_SPHERE_SEGMENTS)
    {
        var sphereColor = color == default ? DEFAULT_COLOR : color;

        // Draw three perpendicular circles
        DrawSphereCircle(center, radius, Vector3.UnitX, Vector3.UnitY, sphereColor, segments); // XY plane
        DrawSphereCircle(center, radius, Vector3.UnitY, Vector3.UnitZ, sphereColor, segments); // YZ plane
        DrawSphereCircle(center, radius, Vector3.UnitZ, Vector3.UnitX, sphereColor, segments); // ZX plane
    }

    private static void DrawSphereCircle(
        Vector3 center,
        float radius,
        Vector3 axis1,
        Vector3 axis2,
        Vector4 color,
        int segments)
    {
        var angleStep = MathHelper.TwoPi / segments;

        for (var i = 0; i < segments; i++)
        {
            var angle1 = i * angleStep;
            var angle2 = (i + 1) * angleStep;

            var point1 = center + axis1 * (radius * MathF.Cos(angle1))
                                + axis2 * (radius * MathF.Sin(angle1));

            var point2 = center + axis1 * (radius * MathF.Cos(angle2))
                                + axis2 * (radius * MathF.Sin(angle2));

            DrawLine(point1, point2, color);
        }
    }

    #endregion

    #region Shape Drawing - Axis

    /// <summary>
    /// Draws the coordinate axes (X, Y, Z) at the specified origin.
    /// X axis is red, Y axis is green, Z axis is blue.
    /// </summary>
    /// <param name="origin">The origin point where the axes intersect.</param>
    /// <param name="length">The length of each axis line.</param>
    public static void DrawAxis(Vector3 origin, float length = 1.0f)
    {
        DrawLine(origin, origin + Vector3.UnitX * length, new Vector4(1, 0, 0, AXIS_ALPHA)); // Red X
        DrawLine(origin, origin + Vector3.UnitY * length, new Vector4(0, 1, 0, AXIS_ALPHA)); // Green Y
        DrawLine(origin, origin + Vector3.UnitZ * length, new Vector4(0, 0, 1, AXIS_ALPHA)); // Blue Z
    }

    #endregion

    #region Rendering

    /// <summary>
    /// Renders all queued debug geometry and clears the queue.
    /// </summary>
    public static void Render()
    {
        if (!_isInitialized || !HasGeometryToRender())
            return;

        SaveAndConfigureOpenGLState();
        GL.BindVertexArray(_vertexArrayObject);

        RenderLines();
        RenderTriangles();

        GL.BindVertexArray(0);
        RestoreOpenGLState();
    }

    private static bool HasGeometryToRender()
    {
        return _lineVertices.Count > 0 || _triangleVertices.Count > 0;
    }

    private static void RenderLines()
    {
        if (_lineVertices.Count == 0)
            return;

        var depthTestWasEnabled = GL.IsEnabled(EnableCap.DepthTest);
        GL.Disable(EnableCap.DepthTest);

        GL.UseProgram(_lineShaderProgram);
        GL.UniformMatrix4(_lineViewProjectionLocation, false, ref _currentViewProjectionMatrix);

        RenderPrimitives(_lineVertices, _lineColors, PrimitiveType.Lines, _lineColorLocation);

        GL.UseProgram(0);

        if (depthTestWasEnabled)
            GL.Enable(EnableCap.DepthTest);
    }

    private static void RenderTriangles()
    {
        if (_triangleVertices.Count == 0)
            return;

        GL.UseProgram(_wireframeShaderProgram);
        GL.UniformMatrix4(_wireframeViewProjectionLocation, false, ref _currentViewProjectionMatrix);

        RenderPrimitives(_triangleVertices, _triangleColors, PrimitiveType.Triangles, _wireframeColorLocation);

        GL.UseProgram(0);
    }

    #endregion

    #region OpenGL State Management

    private static bool _blendWasEnabled;
    private static bool _depthTestWasEnabled;
    private static bool _depthWriteWasEnabled;
    private static int _previousSrcRgb;
    private static int _previousDstRgb;
    private static int _previousSrcAlpha;
    private static int _previousDstAlpha;

    private static void SaveAndConfigureOpenGLState()
    {
        // Save blend state
        _blendWasEnabled = GL.IsEnabled(EnableCap.Blend);

        // Disable blending for opaque debug lines
        if (_blendWasEnabled) GL.Disable(EnableCap.Blend);

        // Save blend function (for restoration)
        GL.GetInteger(GetPName.BlendSrcRgb, out _previousSrcRgb);
        GL.GetInteger(GetPName.BlendDstRgb, out _previousDstRgb);
        GL.GetInteger(GetPName.BlendSrcAlpha, out _previousSrcAlpha);
        GL.GetInteger(GetPName.BlendDstAlpha, out _previousDstAlpha);

        _depthTestWasEnabled = GL.IsEnabled(EnableCap.DepthTest);
        GL.Enable(EnableCap.DepthTest);

        GL.GetBoolean(GetPName.DepthWritemask, out _depthWriteWasEnabled);
        GL.DepthMask(true);
    }

    private static void RestoreOpenGLState()
    {
        // Restore depth write
        GL.DepthMask(_depthWriteWasEnabled);

        // Restore depth test
        if (_depthTestWasEnabled)
            GL.Enable(EnableCap.DepthTest);
        else
            GL.Disable(EnableCap.DepthTest);

        // Restore blend function
        GL.BlendFuncSeparate(
            (BlendingFactorSrc)_previousSrcRgb,
            (BlendingFactorDest)_previousDstRgb,
            (BlendingFactorSrc)_previousSrcAlpha,
            (BlendingFactorDest)_previousDstAlpha
        );

        // Restore blend state
        if (!_blendWasEnabled)
            GL.Disable(EnableCap.Blend);
    }

    #endregion

    #region Primitive Rendering

    private static void RenderPrimitives(List<Vector3> vertices, List<Vector4> colors, PrimitiveType primitiveType,
        int colorUniformLocation)
    {
        var verticesPerPrimitive = primitiveType == PrimitiveType.Lines
            ? VERTICES_PER_LINE
            : VERTICES_PER_TRIANGLE;

        for (var i = 0; i < vertices.Count; i += verticesPerPrimitive)
        {
            var color = colors[i];
            var vertexData = ExtractVertexData(vertices, i, verticesPerPrimitive);

            UploadVertexData(vertexData);
            SetUniformColor(color, colorUniformLocation);
            DrawPrimitive(primitiveType, verticesPerPrimitive);
        }
    }

    private static float[] ExtractVertexData(List<Vector3> vertices, int startIndex, int count)
    {
        var vertexData = new float[count * COMPONENTS_PER_VERTEX];

        for (var i = 0; i < count; i++)
        {
            var vertex = vertices[startIndex + i];
            var offset = i * COMPONENTS_PER_VERTEX;

            vertexData[offset] = vertex.X;
            vertexData[offset + 1] = vertex.Y;
            vertexData[offset + 2] = vertex.Z;
        }

        return vertexData;
    }

    private static void UploadVertexData(float[] vertexData)
    {
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
        GL.BufferData(
            BufferTarget.ArrayBuffer,
            vertexData.Length * sizeof(float),
            vertexData,
            BufferUsageHint.DynamicDraw
        );
    }

    private static void SetUniformColor(Vector4 color, int colorUniformLocation)
    {
        GL.Uniform4(colorUniformLocation, color);
    }

    private static void DrawPrimitive(PrimitiveType primitiveType, int vertexCount)
    {
        GL.DrawArrays(primitiveType, 0, vertexCount);
    }

    #endregion

    #region Queue Management

    /// <summary>
    /// Clears all queued debug geometry.
    /// </summary>
    public static void Clear()
    {
        _lineVertices.Clear();
        _triangleVertices.Clear();
        _lineColors.Clear();
        _triangleColors.Clear();
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Releases all OpenGL resources used by the debug renderer.
    /// </summary>
    public static void Cleanup()
    {
        if (!_isInitialized)
            return;

        GL.DeleteVertexArray(_vertexArrayObject);
        GL.DeleteBuffer(_vertexBufferObject);
        GL.DeleteProgram(_lineShaderProgram);
        GL.DeleteProgram(_wireframeShaderProgram);

        _isInitialized = false;
    }

    #endregion

    #region Helper Methods

    private static void ValidateInitialized()
    {
        if (!_isInitialized)
            throw new InvalidOperationException(
                "DebugRenderer must be initialized before use. Call Initialize() first."
            );
    }

    private static Vector4 ColorToVector4(Color4 color)
    {
        return new Vector4(color.R, color.G, color.B, color.A);
    }

    #endregion
}