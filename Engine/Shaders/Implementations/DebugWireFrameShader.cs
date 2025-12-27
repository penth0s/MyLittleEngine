namespace Engine.Shaders.Implementations;

/// <summary>
/// Shader used for rendering debug visualizations such as wireframes, physics shapes, and skeletal rigs.
/// Provides a simple unlit rendering for debug geometry.
/// </summary>
public sealed class DebugWireFrameShader : ShaderBase
{
    #region Constants

    private const string DEFAULT_VERTEX_SHADER = "debug_wireframe.vert";
    private const string DEFAULT_FRAGMENT_SHADER = "debug_wireframe.frag";
    private const string DEFAULT_GEOMETRY_SHADER = "debug_wireframe.geom";

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the DebugWireFrameShader with default shader files.
    /// </summary>
    public DebugWireFrameShader() : base(DEFAULT_VERTEX_SHADER, DEFAULT_FRAGMENT_SHADER, DEFAULT_GEOMETRY_SHADER)
    {
    }

    /// <summary>
    /// Initializes a new instance of the DebugWireFrameShader with custom shader files.
    /// </summary>
    /// <param name="vertexShaderFile">The vertex shader file name.</param>
    /// <param name="fragmentShaderFile">The fragment shader file name.</param>
    public DebugWireFrameShader(string vertexShaderFile, string fragmentShaderFile)
        : base(vertexShaderFile, fragmentShaderFile)
    {
    }

    #endregion

    #region Shader Properties

    /// <summary>
    /// Gets the vertex shader file name (not used when constructor specifies files).
    /// </summary>
    protected override string VertexShaderName => DEFAULT_VERTEX_SHADER;

    /// <summary>
    /// Gets the fragment shader file name (not used when constructor specifies files).
    /// </summary>
    protected override string FragmentShaderName => DEFAULT_FRAGMENT_SHADER;

    /// <summary>
    /// Gets the geometry shader file name. Debug shader does not use a geometry shader.
    /// </summary>
    protected override string GeometryShaderName => null;

    #endregion
}