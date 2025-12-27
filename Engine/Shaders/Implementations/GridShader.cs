namespace Engine.Shaders.Implementations;

/// <summary>
/// Shader used for rendering the editor grid on the ground plane.
/// Supports distance-based fade-out for better visual clarity.
/// </summary>
public sealed class GridShader : ShaderBase
{
    #region Constants

    private const string DEFAULT_VERTEX_SHADER = "grid.vert";
    private const string DEFAULT_FRAGMENT_SHADER = "grid.frag";

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the GridShader with default shader files.
    /// </summary>
    public GridShader() : base(DEFAULT_VERTEX_SHADER, DEFAULT_FRAGMENT_SHADER)
    {
    }

    /// <summary>
    /// Initializes a new instance of the GridShader with custom shader files.
    /// </summary>
    /// <param name="vertexShaderFile">The vertex shader file name.</param>
    /// <param name="fragmentShaderFile">The fragment shader file name.</param>
    public GridShader(string vertexShaderFile, string fragmentShaderFile)
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
    /// Gets the geometry shader file name. Grid shader does not use a geometry shader.
    /// </summary>
    protected override string GeometryShaderName => null;

    #endregion
}