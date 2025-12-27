using Engine.Database.Implementations;
using Engine.Rendering;
using Engine.Systems;

namespace Engine.Shaders;

/// <summary>
/// Helper class for managing shader selection and binding for materials.
/// Provides UI-friendly methods for shader management in the editor.
/// </summary>
public sealed class ShaderSelector
{
    #region Fields

    private readonly Material _material;
    private readonly ShaderDataBase _shaderDatabase;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the ShaderSelector class.
    /// </summary>
    /// <param name="material">The material whose shader will be managed.</param>
    public ShaderSelector(Material material)
    {
        _material = material ?? throw new ArgumentNullException(nameof(material));
        _shaderDatabase = GetShaderDatabase();
    }

    private ShaderDataBase GetShaderDatabase()
    {
        var databaseSystem = SystemManager.GetSystem<DatabaseSystem>();
        return databaseSystem.GetDatabase<ShaderDataBase>();
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Gets a list of all available shader names.
    /// </summary>
    /// <returns>A read-only list of shader names for UI display.</returns>
    public IReadOnlyList<string> GetAvailableShaderNames()
    {
        return _shaderDatabase.GetAvailableShaderNames();
    }

    /// <summary>
    /// Gets the name of the currently active shader on the material.
    /// </summary>
    /// <returns>The name of the current shader, or "None" if no shader is set.</returns>
    public string GetCurrentShaderName()
    {
        if (_material.Shader == null) return "None";

        return _material.Shader.GetType().Name;
    }

    #endregion

    #region Shader Management

    /// <summary>
    /// Changes the material's shader to the specified shader by name.
    /// Disposes the previous shader before binding the new one.
    /// </summary>
    /// <param name="shaderName">The name of the shader to bind.</param>
    /// <exception cref="ArgumentException">Thrown when the shader name is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the shader cannot be found or created.</exception>
    public void BindShader(string shaderName)
    {
        ValidateShaderName(shaderName);

        DisposeCurrentShader();
        AssignNewShader(shaderName);
    }

    private void ValidateShaderName(string shaderName)
    {
        if (string.IsNullOrWhiteSpace(shaderName))
            throw new ArgumentException(
                "Shader name cannot be null or empty.",
                nameof(shaderName)
            );
    }

    private void DisposeCurrentShader()
    {
        _material.Shader?.Dispose();
    }

    private void AssignNewShader(string shaderName)
    {
        try
        {
            _material.Shader = _shaderDatabase.GetByName(shaderName);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to bind shader '{shaderName}' to material: {ex.Message}",
                ex
            );
        }
    }

    /// <summary>
    /// Changes the material's shader to the specified shader type.
    /// </summary>
    /// <typeparam name="T">The type of shader to bind.</typeparam>
    public void BindShader<T>() where T : ShaderBase
    {
        DisposeCurrentShader();
        AssignNewShader<T>();
    }

    private void AssignNewShader<T>() where T : ShaderBase
    {
        try
        {
            _material.Shader = _shaderDatabase.Get<T>();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to bind shader of type '{typeof(T).Name}' to material: {ex.Message}",
                ex
            );
        }
    }

    #endregion
}