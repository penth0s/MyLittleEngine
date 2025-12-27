using OpenTK.Graphics.OpenGL4;

namespace Engine.Rendering;

/// <summary>
/// Renders a full-screen quad for post-processing and screen-space effects.
/// Provides a simple interface for drawing a quad that covers the entire screen in NDC space.
/// </summary>
internal static class ScreenQuadRenderer
{
    #region Constants

    private const int POSITION_COMPONENT_COUNT = 3;
    private const int TEXCOORD_COMPONENT_COUNT = 2;
    private const int VERTEX_COMPONENT_COUNT = POSITION_COMPONENT_COUNT + TEXCOORD_COMPONENT_COUNT;
    private const int QUAD_VERTEX_COUNT = 6;

    private const int POSITION_ATTRIBUTE_LOCATION = 0;
    private const int TEXCOORD_ATTRIBUTE_LOCATION = 1;

    #endregion

    #region Fields

    private static int _vertexArrayObject;
    private static int _vertexBufferObject;
    private static bool _isInitialized;

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the screen quad renderer by creating vertex buffers and configuring vertex attributes.
    /// </summary>
    public static void Initialize()
    {
        if (_isInitialized)
            return;

        var quadVertices = CreateQuadVertexData();

        CreateOpenGLResources();
        UploadVertexData(quadVertices);
        ConfigureVertexAttributes();

        GL.BindVertexArray(0);

        _isInitialized = true;
    }

    private static float[] CreateQuadVertexData()
    {
        // Full-screen quad in NDC space (-1 to 1) with texture coordinates (0 to 1)
        // Two triangles forming a quad
        return
        [
            // Triangle 1
            // Position (x, y, z)  // TexCoord (u, v)
            -1f, -1f, 0f, 0f, 0f, // Bottom-left
            1f, -1f, 0f, 1f, 0f, // Bottom-right
            1f, 1f, 0f, 1f, 1f, // Top-right

            // Triangle 2
            -1f, -1f, 0f, 0f, 0f, // Bottom-left
            1f, 1f, 0f, 1f, 1f, // Top-right
            -1f, 1f, 0f, 0f, 1f // Top-left
        ];
    }

    private static void CreateOpenGLResources()
    {
        _vertexArrayObject = GL.GenVertexArray();
        _vertexBufferObject = GL.GenBuffer();

        GL.BindVertexArray(_vertexArrayObject);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
    }

    private static void UploadVertexData(float[] vertexData)
    {
        GL.BufferData(
            BufferTarget.ArrayBuffer,
            vertexData.Length * sizeof(float),
            vertexData,
            BufferUsageHint.StaticDraw
        );
    }

    private static void ConfigureVertexAttributes()
    {
        var stride = VERTEX_COMPONENT_COUNT * sizeof(float);

        ConfigurePositionAttribute(stride);
        ConfigureTexCoordAttribute(stride);
    }

    private static void ConfigurePositionAttribute(int stride)
    {
        GL.EnableVertexAttribArray(POSITION_ATTRIBUTE_LOCATION);
        GL.VertexAttribPointer(
            POSITION_ATTRIBUTE_LOCATION,
            POSITION_COMPONENT_COUNT,
            VertexAttribPointerType.Float,
            false,
            stride,
            0
        );
    }

    private static void ConfigureTexCoordAttribute(int stride)
    {
        var texCoordOffset = POSITION_COMPONENT_COUNT * sizeof(float);

        GL.EnableVertexAttribArray(TEXCOORD_ATTRIBUTE_LOCATION);
        GL.VertexAttribPointer(
            TEXCOORD_ATTRIBUTE_LOCATION,
            TEXCOORD_COMPONENT_COUNT,
            VertexAttribPointerType.Float,
            false,
            stride,
            texCoordOffset
        );
    }

    #endregion

    #region Rendering

    /// <summary>
    /// Draws the full-screen quad. Automatically initializes if not already initialized.
    /// </summary>
    public static void Draw()
    {
        EnsureInitialized();
        RenderQuad();
    }

    private static void EnsureInitialized()
    {
        if (!_isInitialized) Initialize();
    }

    private static void RenderQuad()
    {
        GL.BindVertexArray(_vertexArrayObject);
        GL.DrawArrays(PrimitiveType.Triangles, 0, QUAD_VERTEX_COUNT);
        GL.BindVertexArray(0);
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Releases OpenGL resources used by the screen quad renderer.
    /// </summary>
    public static void Cleanup()
    {
        if (!_isInitialized)
            return;

        GL.DeleteVertexArray(_vertexArrayObject);
        GL.DeleteBuffer(_vertexBufferObject);

        _vertexArrayObject = 0;
        _vertexBufferObject = 0;
        _isInitialized = false;
    }

    #endregion
}