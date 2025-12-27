using System;
using System.Runtime.Serialization;
using Adapters;
using Engine.Components;
using Engine.Database.Implementations;
using Engine.Rendering;
using Engine.Shaders;
using Engine.Systems;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Environment = Engine.Scene.Environment;
using RenderingMode = Engine.Rendering.RenderingMode;

namespace Engine.Scripts;

/// <summary>
/// Toon/cel-shaded environment shader with outline support
/// </summary>
public class ToonEnvironmentShader : ShaderBase
{
    #region Constants

    private const string DEFAULT_VERTEX_SHADER = "toon_environment.vert";
    private const string DEFAULT_FRAGMENT_SHADER = "toon_environment.frag";

    private const int PASS_OUTLINE = 0;
    private const int PASS_MAIN = 1;
    private const int TOTAL_PASSES = 2;

    // Uniform Names
    private const string UNIFORM_MODEL = "uModel";
    private const string UNIFORM_VIEW = "uView";
    private const string UNIFORM_PROJ = "uProj";
    private const string UNIFORM_VIEW_POS = "uViewPos";
    private const string UNIFORM_USE_FOG = "uUseFog";
    private const string UNIFORM_FOG_COLOR = "uFogColor";
    private const string UNIFORM_FOG_START = "uFogStart";
    private const string UNIFORM_FOG_END = "uFogEnd";
    private const string UNIFORM_TEXTURE_SAMPLE = "uTextureSample";
    private const string UNIFORM_TEXTURE_RAMP = "uTextureRamp";
    private const string UNIFORM_RAMP_SCALE = "rampScale";
    private const string UNIFORM_OUTLINE_WIDTH = "uOutlineWidth";
    private const string UNIFORM_OUTLINE_COLOR = "uOutlineColor";
    private const string UNIFORM_USE_OUTLINE = "uUseOutline";
    private const string UNIFORM_LIGHT_DIR = "uLightDir";
    private const string UNIFORM_LIGHT_COLOR = "uLightColor";

    #endregion

    #region Properties

    // Textures
    public Texture AlbedoMap { get; set; }
    public Texture RampMap { get; set; }

    // Outline Properties
    public bool UseOutline { get; set; }

    public float OutlineThickness
    {
        get => _outlineThickness;
        set => _outlineThickness = Math.Max(0f, value);
    }

    public System.Numerics.Vector3 OutlineColor { get; set; } = new(0, 0, 0);

    // Ramp Properties
    public float RampFactor
    {
        get => _rampFactor;
        set => _rampFactor = Math.Max(0f, value);
    }

    // Shader Properties
    public override int PassCount => TOTAL_PASSES;

    protected override string VertexShaderName { get; } = DEFAULT_VERTEX_SHADER;
    protected override string FragmentShaderName { get; } = DEFAULT_FRAGMENT_SHADER;
    protected override string GeometryShaderName { get; } = null;

    #endregion

    #region Fields

    private float _outlineThickness = 0.05f;
    private float _rampFactor = 1.0f;

    private SceneSystem _sceneSystem;

    #endregion

    #region Constructor

    public ToonEnvironmentShader() : base(DEFAULT_VERTEX_SHADER, DEFAULT_FRAGMENT_SHADER)
    {
        RenderingMode = RenderingMode.OPAQUE;
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
        UpdateFogUniforms();
        UpdateTextureBindings();
        UpdatePassSpecificSettings(pass);
        UpdateLightingUniforms();
    }

    private void ValidateRenderState(Camera camera)
    {
        if (camera == null) throw new ArgumentNullException(nameof(camera));

        if (_sceneSystem?.CurrentScene == null) throw new InvalidOperationException("No active scene available");
    }

    #endregion

    #region Transform & Camera Uniforms

    private void UpdateTransformUniforms(Camera camera, Matrix4 transform)
    {
        SetMatrix4(UNIFORM_MODEL, transform);
        SetMatrix4(UNIFORM_VIEW, camera.GetViewMatrix());
        SetMatrix4(UNIFORM_PROJ, camera.GetProjectionMatrix());
        SetVector3(UNIFORM_VIEW_POS, camera.GameObject.Transform.WorldPosition);
    }

    #endregion

    #region Fog Uniforms

    private void UpdateFogUniforms()
    {
        SetBool(UNIFORM_USE_FOG, Environment.UseFog);
        SetVector3(UNIFORM_FOG_COLOR, Environment.FogColor.ToOpenTK().ToRGB());
        SetFloat(UNIFORM_FOG_START, Environment.FogStart);
        SetFloat(UNIFORM_FOG_END, Environment.FogEnd);
    }

    #endregion

    #region Texture Binding

    private void UpdateTextureBindings()
    {
        var textureUnit = 0;

        if (AlbedoMap != null)
        {
            SetTexture(UNIFORM_TEXTURE_SAMPLE, AlbedoMap.Handle, textureUnit);
            textureUnit++;
        }

        if (RampMap != null)
        {
            SetTexture(UNIFORM_TEXTURE_RAMP, RampMap.Handle, textureUnit);
            SetFloat(UNIFORM_RAMP_SCALE, RampFactor);
        }
    }

    #endregion

    #region Pass-Specific Settings

    private void UpdatePassSpecificSettings(int pass)
    {
        switch (pass)
        {
            case PASS_OUTLINE when UseOutline:
                ConfigureOutlinePass();
                break;

            case PASS_MAIN:
                ConfigureMainPass();
                break;
        }
    }

    private void ConfigureOutlinePass()
    {
        SetFloat(UNIFORM_OUTLINE_WIDTH, OutlineThickness);
        SetVector3(UNIFORM_OUTLINE_COLOR, OutlineColor.ToOpenTK());
        SetBool(UNIFORM_USE_OUTLINE, true);

        EnableOutlineRenderState();
    }

    private void ConfigureMainPass()
    {
        SetBool(UNIFORM_USE_OUTLINE, false);
        DisableOutlineRenderState();
    }

    private void EnableOutlineRenderState()
    {
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Front);
    }

    private void DisableOutlineRenderState()
    {
        GL.Disable(EnableCap.CullFace);
    }

    #endregion

    #region Lighting Uniforms

    private void UpdateLightingUniforms()
    {
        var lights = _sceneSystem.CurrentScene.GeSceneLights;

        if (lights == null || lights.Count == 0)
        {
            // No lights available - use default lighting
            SetVector3(UNIFORM_LIGHT_DIR, Vector3.UnitY);
            SetVector3(UNIFORM_LIGHT_COLOR, Vector3.One);
            return;
        }

        var mainLight = lights[0];
        SetVector3(UNIFORM_LIGHT_DIR, mainLight.Transform.Forward);
        SetVector3(UNIFORM_LIGHT_COLOR, mainLight.GetRGBColor());
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
    }

    #endregion
}