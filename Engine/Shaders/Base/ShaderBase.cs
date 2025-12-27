using Engine.Components;
using Engine.Database.Implementations;
using Engine.Systems;
using Newtonsoft.Json;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using RenderingMode = Engine.Rendering.RenderingMode;

namespace Engine.Shaders;

/// <summary>
/// Base class for all shader programs in the engine.
/// Handles shader compilation, linking, and uniform variable management.
/// </summary>
public abstract class ShaderBase : IDisposable
{
    #region Constants

    private const int GEOMETRY_SHADER_NOT_PRESENT = -1;
    private const int SHADER_LINK_SUCCESS = 1;
    private const int DEFAULT_STARTING_TEXTURE_UNIT = 1;

    #endregion

    #region Properties

    /// <summary>
    /// The rendering mode for this shader (opaque, transparent, etc.).
    /// </summary>
    public RenderingMode RenderingMode = RenderingMode.OPAQUE;

    /// <summary>
    /// Gets the number of rendering passes this shader requires.
    /// </summary>
    [JsonIgnore]
    public virtual int PassCount => 1;

    /// <summary>
    ///  Gets the refletion cube map
    /// </summary>
    [JsonIgnore]
    public virtual int GetReflectionMap => 0;

    /// <summary>
    /// Gets the OpenGL handle for this shader program.
    /// </summary>
    [JsonIgnore]
    internal int GetShaderHandle => _shaderProgramHandle;

    #endregion

    #region Abstract Properties

    /// <summary>
    /// Gets the file name of the vertex shader.
    /// </summary>
    protected abstract string VertexShaderName { get; }

    /// <summary>
    /// Gets the file name of the fragment shader.
    /// </summary>
    protected abstract string FragmentShaderName { get; }

    /// <summary>
    /// Gets the file name of the geometry shader (null if not used).
    /// </summary>
    protected abstract string GeometryShaderName { get; }

    #endregion

    #region Fields

    private int _shaderProgramHandle;
    private bool _isDisposed;
    private SceneSystem _sceneSystem;
    private ShaderDataBase _shaderData;

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes a new instance of the ShaderBase class.
    /// </summary>
    /// <param name="vertexShaderFile">The vertex shader file name.</param>
    /// <param name="fragmentShaderFile">The fragment shader file name.</param>
    /// <param name="geometryShaderFile">The geometry shader file name (optional).</param>
    protected ShaderBase(string vertexShaderFile, string fragmentShaderFile, string geometryShaderFile = null)
    {
        LoadShader(vertexShaderFile, fragmentShaderFile, geometryShaderFile);
    }

    /// <summary>
    /// Loads and compiles the shader program from the specified files.
    /// </summary>
    protected void LoadShader(string vertexShaderFile, string fragmentShaderFile, string geometryShaderFile = null)
    {
        InitializeSystems();
        var shaderSources = LoadShaderSourceFiles(vertexShaderFile, fragmentShaderFile, geometryShaderFile);
        var compiledShaders = CompileShaders(shaderSources);

        _shaderProgramHandle = CreateShaderProgram(compiledShaders);

        CleanupShaders(compiledShaders);
    }

    private ShaderSources LoadShaderSourceFiles(string vertexFile, string fragmentFile, string geometryFile)
    {
        var vertexPath = _shaderData.GetShaderFilePath(vertexFile);
        var fragmentPath = _shaderData.GetShaderFilePath(fragmentFile);

        var geometryPath = geometryFile != null
            ? _shaderData.GetShaderFilePath(geometryFile)
            : null;

        return new ShaderSources
        {
            VertexSource = File.ReadAllText(vertexPath),
            FragmentSource = File.ReadAllText(fragmentPath),
            GeometrySource = geometryPath != null ? File.ReadAllText(geometryPath) : null
        };
    }

    private CompiledShaders CompileShaders(ShaderSources sources)
    {
        var vertexShader = CompileShader(sources.VertexSource, ShaderType.VertexShader);
        var fragmentShader = CompileShader(sources.FragmentSource, ShaderType.FragmentShader);
        var geometryShader = GEOMETRY_SHADER_NOT_PRESENT;

        if (sources.GeometrySource != null)
            geometryShader = CompileShader(sources.GeometrySource, ShaderType.GeometryShader);

        return new CompiledShaders
        {
            VertexShader = vertexShader,
            FragmentShader = fragmentShader,
            GeometryShader = geometryShader
        };
    }

    private int CompileShader(string shaderSource, ShaderType shaderType)
    {
        var shader = GL.CreateShader(shaderType);
        GL.ShaderSource(shader, shaderSource);
        GL.CompileShader(shader);

        CheckShaderCompilationErrors(shader, shaderType);

        return shader;
    }

    private void CheckShaderCompilationErrors(int shader, ShaderType shaderType)
    {
        var infoLog = GL.GetShaderInfoLog(shader);

        if (!string.IsNullOrEmpty(infoLog)) Console.WriteLine($"{shaderType} Shader Compilation Error: {infoLog}");
    }

