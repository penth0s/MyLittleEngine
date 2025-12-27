using Engine.Database.Implementations;
using Engine.Scene;
using Newtonsoft.Json;
using Environment = Engine.Scene.Environment;

namespace Engine.Systems;

/// <summary>
/// System responsible for managing scene loading, saving, and lifecycle.
/// Maintains the active scene and handles scene transitions.
/// </summary>
public sealed class SceneSystem : ISystem
{
    #region Constants

    private const string DEFAULT_SCENE_NAME = "CapeScene";
    private const string SCENE_FILE_EXTENSION = ".scene";

    #endregion

    #region Properties

    /// <summary>
    /// Gets the currently active scene.
    /// </summary>
    public Scene.Scene CurrentScene => _activeScene;

    /// <summary>
    /// Indicates whether the scene system is ready and not in the middle of a scene transition.
    /// False during scene loading, true when scene is fully loaded and ready.
    /// </summary>
    public bool IsSceneReady { get; private set; } = true;

    #endregion

    #region Fields

    private Scene.Scene _activeScene;
    private EngineInfoProviderSystem _engineInfoProviderSystem;

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the scene system.
    /// </summary>
    public void Initialize()
    {
        InitializeSystems();
    }

    private void InitializeSystems()
    {
        _engineInfoProviderSystem = SystemManager.GetSystem<EngineInfoProviderSystem>();
        _engineInfoProviderSystem.EngineShutdown += OnEngineShutdown;
    }

    private void OnEngineShutdown()
    {
        SaveActiveScene();
    }

    #endregion

    #region File Selection

    /// <summary>
    /// Handles file selection events. Loads a scene if a .scene file is selected.
    /// </summary>
    /// <param name="filePath">The path to the selected file.</param>
    public void OnFileSelected(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            Console.WriteLine("Warning: File path is null or empty.");
            return;
        }

        if (IsSceneFile(filePath)) LoadSceneFromFile(filePath);
    }

    private bool IsSceneFile(string filePath)
    {
        return Path.GetExtension(filePath) == SCENE_FILE_EXTENSION;
    }

    private void LoadSceneFromFile(string filePath)
    {
        var sceneName = Path.GetFileNameWithoutExtension(filePath);
        Console.WriteLine($"Loading scene: {sceneName}");
        LoadScene(sceneName);
    }

    #endregion

    #region Scene Loading

    /// <summary>
    /// Loads a scene by name. If no name is provided, loads the default scene.
    /// </summary>
    /// <param name="sceneName">The name of the scene to load, or null for default scene.</param>
    public void LoadScene(string sceneName = null)
    {
        IsSceneReady = false;

        try
        {
            sceneName = ResolveSceneName(sceneName);

            SaveAndUnloadCurrentScene();
            Environment.Dispose();
            LoadNewScene(sceneName);
            //CreateNewScene("CapeScene");
            NotifySceneLoaded();
        }
        finally
        {
            IsSceneReady = true;
        }
    }

    private void CreateNewScene(string sceneName)
    {
        _activeScene = new Scene.Scene(sceneName);
    }

    private string ResolveSceneName(string? sceneName)
    {
        return sceneName ?? DEFAULT_SCENE_NAME;
    }

    private void SaveAndUnloadCurrentScene()
    {
        SaveActiveScene();
        UnloadCurrentScene();
    }

    private void UnloadCurrentScene()
    {
        _activeScene?.DestroyScene();
        _activeScene = null;
    }

    private void LoadNewScene(string sceneName)
    {
        var sceneData = LoadSceneDataFromDisk(sceneName);
        _activeScene = CreateSceneFromData(sceneData);
        UpdateEditorSettings(sceneData);
    }

    private SceneSaveData LoadSceneDataFromDisk(string sceneName)
    {
        try
        {
            var sceneJson = SceneDataBase.LoadScene(sceneName);
            return DeserializeSceneData(sceneJson);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading scene '{sceneName}': {ex.Message}");
            throw;
        }
    }

    private SceneSaveData DeserializeSceneData(string sceneJson)
    {
        var settings = CreateJsonSerializerSettings();
        return JsonConvert.DeserializeObject<SceneSaveData>(sceneJson, settings);
    }

    private JsonSerializerSettings CreateJsonSerializerSettings()
    {
        return new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            NullValueHandling = NullValueHandling.Ignore
        };
    }

    private Scene.Scene CreateSceneFromData(SceneSaveData sceneData)
    {
        return new Scene.Scene(sceneData);
    }

    private void NotifySceneLoaded()
    {
        _engineInfoProviderSystem.OnNewSceneLoaded();
    }

    private void UpdateEditorSettings(SceneSaveData saveData)
    {
        _engineInfoProviderSystem.IsGridEnabled = saveData.IsGridEnabled;
        _engineInfoProviderSystem.IsWireframeEnabled = saveData.IsWireframeEnabled;
        _engineInfoProviderSystem.IsDebugViewEnabled = saveData.IsDebugViewEnabled;
    }

    #endregion

    #region Scene Saving

    /// <summary>
    /// Saves the currently active scene to disk.
    /// </summary>
    public void SaveActiveScene()
    {
        if (!HasActiveScene())
        {
            LogNoActiveScene();
            return;
        }

        SaveCurrentScene();
    }

    private bool HasActiveScene()
    {
        return _activeScene != null;
    }

    private void LogNoActiveScene()
    {
        Console.WriteLine("No active scene to save.");
    }

    private void SaveCurrentScene()
    {
        try
        {
            _activeScene.SaveScene();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving scene: {ex.Message}");
            throw;
        }
    }

    #endregion
}