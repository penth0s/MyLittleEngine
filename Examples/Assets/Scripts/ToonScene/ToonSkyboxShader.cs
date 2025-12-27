using System;
using System.Runtime.Serialization;
using Adapters;
using Engine.Components;
using Engine.Core;
using Engine.Database.Implementations;
using Engine.Rendering;
using Engine.Shaders;
using Engine.Systems;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Environment = Engine.Scene.Environment;

namespace Engine.Scripts;

/// <summary>
/// Shader for rendering skybox with dual cubemap blending, rotation, and fog effects
/// </summary>
public class ToonSkyboxShader : ShaderBase
{
    #region Constants

    private const string DEFAULT_VERTEX_SHADER = "toon_skybox.vert";
    private const string DEFAULT_FRAGMENT_SHADER = "toon_skybox.frag";

    private const string UNIFORM_VIEW = "view";
    private const string UNIFORM_PROJECTION = "projection";
    private const string UNIFORM_BLEND = "uBlend";
    private const string UNIFORM_EXPOSURE = "uExposure";
    private const string UNIFORM_TINT = "uTint";
    private const string UNIFORM_ROTATION = "uRotation";
    private const string UNIFORM_ROTATION_SPEED = "uRotationSpeed";
    private const string UNIFORM_TIME = "uTime";
    private const string UNIFORM_CUBEMAP_POSITION = "uCubemapPosition";
    private const string UNIFORM_ENABLE_ROTATION = "uEnableRotation";
    private const string UNIFORM_ENABLE_FOG = "uEnableFog";
    private const string UNIFORM_FOG_COLOR = "uFogColor";
    private const string UNIFORM_FOG_INTENSITY = "uFogIntensity";
    private const string UNIFORM_FOG_HEIGHT = "uFogHeight";
    private const string UNIFORM_FOG_SMOOTHNESS = "uFogSmoothness";
    private const string UNIFORM_FOG_FILL = "uFogFill";
    private const string UNIFORM_FOG_POSITION = "uFogPosition";
    private const string UNIFORM_CUBEMAP1 = "uCubemap1";
    private const string UNIFORM_CUBEMAP2 = "uCubemap2";

    #endregion

    #region Properties

    public override int GetReflectionMap => _cubeMapTexture1.Handle;

    // Blend Properties
    public float BlendValue
    {
        get => _blendValue;
        set => _blendValue = Math.Clamp(value, 0f, 1f);
    }

    public float Exposure
    {
        get => _exposure;
        set => _exposure = Math.Max(0f, value);
    }

    public Vector3 Tint { get; set; } = Vector3.One;

    // Rotation Properties
    public bool EnableRotation { get; set; } = true;

    public float Rotation { get; set; }

    public float RotationSpeed { get; set; } = 1.0f;

    public float CubemapPosition { get; set; }

    // Fog Properties
    public float FogIntensity
    {
        get => _fogIntensity;
        set => _fogIntensity = Math.Clamp(value, 0f, 1f);
    }

    public float FogHeight { get; set; } = 0.4f;

    public float FogSmoothness
    {
        get => _fogSmoothness;
        set => _fogSmoothness = Math.Max(0f, value);
    }

    public float FogFill
    {
        get => _fogFill;
        set => _fogFill = Math.Clamp(value, 0f, 1f);
    }

    public float FogPosition { get; set; }

    #endregion

    #region Fields

    private bool _isInitialized;
    private Texture _cubeMapTexture1;
    private Texture _cubeMapTexture2;

    private float _blendValue;
    private float _exposure = 1.0f;
    private float _fogIntensity = 0.5f;
    private float _fogSmoothness = 0.0f;
    private float _fogFill = 0.5f;

    #endregion

    #region Shader Properties (Base Class)

    protected override string VertexShaderName { get; }
    protected override string FragmentShaderName { get; }
    protected override string GeometryShaderName { get; }

    #endregion

    #region Constructors

    public ToonSkyboxShader() : this(DEFAULT_VERTEX_SHADER, DEFAULT_FRAGMENT_SHADER)
    {
    }

    public ToonSkyboxShader(string vertexName, string fragmentName) : base(vertexName, fragmentName)
    {
    }

    #endregion

    #region Initialization

    private void Initialize()
    {
        LoadTextures();
        ConfigureTextureUnits();
    }

