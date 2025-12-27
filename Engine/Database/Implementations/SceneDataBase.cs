using Engine.Utilities;

namespace Engine.Database.Implementations;

/// <summary>
/// Database for managing scene serialization and deserialization.
/// Handles saving and loading scene data to/from disk.
/// </summary>
internal sealed class SceneDataBase : IDatabase
{
    #region Constants

    private const string SCENES_FOLDER_PATH = "Assets/Scenes";
    private const string SCENE_FILE_EXTENSION = ".scene";

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the scene database.
    /// </summary>
    public void Initialize()
    {
        EnsureScenesFolderExists();
    }

    private void EnsureScenesFolderExists()
    {
        var projectPath = ProjectPathHelper.GetExamplePath();
        var scenesFolder = Path.Combine(projectPath, SCENES_FOLDER_PATH);

        Directory.CreateDirectory(scenesFolder);
    }

    #endregion

    #region Scene Saving

    /// <summary>
    /// Saves scene data to disk.
    /// </summary>
    /// <param name="sceneName">The name of the scene to save.</param>
    /// <param name="sceneData">The serialized scene data.</param>
    public static void SaveScene(string sceneName, string sceneData)
    {
        ValidateSceneName(sceneName);
        ValidateSceneData(sceneData);

        var filePath = GetSceneFilePath(sceneName);
        EnsureDirectoryExists(filePath);
        WriteSceneDataToFile(filePath, sceneData);
    }

    private static void ValidateSceneName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            throw new ArgumentException(
                "Scene name cannot be null or empty.",
                nameof(sceneName)
            );
    }

    private static void ValidateSceneData(string sceneData)
    {
        if (sceneData == null)
            throw new ArgumentNullException(
                nameof(sceneData),
                "Scene data cannot be null."
            );
    }

    private static void EnsureDirectoryExists(string filePath)
    {
        var directoryPath = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directoryPath)) Directory.CreateDirectory(directoryPath);
    }

    private static void WriteSceneDataToFile(string filePath, string sceneData)
    {
        try
        {
            File.WriteAllText(filePath, sceneData);
        }
        catch (Exception ex)
        {
            throw new IOException(
                $"Failed to write scene data to file: {filePath}",
                ex
            );
        }
    }

    #endregion

    #region Scene Loading

    /// <summary>
    /// Loads scene data from disk.
    /// </summary>
    /// <param name="sceneName">The name of the scene to load.</param>
    /// <returns>The serialized scene data.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the scene file does not exist.</exception>
    public static string LoadScene(string sceneName)
    {
        ValidateSceneName(sceneName);

        var filePath = GetSceneFilePath(sceneName);
        ValidateSceneFileExists(filePath, sceneName);

        return ReadSceneDataFromFile(filePath);
    }

    private static void ValidateSceneFileExists(string filePath, string sceneName)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException(
                $"Scene '{sceneName}' not found at: {filePath}",
                filePath
            );
    }

    private static string ReadSceneDataFromFile(string filePath)
    {
        try
        {
            return File.ReadAllText(filePath);
        }
        catch (Exception ex)
        {
            throw new IOException(
                $"Failed to read scene data from file: {filePath}",
                ex
            );
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets the full file path for a scene.
    /// </summary>
    /// <param name="sceneName">The name of the scene.</param>
    /// <returns>The complete file path for the scene.</returns>
    private static string GetSceneFilePath(string sceneName)
    {
        var projectPath = ProjectPathHelper.GetExamplePath();
        var fileName = $"{sceneName}{SCENE_FILE_EXTENSION}";

        return Path.Combine(projectPath, SCENES_FOLDER_PATH, fileName);
    }

    #endregion
}