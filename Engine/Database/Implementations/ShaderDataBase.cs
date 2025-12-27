using Engine.Shaders;
using Engine.Utilities;

namespace Engine.Database.Implementations;

/// <summary>
/// Database for managing shader types, instances, and shader file paths.
/// Provides reflection-based discovery of all available shaders and factory methods for creating shader instances.
/// </summary>
internal sealed class ShaderDataBase : IDatabase
{
    #region Constants

    private const string SHADERS_FOLDER_NAME = "Shaders";

    #endregion

    #region Fields

    private List<Type> _availableShaderTypes;
    private Dictionary<string, Type> _shaderTypeByName;
    private Dictionary<string, string> _shaderFilePathMap;

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the shader database by discovering all available shader types via reflection
    /// and discovering all shader files in engine and example paths.
    /// </summary>
    public void Initialize()
    {
        DiscoverAvailableShaders();
        BuildShaderNameMapping();
        DiscoverShaderFiles();
    }

    private void DiscoverAvailableShaders()
    {
        _availableShaderTypes = FindAllShaderTypes();
    }

    private List<Type> FindAllShaderTypes()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(IsValidShaderType)
            .ToList();
    }

    private bool IsValidShaderType(Type type)
    {
        return !type.IsAbstract
               && !type.IsInterface
               && typeof(ShaderBase).IsAssignableFrom(type)
               && type != typeof(ShaderBase);
    }

    private void BuildShaderNameMapping()
    {
        _shaderTypeByName = _availableShaderTypes.ToDictionary(
            type => type.Name,
            type => type
        );
    }

    #endregion

    #region Shader File Discovery

    private void DiscoverShaderFiles()
    {
        _shaderFilePathMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        DiscoverShaderFilesInPath(ProjectPathHelper.GetEnginePath());
        DiscoverShaderFilesInPath(ProjectPathHelper.GetExamplePath());
    }

    private void DiscoverShaderFilesInPath(string basePath)
    {
        var shadersPath = Path.Combine(basePath, SHADERS_FOLDER_NAME);

        if (!Directory.Exists(shadersPath))
        {
            Console.WriteLine($"Warning: Shader directory not found: {shadersPath}");
            return;
        }

        try
        {
            DiscoverVertexShaders(shadersPath);
            DiscoverFragmentShaders(shadersPath);
            DiscoverGeometryShaders(shadersPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error discovering shaders in {shadersPath}: {ex.Message}");
        }
    }

    private void DiscoverVertexShaders(string shadersPath)
    {
        var vertexFiles = Directory.GetFiles(shadersPath, "*.vert", SearchOption.AllDirectories);
        foreach (var filePath in vertexFiles)
        {
            var fileName = Path.GetFileName(filePath);
            RegisterShaderFile(fileName, filePath);
        }
    }

    private void DiscoverFragmentShaders(string shadersPath)
    {
        var fragmentFiles = Directory.GetFiles(shadersPath, "*.frag", SearchOption.AllDirectories);
        foreach (var filePath in fragmentFiles)
        {
            var fileName = Path.GetFileName(filePath);
            RegisterShaderFile(fileName, filePath);
        }
    }

    private void DiscoverGeometryShaders(string shadersPath)
    {
        var geometryFiles = Directory.GetFiles(shadersPath, "*.geom", SearchOption.AllDirectories);
        foreach (var filePath in geometryFiles)
        {
            var fileName = Path.GetFileName(filePath);
            RegisterShaderFile(fileName, filePath);
        }
    }

    private void RegisterShaderFile(string fileName, string fullPath)
    {
        if (!_shaderFilePathMap.ContainsKey(fileName))
        {
            _shaderFilePathMap[fileName] = fullPath;
            Console.WriteLine($"Registered shader file: {fileName} -> {fullPath}");
        }
        else
        {
            Console.WriteLine($"Warning: Duplicate shader file name: {fileName}");
        }
    }

    #endregion

    #region Shader File Path Query

    /// <summary>
    /// Gets the full file path for a shader file by its name.
    /// </summary>
    /// <param name="shaderFileName">The shader file name (e.g., "shader.vert", "shader.frag")</param>
    /// <returns>The full path to the shader file, or null if not found</returns>
    public string GetShaderFilePath(string shaderFileName)
    {
        if (string.IsNullOrWhiteSpace(shaderFileName)) return null;

        if (_shaderFilePathMap.TryGetValue(shaderFileName, out var path)) return path;

        Console.WriteLine($"Warning: Shader file not found: {shaderFileName}");
        return null;
    }

    /// <summary>
    /// Gets the full file paths for vertex and fragment shaders.
    /// </summary>
    /// <param name="vertexFileName">Vertex shader file name</param>
    /// <param name="fragmentFileName">Fragment shader file name</param>
    /// <returns>Tuple of (vertexPath, fragmentPath). Either can be null if not found.</returns>
    public (string vertexPath, string fragmentPath) GetShaderFilePaths(string vertexFileName, string fragmentFileName)
    {
        var vertexPath = GetShaderFilePath(vertexFileName);
        var fragmentPath = GetShaderFilePath(fragmentFileName);

        return (vertexPath, fragmentPath);
    }

    /// <summary>
    /// Gets the full file paths for vertex, fragment, and geometry shaders.
    /// </summary>
    /// <param name="vertexFileName">Vertex shader file name</param>
    /// <param name="fragmentFileName">Fragment shader file name</param>
    /// <param name="geometryFileName">Geometry shader file name (optional)</param>
    /// <returns>Tuple of (vertexPath, fragmentPath, geometryPath). Any can be null if not found.</returns>
    public (string vertexPath, string fragmentPath, string geometryPath) GetShaderFilePaths(
        string vertexFileName,
        string fragmentFileName,
        string geometryFileName)
    {
        var vertexPath = GetShaderFilePath(vertexFileName);
        var fragmentPath = GetShaderFilePath(fragmentFileName);
        var geometryPath = !string.IsNullOrWhiteSpace(geometryFileName)
            ? GetShaderFilePath(geometryFileName)
            : null;

        return (vertexPath, fragmentPath, geometryPath);
    }

    /// <summary>
    /// Gets all discovered shader file names.
    /// </summary>
    /// <returns>A read-only list of shader file names</returns>
    public IReadOnlyList<string> GetAvailableShaderFiles()
    {
        return _shaderFilePathMap.Keys.ToList();
    }

    /// <summary>
    /// Checks if a shader file exists in the database.
    /// </summary>
    /// <param name="shaderFileName">The shader file name to check</param>
    /// <returns>True if the shader file exists, false otherwise</returns>
    public bool ShaderFileExists(string shaderFileName)
    {
        if (string.IsNullOrWhiteSpace(shaderFileName)) return false;

        return _shaderFilePathMap.ContainsKey(shaderFileName);
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Gets a read-only list of all available shader names.
    /// Useful for populating editor UI dropdowns.
    /// </summary>
    /// <returns>A read-only list of shader names.</returns>
    public IReadOnlyList<string> GetAvailableShaderNames()
    {
        return _shaderTypeByName.Keys.ToList();
    }

    #endregion

    #region Factory Methods - By Name

    /// <summary>
    /// Creates a shader instance by name.
    /// Primarily used by editor UI for shader selection.
    /// </summary>
    /// <param name="shaderName">The name of the shader to create.</param>
    /// <returns>A new instance of the specified shader.</returns>
    /// <exception cref="ArgumentException">Thrown when the shader name is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the shader is not found or cannot be instantiated.</exception>
    public ShaderBase GetByName(string shaderName)
    {
        ValidateShaderName(shaderName);

        var shaderType = GetShaderTypeByName(shaderName);
        return CreateShaderInstance(shaderType);
    }

    private void ValidateShaderName(string shaderName)
    {
        if (string.IsNullOrWhiteSpace(shaderName))
            throw new ArgumentException(
                "Shader name cannot be null or empty.",
                nameof(shaderName)
            );
    }

    private Type GetShaderTypeByName(string shaderName)
    {
        if (!_shaderTypeByName.TryGetValue(shaderName, out var shaderType))
            throw new InvalidOperationException(
                $"Shader not found: {shaderName}. " +
                $"Available shaders: {string.Join(", ", _shaderTypeByName.Keys)}"
            );

        return shaderType;
    }

    #endregion

    #region Factory Methods - By Type

    /// <summary>
    /// Creates a shader instance from a Type object.
    /// </summary>
    /// <param name="shaderType">The type of shader to create.</param>
    /// <returns>A new instance of the specified shader type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the shader type is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the type is not a valid shader type.</exception>
    public ShaderBase GetByType(Type shaderType)
    {
        ValidateShaderType(shaderType);
        return CreateShaderInstance(shaderType);
    }

    private void ValidateShaderType(Type shaderType)
    {
        if (shaderType == null)
            throw new ArgumentNullException(
                nameof(shaderType),
                "Shader type cannot be null."
            );

        if (!typeof(ShaderBase).IsAssignableFrom(shaderType))
            throw new ArgumentException(
                $"Type {shaderType.Name} does not inherit from ShaderBase.",
                nameof(shaderType)
            );
    }

    #endregion

    #region Factory Methods - Generic

    /// <summary>
    /// Creates a shader instance using generic type parameter.
    /// This is the preferred method for creating shaders from code.
    /// </summary>
    /// <typeparam name="T">The type of shader to create.</typeparam>
    /// <returns>A new instance of the specified shader type.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the shader cannot be instantiated.</exception>
    public T Get<T>() where T : ShaderBase
    {
        var shaderType = typeof(T);
        var instance = CreateShaderInstance(shaderType);

        return (T)instance;
    }

    #endregion

    #region Helper Methods

    private ShaderBase CreateShaderInstance(Type shaderType)
    {
        try
        {
            var instance = Activator.CreateInstance(shaderType) as ShaderBase;

            if (instance == null)
                throw new InvalidOperationException(
                    $"Failed to create instance of {shaderType.Name}. " +
                    "Ensure the shader has a public parameterless constructor."
                );

            return instance;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"Error creating shader instance of type {shaderType.Name}: {ex.Message}",
                ex
            );
        }
    }

    #endregion
}