    private void LoadTextures()
    {
        var textureDatabase = GetTextureDatabase();

        _cubeMapTexture1 = LoadCubemapTexture(textureDatabase, "Skybox1", "1");
        _cubeMapTexture2 = LoadCubemapTexture(textureDatabase, "Skybox2", "2");
    }

    private TextureDataBase GetTextureDatabase()
    {
        var databaseSystem = SystemManager.GetSystem<DatabaseSystem>();
        if (databaseSystem == null) throw new InvalidOperationException("DatabaseSystem not found");

        return databaseSystem.GetDatabase<TextureDataBase>();
    }

    private Texture LoadCubemapTexture(TextureDataBase textureDatabase, string folderName, string prefix)
    {
        var faces = new[]
        {
            $"{folderName}/{prefix}px.png", // Positive X (Right)
            $"{folderName}/{prefix}nx.png", // Negative X (Left)
            $"{folderName}/{prefix}py.png", // Positive Y (Top)
            $"{folderName}/{prefix}ny.png", // Negative Y (Bottom)
            $"{folderName}/{prefix}pz.png", // Positive Z (Front)
            $"{folderName}/{prefix}nz.png" // Negative Z (Back)
        };


        return textureDatabase.LoadSkyboxCubeMap(faces);
    }

    private void ConfigureTextureUnits()
    {
        Use();
        SetInt(UNIFORM_CUBEMAP1, 0);
        SetInt(UNIFORM_CUBEMAP2, 1);
    }

    #endregion

    #region Serialization

    [OnDeserialized]
    internal void OnDeserializedMethod(StreamingContext context)
    {
        LoadShader(DEFAULT_VERTEX_SHADER, DEFAULT_FRAGMENT_SHADER);
    }

    #endregion

    #region Shader Updates

    public override void UpdateProperties(Camera camera)
    {
        if (camera == null) throw new ArgumentNullException(nameof(camera));

        if (!_isInitialized)
        {
            Initialize();
            _isInitialized = true;
        }

        UpdateMatrices(camera);
        UpdateBlendProperties();
        UpdateRotationProperties();
        UpdateFogProperties();
        BindCubemapTextures();
    }

    private void UpdateMatrices(Camera camera)
    {
        // Remove translation from view matrix for skybox (always centered on camera)
        var view = new Matrix4(new Matrix3(camera.GetViewMatrix()));
        var projection = camera.GetProjectionMatrix();

        SetMatrix4(UNIFORM_VIEW, view);
        SetMatrix4(UNIFORM_PROJECTION, projection);
    }

    private void UpdateBlendProperties()
    {
        SetFloat(UNIFORM_BLEND, BlendValue);
        SetFloat(UNIFORM_EXPOSURE, Exposure);
        SetVector3(UNIFORM_TINT, Tint);
    }

    private void UpdateRotationProperties()
    {
        SetBool(UNIFORM_ENABLE_ROTATION, EnableRotation);
        SetFloat(UNIFORM_ROTATION, Rotation);
        SetFloat(UNIFORM_ROTATION_SPEED, RotationSpeed);
        SetFloat(UNIFORM_TIME, Time.TotalTime);
        SetFloat(UNIFORM_CUBEMAP_POSITION, CubemapPosition);
    }

    private void UpdateFogProperties()
    {
        SetBool(UNIFORM_ENABLE_FOG, Environment.UseFog);
        SetVector3(UNIFORM_FOG_COLOR, Environment.FogColor.ToOpenTK().ToRGB());
        SetFloat(UNIFORM_FOG_INTENSITY, FogIntensity);
        SetFloat(UNIFORM_FOG_HEIGHT, FogHeight);
        SetFloat(UNIFORM_FOG_SMOOTHNESS, FogSmoothness);
        SetFloat(UNIFORM_FOG_FILL, FogFill);
        SetFloat(UNIFORM_FOG_POSITION, FogPosition);
    }

    private void BindCubemapTextures()
    {
        // Bind first cubemap to texture unit 0
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.TextureCubeMap, _cubeMapTexture1.Handle);

        // Bind second cubemap to texture unit 1
        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.TextureCubeMap, _cubeMapTexture2.Handle);
    }

    #endregion

    #region Dispose

    protected override void CleanupDerivedResources()
    {
        base.CleanupDerivedResources();

        _cubeMapTexture1?.Dispose();
        _cubeMapTexture1 = null;

        _cubeMapTexture2?.Dispose();
        _cubeMapTexture2 = null;
    }

    #endregion
}