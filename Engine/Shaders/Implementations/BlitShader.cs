namespace Engine.Shaders.Implementations;

/// <summary>
/// Shader used for rendering debug visualizations such as wireframes, physics shapes, and skeletal rigs.
/// Provides a simple unlit rendering for debug geometry.
/// </summary>
public sealed class BlitShader : ShaderBase
{
    #region Constants

    private const string DEFAULT_VERTEX_SHADER = "blit.vert";
    private const string DEFAULT_FRAGMENT_SHADER = "blit.frag";

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the BlitShader with default shader files.
    /// </summary>
    public BlitShader() : base(DEFAULT_VERTEX_SHADER, DEFAULT_FRAGMENT_SHADER)
    {
    }

    /// <summary>
    /// Initializes a new instance of the BlitShader with custom shader files.
    /// </summary>
    /// <param name="vertexShaderFile">The vertex shader file name.</param>
    /// <param name="fragmentShaderFile">The fragment shader file name.</param>
    public BlitShader(string vertexShaderFile, string fragmentShaderFile)
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