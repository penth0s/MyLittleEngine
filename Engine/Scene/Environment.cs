using System.Numerics;
using Engine.Components;
using Engine.Database.Implementations;
using Engine.Rendering;
using Engine.Shaders;
using Engine.Shaders.Implementations;
using Engine.Systems;
using Newtonsoft.Json;

namespace Engine.Scene;

/// <summary>
/// Manages global environmental settings for the scene including skybox, ambient lighting, and fog.
/// Provides centralized access to environment-related rendering properties.
/// </summary>
public static class Environment
{
    #region Fields

    private static EnvironmentData _data;

    #endregion

    #region Skybox Properties

    /// <summary>
    /// Gets the OpenGL texture handle for the skybox cubemap.
    /// </summary>
    public static int SkyBoxTexture => _data.SkyboxShader.GetReflectionMap;


    private static SkyBox _skyBoxRenderer;

    #endregion

    #region Lighting Properties

    /// <summary>
    /// The ambient light color and intensity for the scene.
    /// Default is medium gray with full alpha.
    /// </summary>
    public static Vector4 AmbientColor
    {
        get => _data.AmbientColor;
        set => _data.AmbientColor = value;
    }

    #endregion

    public static void Initialize()
    {
        _skyBoxRenderer = new SkyBox();
        var deafuktData = new EnvironmentData();

        SetData(deafuktData);
        InitializeShader();
    }

    private static void InitializeShader()
    {
        var shaderDatabase = SystemManager.GetSystem<DatabaseSystem>().GetDatabase<ShaderDataBase>();
        _data.SkyboxShader = shaderDatabase.Get<DefaultSkyboxShader>();

        if (_data.SkyboxShader == null)
            throw new InvalidOperationException("Failed to load ToonSkyboxShader from database");

        _data.SkyboxShader.Use();
    }

    #region Fog Properties

    /// <summary>
    /// Gets or sets whether fog is enabled in the scene.
    /// </summary>
    public static bool UseFog
    {
        get => _data.UseFog;
        set => _data.UseFog = value;
    }

    /// <summary>
    /// Gets or sets the fog color.
    /// Default is medium gray with full alpha.
    /// </summary>
    public static Vector4 FogColor
    {
        get => _data.FogColor;
        set => _data.FogColor = value;
    }

    /// <summary>
    /// Gets or sets the distance at which fog starts to appear.
    /// </summary>
    public static float FogStart
    {
        get => _data.FogStart;
        set => _data.FogStart = value;
    }

    /// <summary>
    /// Gets or sets the distance at which fog reaches maximum density.
    /// </summary>
    public static float FogEnd
    {
        get => _data.FogEnd;
        set => _data.FogEnd = value;
    }

    #endregion

    #region Rendering

    /// <summary>
    /// Renders the skybox from the perspective of the given camera.
    /// </summary>
    /// <param name="camera">The camera to render the skybox from.</param>
    internal static void RenderSkybox(Camera camera)
    {
        if (_skyBoxRenderer == null)
        {
            Console.WriteLine("Warning: Skybox not initialized. Call Initialize() first.");
            return;
        }

        _skyBoxRenderer.PrepareRender();

        _data.SkyboxShader.Use();
        _data.SkyboxShader.UpdateProperties(camera);

        _skyBoxRenderer.RenderSkybox();
        _skyBoxRenderer.RestoreDepthState();
    }

    #endregion

    #region Shader Management

    /// <summary>
    /// Updates the skybox to use a different shader.
    /// </summary>
    /// <typeparam name="T">The type of shader to use (must inherit from ShaderBase).</typeparam>
    public static void UpdateSkyboxShader<T>(T newShader) where T : ShaderBase
    {
        _data.SkyboxShader = newShader;
    }

    #endregion

    #region Data Management

    /// <summary>
    /// Gets a copy of the current environment data.
    /// </summary>
    /// <returns>A new EnvironmentData instance containing current environment settings.</returns>
    public static string GetSaveData()
    {
        return JsonConvert.SerializeObject(_data, CreateSerializationSettings());
    }

    private static JsonSerializerSettings CreateSerializationSettings()
    {
        return new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
    }

    /// <summary>
    /// Updates the environment with new data.
    /// </summary>
    /// <param name="newData">The new environment data to apply.</param>
    public static void SetData(EnvironmentData newData)
    {
        _data = newData;
        //_data.SkyBox.UpdateShader(newData.SkyBox.SkyboxShader);
    }

    /// <summary>
    /// Gets a reference to the current environment data (direct access, not a copy).
    /// Use with caution as modifications will affect the environment directly.
    /// </summary>
    /// <returns>Reference to the internal EnvironmentData.</returns>
    public static EnvironmentData GetDataReference()
    {
        return _data;
    }

    #endregion


    #region Dispose

    public static void Dispose()
    {
        if (_data == null) return;
        
        _data.SkyboxShader?.Dispose();
        _data.SkyboxShader = null;
    }

    #endregion
}