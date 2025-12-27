using System;
using System.Runtime.Serialization;
using Adapters;
using Engine.Components;
using Engine.Core;
using Engine.Database.Implementations;
using Engine.Rendering;
using Engine.Shaders;
using Engine.Systems;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Environment = Engine.Scene.Environment;

namespace Engine.Scripts;

/// <summary>
/// Toon/cel-shaded tree shader with wind animation and ramp-based lighting
/// </summary>
public class ToonTreeShader : ShaderBase
{
    #region Constants

    private const string DEFAULT_VERTEX_SHADER = "toon_tree.vert";
    private const string DEFAULT_FRAGMENT_SHADER = "toon_tree.frag";

    private const float DEFAULT_CUTOFF = 0.5f;

    // Uniform Names
    private const string UNIFORM_MODEL = "uModel";
    private const string UNIFORM_VIEW = "uView";
    private const string UNIFORM_PROJ = "uProj";
    private const string UNIFORM_TIME = "uTime";
    private const string UNIFORM_WIND_SCROLL = "uWindScroll";
    private const string UNIFORM_WIND_JITTER = "uWindJitter";
    private const string UNIFORM_CUTOFF = "uCutoff";
    private const string UNIFORM_LIGHT_DIR = "uLightDir";
    private const string UNIFORM_LIGHT_COLOR = "uLightColor";
    private const string UNIFORM_USE_FOG = "uUseFog";
    private const string UNIFORM_FOG_COLOR = "uFogColor";
    private const string UNIFORM_FOG_START = "uFogStart";
    private const string UNIFORM_FOG_END = "uFogEnd";
    private const string UNIFORM_VIEW_POS = "uViewPos";
    private const string UNIFORM_TEXTURE_SAMPLE = "uTextureSample";
    private const string UNIFORM_TEXTURE_RAMP = "uTextureRamp";
    private const string UNIFORM_RAMP_SCALE = "rampScale";
    private const string UNIFORM_WIND_NOISE_TEXTURE = "uWindNoiseTexture";

    #endregion

    #region Properties

    // Textures
    public Texture AlbedoMap { get; set; }
    public Texture RampMap { get; set; }
    public Texture NoiseMap { get; set; }

    // Wind Properties
    public float WindScrool
    {
        get => _windScroll;
        set => _windScroll = Math.Max(0f, value);
    }

    public float WindJitter
    {
        get => _windJitter;
        set => _windJitter = Math.Max(0f, value);
    }

    // Ramp Properties
    private float RampFactor
    {
        get => _rampFactor;
        set => _rampFactor = Math.Max(0f, value);
    }

    // Shader Properties
    protected override string VertexShaderName { get; } = DEFAULT_VERTEX_SHADER;
    protected override string FragmentShaderName { get; } = DEFAULT_FRAGMENT_SHADER;
    protected override string GeometryShaderName { get; } = null;

    #endregion

    #region Fields

    private float _windScroll = 1.0f;
    private float _windJitter = 1.0f;
    private float _rampFactor = 1.0f;

    private SceneSystem _sceneSystem;

    #endregion

    #region Constructors

    public ToonTreeShader() : this(DEFAULT_VERTEX_SHADER, DEFAULT_FRAGMENT_SHADER)
    {
        RenderingMode = RenderingMode.OPAQUE;
    }

    public ToonTreeShader(string vertexName, string fragmentName, string geometryName = null)
        : base(vertexName, fragmentName, geometryName)
    {
        Initialize();
    }

    #endregion

    #region Initialization

    private void Initialize()
    {
        CacheSystems();
    }

    private void CacheSystems()
    {
        _sceneSystem = SystemManager.GetSystem<SceneSystem>();

        if (_sceneSystem == null) throw new InvalidOperationException("SceneSystem not found in SystemManager");
    }

    #endregion

    #region Serialization

    [OnDeserialized]
    internal void OnDeserializedMethod(StreamingContext context)
    {
        LoadShader(DEFAULT_VERTEX_SHADER, DEFAULT_FRAGMENT_SHADER);
        LoadTexturesAfterDeserialization();
        CacheSystems();
    }

    private void LoadTexturesAfterDeserialization()
    {
        var textureDatabase = GetTextureDatabase();

        if (AlbedoMap != null && !string.IsNullOrEmpty(AlbedoMap.Path))
            AlbedoMap = textureDatabase.LoadTexture(AlbedoMap.Path);

        if (RampMap != null && !string.IsNullOrEmpty(RampMap.Path)) RampMap = textureDatabase.LoadTexture(RampMap.Path);

        if (NoiseMap != null && !string.IsNullOrEmpty(NoiseMap.Path))
            NoiseMap = textureDatabase.LoadTexture(NoiseMap.Path);
    }

