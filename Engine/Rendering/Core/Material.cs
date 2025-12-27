using System.Runtime.Serialization;
using Engine.Components;
using Engine.Database.Implementations;
using Engine.Shaders;
using Engine.Shaders.Implementations;
using Engine.Systems;
using Newtonsoft.Json;
using OpenTK.Mathematics;

namespace Engine.Rendering;

/// <summary>
/// Represents a material that defines the visual appearance of a rendered object.
/// Materials contain shader references and manage shader properties and rendering modes.
/// </summary>
public sealed class Material : IDisposable
{
    #region Properties

    /// <summary>
    /// Gets or sets the shader used by this material.
    /// </summary>
    public ShaderBase Shader { get; set; }

    /// <summary>
    /// Gets the shader selector UI helper for this material.
    /// </summary>
    public ShaderSelector ShaderSelector { get; private set; }

    /// <summary>
    /// Gets the rendering mode of the current shader (opaque, transparent, etc.).
    /// </summary>
    [JsonIgnore]
    public RenderingMode RenderingMode => Shader?.RenderingMode ?? RenderingMode.OPAQUE;

    #endregion

    #region Fields

    private bool _isDisposed;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the Material class with no shader assigned.
    /// </summary>
    public Material()
    {
        InitializeShaderSelector();
    }

    /// <summary>
    /// Initializes a new instance of the Material class from Assimp material data.
    /// Uses StandardShader by default and applies Assimp material properties.
    /// </summary>
    /// <param name="assimpMaterial">The Assimp material data to import.</param>
    public Material(Assimp.Material assimpMaterial)
    {
        InitializeFromAssimpMaterial(assimpMaterial);
        InitializeShaderSelector();
    }

    private void InitializeShaderSelector()
    {
        ShaderSelector = new ShaderSelector(this);
    }

    private void InitializeFromAssimpMaterial(Assimp.Material assimpMaterial)
    {
        UpdateShader<StandardShader>();
        Shader?.UpdateAssimpProperties(assimpMaterial);
    }

    #endregion

    #region Serialization

    /// <summary>
    /// Called after deserialization to reinitialize transient state.
    /// </summary>
    [OnDeserialized]
    internal void OnDeserialized(StreamingContext context)
    {
        InitializeShaderSelector();
    }

    #endregion

    #region Shader Management

    /// <summary>
    /// Updates the material to use a shader of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of shader to use.</typeparam>
    /// <returns>The shader instance.</returns>
    public T UpdateShader<T>() where T : ShaderBase
    {
        Shader?.Dispose();
        Shader = GetShaderFromDatabase<T>();
        return (T)Shader;
    }

    private T GetShaderFromDatabase<T>() where T : ShaderBase
    {
        var databaseSystem = SystemManager.GetSystem<DatabaseSystem>();
        var shaderDatabase = databaseSystem.GetDatabase<ShaderDataBase>();

        return shaderDatabase.Get<T>();
    }

    /// <summary>
    /// Activates this material's shader for rendering.
    /// </summary>
    public void BindShader()
    {
        if (Shader == null)
        {
            Console.WriteLine("Warning: Cannot bind shader - Material has no shader assigned.");
            return;
        }

        Shader.Use();
    }

    #endregion

    #region Shader Property Updates

    /// <summary>
    /// Updates shader properties for rendering with the given camera and transform.
    /// </summary>
    /// <param name="camera">The active camera.</param>
    /// <param name="transform">The model transform matrix.</param>
    /// <param name="activePass">The current rendering pass index.</param>
    public void UpdateProperties(Camera camera, Matrix4 transform, int activePass = 0)
    {
        if (Shader == null)
        {
            Console.WriteLine("Warning: Cannot update shader properties - Material has no shader assigned.");
            return;
        }

        Shader.UpdateProperties(camera, transform, activePass);
    }

    #endregion

    #region Disposal

    /// <summary>
    /// Releases resources used by this material.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        DisposeShader();

        _isDisposed = true;
    }

    private void DisposeShader()
    {
        Shader?.Dispose();
        Shader = null;
    }

    #endregion
}