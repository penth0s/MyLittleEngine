using OpenTK.Graphics.OpenGL4;

namespace Engine.Rendering;

/// <summary>
/// Manages skybox rendering with proper resource management and separation of concerns
/// </summary>
internal sealed class SkyBox : IDisposable
{
    #region Fields

    private int _skyboxVAO;
    private int _skyboxVBO;
    private bool _isDisposed;

    private const int VERTEX_COUNT = 36;
    private const int POSITION_ATTRIBUTE_INDEX = 0;
    private const int COMPONENTS_PER_VERTEX = 3;

    #endregion

    #region Vertex Data

    private static readonly float[] SkyboxVertices =
    {
        // Back face
        -1.0f, 1.0f, -1.0f,
        -1.0f, -1.0f, -1.0f,
        1.0f, -1.0f, -1.0f,
        1.0f, -1.0f, -1.0f,
        1.0f, 1.0f, -1.0f,
        -1.0f, 1.0f, -1.0f,

        // Left face
        -1.0f, -1.0f, 1.0f,
        -1.0f, -1.0f, -1.0f,
        -1.0f, 1.0f, -1.0f,
        -1.0f, 1.0f, -1.0f,
        -1.0f, 1.0f, 1.0f,
        -1.0f, -1.0f, 1.0f,

        // Right face
        1.0f, -1.0f, -1.0f,
        1.0f, -1.0f, 1.0f,
        1.0f, 1.0f, 1.0f,
        1.0f, 1.0f, 1.0f,
        1.0f, 1.0f, -1.0f,
        1.0f, -1.0f, -1.0f,

        // Front face
        -1.0f, -1.0f, 1.0f,
        -1.0f, 1.0f, 1.0f,
        1.0f, 1.0f, 1.0f,
        1.0f, 1.0f, 1.0f,
        1.0f, -1.0f, 1.0f,
        -1.0f, -1.0f, 1.0f,

        // Top face
        -1.0f, 1.0f, -1.0f,
        1.0f, 1.0f, -1.0f,
        1.0f, 1.0f, 1.0f,
        1.0f, 1.0f, 1.0f,
        -1.0f, 1.0f, 1.0f,
        -1.0f, 1.0f, -1.0f,

        // Bottom face
        -1.0f, -1.0f, -1.0f,
        -1.0f, -1.0f, 1.0f,
        1.0f, -1.0f, -1.0f,
        1.0f, -1.0f, -1.0f,
        -1.0f, -1.0f, 1.0f,
        1.0f, -1.0f, 1.0f
    };

    #endregion

    #region Constructor & Initialization

    public SkyBox()
    {
        InitializeBuffers();
    }

    private void InitializeBuffers()
    {
        // Generate and bind VAO
        _skyboxVAO = GL.GenVertexArray();
        GL.BindVertexArray(_skyboxVAO);

        // Generate and bind VBO
        _skyboxVBO = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _skyboxVBO);

        // Upload vertex data
        GL.BufferData(
            BufferTarget.ArrayBuffer,
            SkyboxVertices.Length * sizeof(float),
            SkyboxVertices,
            BufferUsageHint.StaticDraw
        );

        // Configure vertex attributes
        GL.EnableVertexAttribArray(POSITION_ATTRIBUTE_INDEX);
        GL.VertexAttribPointer(
            POSITION_ATTRIBUTE_INDEX,
            COMPONENTS_PER_VERTEX,
            VertexAttribPointerType.Float,
            false,
            COMPONENTS_PER_VERTEX * sizeof(float),
            0
        );

        // Unbind VAO
        GL.BindVertexArray(0);
    }

    #endregion

    #region Rendering

    public void PrepareRender()
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(SkyBox));

        // Configure depth testing for skybox
        ConfigureDepthState();
    }

    private void ConfigureDepthState()
    {
        // Draw skybox only where no geometry has been drawn
        GL.DepthFunc(DepthFunction.Lequal);

        // Don't write to depth buffer (skybox is always at far plane)
        GL.DepthMask(false);
    }

    public void RenderSkybox()
    {
        GL.BindVertexArray(_skyboxVAO);
        GL.DrawArrays(PrimitiveType.Triangles, 0, VERTEX_COUNT);
        GL.BindVertexArray(0);
    }

    public void RestoreDepthState()
    {
        GL.DepthMask(true);
        GL.DepthFunc(DepthFunction.Less);
    }

    #endregion

    #region Resource Management

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_isDisposed) return;

        if (disposing)
        {
            // Dispose managed resources
            // Note: Shader is managed by ShaderDatabase, don't dispose here
        }

        // Free unmanaged resources
        if (_skyboxVAO != 0)
        {
            GL.DeleteVertexArray(_skyboxVAO);
            _skyboxVAO = 0;
        }

        if (_skyboxVBO != 0)
        {
            GL.DeleteBuffer(_skyboxVBO);
            _skyboxVBO = 0;
        }

        _isDisposed = true;
    }

    ~SkyBox()
    {
        Dispose(false);
    }

    #endregion
}