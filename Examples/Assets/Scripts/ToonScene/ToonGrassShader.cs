using System;
using System.Runtime.Serialization;
using Adapters;
using Engine.Components;
using Engine.Database.Implementations;
using Engine.Rendering;
using Engine.Shaders;
using Engine.Systems;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Environment = Engine.Scene.Environment;

namespace Engine.Scripts;

/// <summary>
/// Toon/cel-shaded grass shader with wind animation and color blending
/// </summary>
public class ToonGrassShader : ShaderBase
{
    #region Constants

    private const string DEFAULT_VERTEX_SHADER = "toon_grass.vert";
    private const string DEFAULT_FRAGMENT_SHADER = "toon_grass.frag";

    // Default Wind Parameters
    private const float DEFAULT_WIND_SCROLL = 0.1f;
    private const float DEFAULT_WIND_JITTER = 0.15f;

    // Uniform Names
    private const string UNIFORM_MODEL = "uModel";
    private const string UNIFORM_VIEW = "uView";
    private const string UNIFORM_PROJECTION = "uProjection";
    private const string UNIFORM_TIME = "uTime";
    private const string UNIFORM_WIND_SCROLL = "uWindScroll";
    private const string UNIFORM_WIND_JITTER = "uWindJitter";
    private const string UNIFORM_COLOR1 = "uColor1";
    private const string UNIFORM_COLOR2 = "uColor2";
    private const string UNIFORM_COLOR1_LEVEL = "uColor1Level";
    private const string UNIFORM_CUTOFF = "uCutoff";
    private const string UNIFORM_LIGHT_DIR = "uLightDir";
    private const string UNIFORM_LIGHT_COLOR = "uLightColor";
    private const string UNIFORM_AMBIENT_COLOR = "uAmbientColor";
    private const string UNIFORM_USE_FOG = "uUseFog";
    private const string UNIFORM_FOG_COLOR = "uFogColor";
    private const string UNIFORM_FOG_START = "uFogStart";
    private const string UNIFORM_FOG_END = "uFogEnd";
    private const string UNIFORM_VIEW_POS = "uViewPos";
    private const string UNIFORM_MAIN_TEX = "uMainTex";
    private const string UNIFORM_WIND_NOISE = "uWindNoise";

    #endregion

    #region Properties

    // Textures
    public Texture AlbedoMap { get; set; }
    public Texture NoiseMap { get; set; }

    // Color Properties
    public System.Numerics.Vector4 Color1 { get; set; } = new(0, 0.5f, 0, 1);
    public System.Numerics.Vector4 Color2 { get; set; } = new(0.3f, 0.8f, 0.3f, 1);

    public float Color1Level
    {
        get => _color1Level;
        set => _color1Level = Math.Clamp(value, 0f, 1f);
    }

    public float Cutoff
    {
        get => _cutoff;
        set => _cutoff = Math.Clamp(value, 0f, 1f);
    }

    // Shader Properties
    protected override string VertexShaderName { get; } = DEFAULT_VERTEX_SHADER;
    protected override string FragmentShaderName { get; } = DEFAULT_FRAGMENT_SHADER;
    protected override string GeometryShaderName { get; } = null;

    #endregion

    #region Fields

    private float _color1Level = 0.35f;
    private float _cutoff = 0.5f;

    private SceneSystem _sceneSystem;

    #endregion

    #region Constructors

    public ToonGrassShader() : this(DEFAULT_VERTEX_SHADER, DEFAULT_FRAGMENT_SHADER)
    {
        RenderingMode = RenderingMode.OPAQUE;
    }

    public ToonGrassShader(string vertexName, string fragmentName, string geometryName = null)
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
        UpdateColorUniforms();
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
        SetMatrix4(UNIFORM_PROJECTION, camera.GetProjectionMatrix());
    }

    #endregion

    #region Wind Uniforms

    private void UpdateWindUniforms()
    {
        SetFloat(UNIFORM_TIME, (float)GLFW.GetTime());
        SetFloat(UNIFORM_WIND_SCROLL, DEFAULT_WIND_SCROLL);
        SetFloat(UNIFORM_WIND_JITTER, DEFAULT_WIND_JITTER);
    }

    #endregion

    #region Color Uniforms

    private void UpdateColorUniforms()
    {
        SetVector4(UNIFORM_COLOR1, ConvertToVector4(Color1));
        SetVector4(UNIFORM_COLOR2, ConvertToVector4(Color2));
        SetFloat(UNIFORM_COLOR1_LEVEL, Color1Level);
        SetFloat(UNIFORM_CUTOFF, Cutoff);
    }

    private Vector4 ConvertToVector4(System.Numerics.Vector4 color)
    {
        return new Vector4(color.X, color.Y, color.Z, 1.0f);
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
            SetVector3(UNIFORM_AMBIENT_COLOR, Vector3.One * 0.3f);
            return;
        }

        var mainLight = lights[0];
        SetVector3(UNIFORM_LIGHT_DIR, mainLight.Transform.Forward);
        SetVector3(UNIFORM_LIGHT_COLOR, mainLight.GetRGBColor());
        SetVector3(UNIFORM_AMBIENT_COLOR, Environment.AmbientColor.ToOpenTK().ToRGB());
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

        if (AlbedoMap != null)
        {
            SetTexture(UNIFORM_MAIN_TEX, AlbedoMap.Handle, textureUnit);
            textureUnit++;
        }

        if (NoiseMap != null) SetTexture(UNIFORM_WIND_NOISE, NoiseMap.Handle, textureUnit);
    }

    #endregion

    #region Dispose

    protected override void CleanupDerivedResources()
    {
        base.CleanupDerivedResources();

        AlbedoMap?.Dispose();
        AlbedoMap = null;

        NoiseMap?.Dispose();
        NoiseMap = null;
    }

    #endregion
}