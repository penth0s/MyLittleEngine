namespace Engine.Shaders.Implementations;

/// <summary>
/// Shader used for rendering shadow maps from light perspectives.
/// Outputs depth information for shadow mapping calculations.
/// </summary>
public sealed class ShadowShader : ShaderBase
{
    #region Constants

    private const string DEFAULT_VERTEX_SHADER = "shadow.vert";
    private const string DEFAULT_FRAGMENT_SHADER = "shadow.frag";

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the ShadowShader with default shader files.
    /// </summary>
    public ShadowShader() : base(DEFAULT_VERTEX_SHADER, DEFAULT_FRAGMENT_SHADER)
    {
    }

    /// <summary>
    /// Initializes a new instance of the ShadowShader with custom shader files.
    /// </summary>
    /// <param name="vertexShaderFile">The vertex shader file name.</param>
    /// <param name="fragmentShaderFile">The fragment shader file name.</param>
    public ShadowShader(string vertexShaderFile, string fragmentShaderFile)
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
    /// Gets the geometry shader file name. Shadow shader does not use a geometry shader.
    /// </summary>
    protected override string GeometryShaderName => null;

    #endregion
}