    private int CreateShaderProgram(CompiledShaders shaders)
    {
        var program = GL.CreateProgram();

        AttachShadersToProgram(program, shaders);
        LinkShaderProgram(program);

        return program;
    }

    private void AttachShadersToProgram(int program, CompiledShaders shaders)
    {
        GL.AttachShader(program, shaders.VertexShader);
        GL.AttachShader(program, shaders.FragmentShader);

        if (shaders.GeometryShader != GEOMETRY_SHADER_NOT_PRESENT) GL.AttachShader(program, shaders.GeometryShader);
    }

    private void LinkShaderProgram(int program)
    {
        GL.LinkProgram(program);

        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var linkStatus);

        if (linkStatus != SHADER_LINK_SUCCESS)
        {
            var infoLog = GL.GetProgramInfoLog(program);
            Console.WriteLine($"Shader Program Linking Error: {infoLog}");
        }
    }

    private void CleanupShaders(CompiledShaders shaders)
    {
        DetachAndDeleteShader(_shaderProgramHandle, shaders.VertexShader);
        DetachAndDeleteShader(_shaderProgramHandle, shaders.FragmentShader);

        if (shaders.GeometryShader != GEOMETRY_SHADER_NOT_PRESENT)
            DetachAndDeleteShader(_shaderProgramHandle, shaders.GeometryShader);
    }

    private void DetachAndDeleteShader(int program, int shader)
    {
        GL.DetachShader(program, shader);
        GL.DeleteShader(shader);
    }

    private void InitializeSystems()
    {
        _sceneSystem = SystemManager.GetSystem<SceneSystem>();
        _shaderData = SystemManager.GetSystem<DatabaseSystem>().GetDatabase<ShaderDataBase>();
    }

    #endregion

    #region Virtual Methods

    /// <summary>
    /// Updates shader properties from Assimp material data.
    /// Override this to handle material-specific properties.
    /// </summary>
    public virtual void UpdateAssimpProperties(Assimp.Material assimpMaterial)
    {
    }

    /// <summary>
    /// Updates shader properties for rendering.
    /// Override this to set shader uniforms for each render pass.
    /// </summary>
    /// <param name="camera">The active camera.</param>
    /// <param name="transform">The model transform matrix.</param>
    /// <param name="activePass">The current rendering pass index.</param>
    public virtual void UpdateProperties(Camera camera, Matrix4 transform, int activePass = 0)
    {
    }

    public virtual void UpdateProperties(Camera camera)
    {
    }

    #endregion

    #region Light Properties

    /// <summary>
    /// Updates light-related uniforms in the shader.
    /// </summary>
    protected void UpdateLightProperties()
    {
        if (_sceneSystem == null)
            return;

        var directionalLights = _sceneSystem.CurrentScene.GeSceneLights;

        for (var i = 0; i < directionalLights.Count; i++) SetLightUniforms(directionalLights[i], i);
    }

    private void SetLightUniforms(Light light, int lightIndex)
    {
        var prefix = $"lightData[{lightIndex}]";

        SetVector3($"{prefix}.position", light.Transform.LocalPosition);
        SetInt($"{prefix}.lightType", (int)light.LightType);
        SetVector3($"{prefix}.direction", light.Transform.Forward);
        SetVector3($"{prefix}.color", light.GetRGBColor());
        SetFloat($"{prefix}.intensity", light.Intensity);
        SetFloat($"{prefix}.range", light.Range);
        SetFloat($"{prefix}.spotAngle", light.SpotAngle);
        SetFloat($"{prefix}.innerSpotAngle", light.InnerSpotAngle);
        SetMatrix4($"{prefix}.lightsSpaceMatrix", light.GetLightSpaceMatrix());
    }

    #endregion

    #region Shader Usage

    /// <summary>
    /// Activates this shader program for rendering.
    /// </summary>
    public void Use()
    {
        GL.UseProgram(_shaderProgramHandle);
    }

    /// <summary>
    /// Gets the location of a vertex attribute in the shader.
    /// </summary>
    public int GetAttribLocation(string attributeName)
    {
        return GL.GetAttribLocation(_shaderProgramHandle, attributeName);
    }

    #endregion

    #region Uniform Setters - Primitives

    /// <summary>
    /// Sets an integer uniform value.
    /// </summary>
    public void SetInt(string name, int value)
    {
        var location = GetUniformLocation(name);
        GL.Uniform1(location, value);
    }

    /// <summary>
    /// Sets a boolean uniform value.
    /// </summary>
    public void SetBool(string name, bool value)
    {
        var location = GetUniformLocation(name);
        GL.Uniform1(location, value ? 1 : 0);
    }

    /// <summary>
    /// Sets a float uniform value.
    /// </summary>
    public void SetFloat(string name, float value)
    {
        var location = GetUniformLocation(name);
        GL.Uniform1(location, value);
    }

    #endregion

    #region Uniform Setters - Vectors

    /// <summary>
    /// Sets a Vector2 uniform value.
    /// </summary>
    public void SetVector2(string name, Vector2 value)
    {
        var location = GetUniformLocation(name);
        GL.Uniform2(location, value);
    }

    /// <summary>
    /// Sets a Vector3 uniform value.
    /// </summary>
    public void SetVector3(string name, Vector3 value)
    {
        var location = GetUniformLocation(name);
        GL.Uniform3(location, value);
    }

    /// <summary>
    /// Sets a Vector4 uniform value.
    /// </summary>
    public void SetVector4(string name, Vector4 value)
    {
        var location = GetUniformLocation(name);
        GL.Uniform4(location, value);
    }

    /// <summary>
    /// Sets a Color4 uniform value.
    /// </summary>
    public void SetColor(string name, Color4 value)
    {
        var location = GetUniformLocation(name);
        GL.Uniform4(location, value);
    }

    #endregion

    #region Uniform Setters - Matrices

    /// <summary>
    /// Sets a Matrix4 uniform value.
    /// </summary>
    public void SetMatrix4(string name, Matrix4 matrix, bool transpose = false)
    {
        var location = GetUniformLocation(name);
        GL.UniformMatrix4(location, transpose, ref matrix);
    }

    /// <summary>
    /// Uploads an array of bone transformation matrices for skeletal animation.
    /// </summary>
    public void UploadBoneMatrices(Matrix4[] boneMatrices)
    {
        for (var i = 0; i < boneMatrices.Length; i++) SetMatrix4($"uBoneMatrices[{i}]", boneMatrices[i], false);
    }

    #endregion

    #region Uniform Setters - Textures

    /// <summary>
    /// Binds a texture to a texture unit and sets the corresponding sampler uniform.
    /// </summary>
    protected void SetTexture(string name, int textureId, int textureSlot = 0)
    {
        GL.ActiveTexture(TextureUnit.Texture0 + textureSlot);
        GL.BindTexture(TextureTarget.Texture2D, textureId);

        var location = GetUniformLocation(name);
        GL.Uniform1(location, textureSlot);
    }

    /// <summary>
    /// Binds an array of shadow map textures to consecutive texture units.
    /// </summary>
    public void SetShadowMapArray(
        string uniformName,
        List<int> textureIds,
        int startingTextureUnit = DEFAULT_STARTING_TEXTURE_UNIT)
    {
        var location = GetUniformLocation($"{uniformName}[0]");

        if (location == -1)
        {
            Console.WriteLine($"Uniform array '{uniformName}' not found in shader.");
            return;
        }

        var textureUnits = BindShadowMapTextures(textureIds, startingTextureUnit);
        GL.Uniform1(location, textureIds.Count, textureUnits);
    }

    private int[] BindShadowMapTextures(List<int> textureIds, int startingTextureUnit)
    {
        var textureUnits = new int[textureIds.Count];

        for (var i = 0; i < textureIds.Count; i++)
        {
            var textureUnitIndex = startingTextureUnit + i;
            textureUnits[i] = textureUnitIndex;

            GL.ActiveTexture(TextureUnit.Texture0 + textureUnitIndex);
            GL.BindTexture(TextureTarget.Texture2D, textureIds[i]);
        }

        return textureUnits;
    }

    #endregion

    #region Helper Methods

    private int GetUniformLocation(string uniformName)
    {
        return GL.GetUniformLocation(_shaderProgramHandle, uniformName);
    }

    #endregion

    #region Disposal

    /// <summary>
    /// Releases OpenGL resources used by this shader.
    /// </summary>
    public void Dispose()
    {
        DisposeShader(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases resources used by this shader.
    /// Override CleanupDerivedResources() in derived classes to clean up additional resources.
    /// </summary>
    private void DisposeShader(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
        {
            CleanupDerivedResources();
            DisposeShaderProgram();
        }

        _isDisposed = true;
    }

    /// <summary>
    /// Virtual method for derived classes to clean up their specific resources (textures, etc.).
    /// Override this method in derived shader classes to dispose of textures and other resources.
    /// </summary>
    protected virtual void CleanupDerivedResources()
    {
        // Base implementation does nothing
    }

    private void DisposeShaderProgram()
    {
        if (_shaderProgramHandle != 0 && GL.IsProgram(_shaderProgramHandle))
        {
            GL.DeleteProgram(_shaderProgramHandle);
            _shaderProgramHandle = 0;
        }
    }

    #endregion

    #region Helper Structures

    private struct ShaderSources
    {
        public string VertexSource;
        public string FragmentSource;
        public string GeometrySource;
    }

    private struct CompiledShaders
    {
        public int VertexShader;
        public int FragmentShader;
        public int GeometryShader;
    }

    #endregion
}