    private TextureDataBase GetTextureDatabase()
    {
        var databaseSystem = SystemManager.GetSystem<DatabaseSystem>();
        if (databaseSystem == null) throw new InvalidOperationException("DatabaseSystem not found");

        return databaseSystem.GetDatabase<TextureDataBase>();
    }

    #endregion

    #region Shader Updates

    public override void UpdateProperties(Camera camera, Matrix4 transform, int pass = 0)
    {
        base.UpdateProperties(camera, transform, pass);

        ValidateRenderState(camera);

        UpdateTransformUniforms(camera, transform);
        UpdateWindUniforms();
        UpdateLightingUniforms();
        UpdateFogUniforms(camera);
        UpdateTextureBindings();
    }

    private void ValidateRenderState(Camera camera)
    {
        if (camera == null) throw new ArgumentNullException(nameof(camera));

        if (_sceneSystem?.CurrentScene == null) throw new InvalidOperationException("No active scene available");
    }

    #endregion

    #region Transform Uniforms

    private void UpdateTransformUniforms(Camera camera, Matrix4 transform)
    {
        SetMatrix4(UNIFORM_MODEL, transform);
        SetMatrix4(UNIFORM_VIEW, camera.GetViewMatrix());
        SetMatrix4(UNIFORM_PROJ, camera.GetProjectionMatrix());
    }

    #endregion

    #region Wind Uniforms

    private void UpdateWindUniforms()
    {
        SetFloat(UNIFORM_TIME, Time.TotalTime);
        SetFloat(UNIFORM_WIND_SCROLL, WindScrool);
        SetFloat(UNIFORM_WIND_JITTER, WindJitter);
        SetFloat(UNIFORM_CUTOFF, DEFAULT_CUTOFF);
    }

    #endregion

    #region Lighting Uniforms

    private void UpdateLightingUniforms()
    {
        var lights = _sceneSystem.CurrentScene.GeSceneLights;

        if (lights == null || lights.Count == 0)
        {
            // Use default lighting
            SetVector3(UNIFORM_LIGHT_DIR, Vector3.UnitY);
            SetVector3(UNIFORM_LIGHT_COLOR, Vector3.One);
            return;
        }

        var mainLight = lights[0];
        SetVector3(UNIFORM_LIGHT_DIR, mainLight.Transform.Forward);
        SetVector3(UNIFORM_LIGHT_COLOR, mainLight.GetRGBColor());
    }

    #endregion

    #region Fog Uniforms

    private void UpdateFogUniforms(Camera camera)
    {
        SetBool(UNIFORM_USE_FOG, Environment.UseFog);
        SetVector3(UNIFORM_FOG_COLOR, Environment.FogColor.ToOpenTK().ToRGB());
        SetFloat(UNIFORM_FOG_START, Environment.FogStart);
        SetFloat(UNIFORM_FOG_END, Environment.FogEnd);
        SetVector3(UNIFORM_VIEW_POS, camera.GameObject.Transform.WorldPosition);
    }

    #endregion

    #region Texture Binding

    private void UpdateTextureBindings()
    {
        var textureUnit = 0;

        textureUnit = BindAlbedoTexture(textureUnit);
        textureUnit = BindRampTexture(textureUnit);
        BindNoiseTexture(textureUnit);
    }

    private int BindAlbedoTexture(int textureUnit)
    {
        if (AlbedoMap != null)
        {
            SetTexture(UNIFORM_TEXTURE_SAMPLE, AlbedoMap.Handle, textureUnit);
            return textureUnit + 1;
        }

        return textureUnit;
    }

    private int BindRampTexture(int textureUnit)
    {
        if (RampMap != null)
        {
            SetTexture(UNIFORM_TEXTURE_RAMP, RampMap.Handle, textureUnit);
            SetFloat(UNIFORM_RAMP_SCALE, RampFactor);
            return textureUnit + 1;
        }

        return textureUnit;
    }

    private void BindNoiseTexture(int textureUnit)
    {
        if (NoiseMap != null) SetTexture(UNIFORM_WIND_NOISE_TEXTURE, NoiseMap.Handle, textureUnit);
    }

    #endregion


    #region Dispose

    protected override void CleanupDerivedResources()
    {
        base.CleanupDerivedResources();

        AlbedoMap?.Dispose();
        AlbedoMap = null;

        RampMap?.Dispose();
        RampMap = null;

        NoiseMap?.Dispose();
        NoiseMap = null;
    }

    #endregion
}