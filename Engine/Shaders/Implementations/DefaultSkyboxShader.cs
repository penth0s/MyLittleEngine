using System.Runtime.Serialization;
using Engine.Components;
using Engine.Database.Implementations;
using Engine.Rendering;
using Engine.Systems;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine.Shaders.Implementations;

/// <summary>
/// Basic skybox shader that renders a single cubemap without any additional effects.
/// This is the default skybox shader with minimal parameters.
/// </summary>
public sealed class DefaultSkyboxShader : ShaderBase
{
    #region Constants

    private const string DEFAULT_VERTEX_SHADER = "skybox.vert";
    private const string DEFAULT_FRAGMENT_SHADER = "skybox.frag";

    private const string UNIFORM_VIEW = "view";
    private const string UNIFORM_PROJECTION = "projection";
    private const string UNIFORM_SKYBOX = "uSkybox";

    #endregion

    #region Properties

    public override int GetReflectionMap => _cubemapTexture.Handle;

    #endregion

    #region Fields

    private Texture _cubemapTexture;
    private bool _isInitialized;

    #endregion

    #region Shader Properties (Base Class)

    /// <summary>
    /// Gets the vertex shader file name.
    /// </summary>
    protected override string VertexShaderName => DEFAULT_VERTEX_SHADER;

    /// <summary>
    /// Gets the fragment shader file name.
    /// </summary>
    protected override string FragmentShaderName => DEFAULT_FRAGMENT_SHADER;

    /// <summary>
    /// Gets the geometry shader file name. Default skybox shader does not use a geometry shader.
    /// </summary>
    protected override string GeometryShaderName => null;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the DefaultSkyboxShader with default shader files.
    /// </summary>
    public DefaultSkyboxShader() : this(DEFAULT_VERTEX_SHADER, DEFAULT_FRAGMENT_SHADER)
    {
    }

    /// <summary>
    /// Initializes a new instance of the DefaultSkyboxShader with custom shader files.
    /// </summary>
    /// <param name="vertexName">The vertex shader file name.</param>
    /// <param name="fragmentName">The fragment shader file name.</param>
    public DefaultSkyboxShader(string vertexName, string fragmentName) : base(vertexName, fragmentName)
    {
    }

    #endregion

    #region Initialization

    private void Initialize()
    {
        LoadTexture();
        ConfigureTextureUnit();
    }

    private void LoadTexture()
    {
        var textureDatabase = GetTextureDatabase();
        _cubemapTexture = LoadCubemapTexture(textureDatabase, "Skybox");
    }

    private TextureDataBase GetTextureDatabase()
    {
        var databaseSystem = SystemManager.GetSystem<DatabaseSystem>();
        if (databaseSystem == null) throw new InvalidOperationException("DatabaseSystem not found");

        return databaseSystem.GetDatabase<TextureDataBase>();
    }

    private Texture LoadCubemapTexture(TextureDataBase textureDatabase, string skyboxName)
    {
        var faces = new[]
        {
            $"{skyboxName}/e_px.png", // Positive X (Right)
            $"{skyboxName}/e_nx.png", // Negative X (Left)
            $"{skyboxName}/e_py.png", // Positive Y (Top)
            $"{skyboxName}/e_ny.png", // Negative Y (Bottom)
            $"{skyboxName}/e_pz.png", // Positive Z (Front)
            $"{skyboxName}/e_nz.png" // Negative Z (Back)
        };


        return textureDatabase.LoadSkyboxCubeMap(faces);
    }

    private void ConfigureTextureUnit()
    {
        Use();
        SetInt(UNIFORM_SKYBOX, 0);
    }

    #endregion

    #region Serialization

    [OnDeserialized]
    internal void OnDeserializedMethod(StreamingContext context)
    {
        LoadShader(DEFAULT_VERTEX_SHADER, DEFAULT_FRAGMENT_SHADER);
    }

    #endregion

    #region Shader Updates

    /// <summary>
    /// Updates shader uniforms with camera matrices and binds the cubemap texture.
    /// </summary>
    /// <param name="camera">The camera to use for view and projection matrices.</param>
    /// <exception cref="ArgumentNullException">Thrown when camera is null.</exception>
    public override void UpdateProperties(Camera camera)
    {
        if (camera == null) throw new ArgumentNullException(nameof(camera));

        if (!_isInitialized)
        {
            Initialize();
            _isInitialized = true;
        }

        UpdateMatrices(camera);
        BindCubemapTexture();
    }

    private void UpdateMatrices(Camera camera)
    {
        // Remove translation from view matrix for skybox (always centered on camera)
        var view = new Matrix4(new Matrix3(camera.GetViewMatrix()));
        var projection = camera.GetProjectionMatrix();

        SetMatrix4(UNIFORM_VIEW, view);
        SetMatrix4(UNIFORM_PROJECTION, projection);
    }

    private void BindCubemapTexture()
    {
        // Bind cubemap to texture unit 0
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.TextureCubeMap, _cubemapTexture.Handle);
    }

    #endregion

    #region Dispose

    protected override void CleanupDerivedResources()
    {
        base.CleanupDerivedResources();

        _cubemapTexture?.Dispose();
        _cubemapTexture = null;
    }

    #endregion
}