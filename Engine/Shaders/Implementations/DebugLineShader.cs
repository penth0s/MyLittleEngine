namespace Engine.Shaders.Implementations;

/// <summary>
/// Shader used for rendering debug lines, spheres, and axes.
/// Simple pass-through rendering without geometry shader.
/// </summary>
public sealed class DebugLineShader : ShaderBase
{
    #region Constants

    private const string DEFAULT_VERTEX_SHADER = "debug_line.vert";
    private const string DEFAULT_FRAGMENT_SHADER = "debug_line.frag";

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the DebugLineShader with default shader files.
    /// </summary>
    public DebugLineShader() : base(DEFAULT_VERTEX_SHADER, DEFAULT_FRAGMENT_SHADER)
    {
    }

    /// <summary>
    /// Initializes a new instance of the DebugLineShader with custom shader files.
    /// </summary>
    /// <param name="vertexShaderFile">The vertex shader file name.</param>
    /// <param name="fragmentShaderFile">The fragment shader file name.</param>
    public DebugLineShader(string vertexShaderFile, string fragmentShaderFile)
        : base(vertexShaderFile, fragmentShaderFile)
    {
    }

    #endregion

    #region Shader Properties

    /// <summary>
    /// Gets the vertex shader file name.
    /// </summary>
    protected override string VertexShaderName => DEFAULT_VERTEX_SHADER;

    /// <summary>
    /// Gets the fragment shader file name.
    /// </summary>
    protected override string FragmentShaderName => DEFAULT_FRAGMENT_SHADER;

    /// <summary>
    /// Debug line shader does not use a geometry shader.
    /// </summary>
    protected override string GeometryShaderName => null;

    #endregion
}