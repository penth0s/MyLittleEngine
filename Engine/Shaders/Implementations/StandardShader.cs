using System.Runtime.Serialization;
using Adapters;
using Engine.Components;
using Engine.Core;
using Engine.Database.Implementations;
using Engine.Rendering;
using Engine.Systems;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Environment = Engine.Scene.Environment;
using RenderingMode = Engine.Rendering.RenderingMode;

namespace Engine.Shaders.Implementations;

/// <summary>
/// Standard PBR shader with support for albedo, metallic, smoothness, shadows, skybox reflections, and tiling
/// </summary>
public sealed class StandardShader : ShaderBase
{
    #region Constants

    private const string DEFAULT_VERTEX_SHADER = "shader.vert";
    private const string DEFAULT_FRAGMENT_SHADER = "shader.frag";
    private const string DEFAULT_GEOMETRY_SHADER = "shader.geom";

    // Texture Unit Offsets
    private const int ALBEDO_TEXTURE_UNIT = 0;

    // Uniform Names - Material
    private const string UNIFORM_ALBEDO = "albedo";
    private const string UNIFORM_MATERIAL_HAS_ALBEDO = "material.hasAlbedoMap";
    private const string UNIFORM_MATERIAL_ALBEDO_COLOR = "material.albedoColor";
    private const string UNIFORM_MATERIAL_DIFFUSE = "material.diffuse";
    private const string UNIFORM_MATERIAL_METALLIC = "material.metallic";
    private const string UNIFORM_MATERIAL_SMOOTHNESS = "material.smoothness";
    private const string UNIFORM_MATERIAL_RENDERING_MODE = "material.RenderingMode";
    private const string UNIFORM_MATERIAL_ALPHA_CUTOFF = "material.alphaCutoff";
    private const string UNIFORM_MATERIAL_TILE = "material.tile";

    // Uniform Names - Scene
    private const string UNIFORM_TRANSFORM = "transform";
    private const string UNIFORM_VIEW = "view";
    private const string UNIFORM_PROJECTION = "projection";
    private const string UNIFORM_VIEW_POS = "viewPos";
    private const string UNIFORM_AMBIENT_COLOR = "ambientColor";
    private const string UNIFORM_DIRECTIONAL_COUNT = "directionalCount";
    private const string UNIFORM_SHADOW_MAP_COUNT = "shadowMapCount";

    // Uniform Names - Effects
    private const string UNIFORM_SHADOW_MAPS = "shadowMaps";
    private const string UNIFORM_SKYBOX_CUBEMAP = "skyboxCubemap";
    private const string UNIFORM_USE_SKYBOX = "useSkybox";
    private const string UNIFORM_USE_WIREFRAME = "useWireframe";

    #endregion

    #region Properties

    // Textures
    public Texture AlbedoMap { get; set; }

    // Material Properties
    public System.Numerics.Vector4 AlbedoColor { get; set; } = new(1, 1, 1, 1);

    /// <summary>
    /// Texture tiling factors for U and V coordinates. Default is (1, 1) for no tiling.
    /// </summary>
    public System.Numerics.Vector2 Tile { get; set; } = new(1f, 1f);

    public float Metallic
    {
        get => _metallic;
        set => _metallic = Math.Clamp(value, 0f, 1f);
    }

    public float Smoothness
    {
        get => _smoothness;
        set => _smoothness = Math.Clamp(value, 0f, 1f);
    }

    public float CutOff
    {
        get => _cutOff;
        set => _cutOff = Math.Clamp(value, 0f, 1f);
    }

    public float Diffuse
    {
        get => _diffuse;
        set => _diffuse = Math.Max(0f, value);
    }

    #endregion

    #region Fields

    private float _metallic;
    private float _smoothness = 0.5f;
    private float _cutOff = 0.5f;
    private float _diffuse = 1f;

    private SceneSystem _sceneSystem;

    #endregion

    #region Shader Properties (Base Class)
    
    protected override string VertexShaderName { get; } = DEFAULT_VERTEX_SHADER;
    protected override string FragmentShaderName { get; } = DEFAULT_FRAGMENT_SHADER;
    protected override string GeometryShaderName { get; } = DEFAULT_GEOMETRY_SHADER;

    #endregion

    #region Constructors

    public StandardShader() : this(DEFAULT_VERTEX_SHADER, DEFAULT_FRAGMENT_SHADER, DEFAULT_GEOMETRY_SHADER)
    {
        RenderingMode = RenderingMode.OPAQUE;
    }

    public StandardShader(string vertexName, string fragmentName, string geometryName = null)
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
        LoadShader(DEFAULT_VERTEX_SHADER, DEFAULT_FRAGMENT_SHADER, DEFAULT_GEOMETRY_SHADER);
        LoadTexturesAfterDeserialization();
        CacheSystems();
    }

    private void LoadTexturesAfterDeserialization()
    {
        var textureDatabase = GetTextureDatabase();

        if (AlbedoMap != null && !string.IsNullOrEmpty(AlbedoMap.Path))
            AlbedoMap = textureDatabase.LoadTexture(AlbedoMap.Path);
    }

    private TextureDataBase GetTextureDatabase()
    {
        var databaseSystem = SystemManager.GetSystem<DatabaseSystem>();
        if (databaseSystem == null) throw new InvalidOperationException("DatabaseSystem not found");

        return databaseSystem.GetDatabase<TextureDataBase>();
    }

    #endregion

    #region Assimp Integration

    public override void UpdateAssimpProperties(Assimp.Material assimpMaterial)
    {
        base.UpdateAssimpProperties(assimpMaterial);

        if (assimpMaterial?.HasColorDiffuse == true) AlbedoColor = assimpMaterial.ColorDiffuse;
    }

    #endregion

    #region Shader Updates

    public override void UpdateProperties(Camera camera, Matrix4 transform, int pass = 0)
    {
        base.UpdateProperties(camera, transform, pass);

        ValidateRenderState(camera);

        UpdateMaterialUniforms();
        UpdateTransformUniforms(camera, transform);
        UpdateSceneUniforms();
        UpdateTextureBindings();
        UpdateLightProperties();
    }

    private void ValidateRenderState(Camera camera)
    {
        if (camera == null) throw new ArgumentNullException(nameof(camera));

        if (_sceneSystem?.CurrentScene == null) throw new InvalidOperationException("No active scene available");
    }

    #endregion

    #region Material Uniforms

    private void UpdateMaterialUniforms()
    {
        var hasAlbedoMap = AlbedoMap != null;

        SetBool(UNIFORM_MATERIAL_HAS_ALBEDO, hasAlbedoMap);
        SetVector4(UNIFORM_MATERIAL_ALBEDO_COLOR, AlbedoColor.ToOpenTK());
        SetFloat(UNIFORM_MATERIAL_DIFFUSE, Diffuse);
        SetFloat(UNIFORM_MATERIAL_METALLIC, Metallic);
        SetFloat(UNIFORM_MATERIAL_SMOOTHNESS, Smoothness);
        SetInt(UNIFORM_MATERIAL_RENDERING_MODE, (int)RenderingMode);
        SetFloat(UNIFORM_MATERIAL_ALPHA_CUTOFF, CutOff);
        SetVector2(UNIFORM_MATERIAL_TILE, Tile.ToOpenTK());
    }

    #endregion

    #region Transform & Camera Uniforms

    private void UpdateTransformUniforms(Camera camera, Matrix4 transform)
    {
        SetMatrix4(UNIFORM_TRANSFORM, transform);
        SetMatrix4(UNIFORM_VIEW, camera.GetViewMatrix());
        SetMatrix4(UNIFORM_PROJECTION, camera.GetProjectionMatrix());
        SetVector3(UNIFORM_VIEW_POS, camera.Transform.WorldPosition);
    }

    #endregion

    #region Scene Uniforms

    private void UpdateSceneUniforms()
    {
        var scene = _sceneSystem.CurrentScene;

        SetVector3(UNIFORM_AMBIENT_COLOR, Environment.AmbientColor.ToOpenTK().ToRGB());
        SetInt(UNIFORM_DIRECTIONAL_COUNT, scene.GeSceneLights.Count);
        SetBool(UNIFORM_USE_WIREFRAME, Screen.IsWireFrameActive());
    }

    #endregion

    #region Texture Binding

    private void UpdateTextureBindings()
    {
        var nextAvailableUnit = ALBEDO_TEXTURE_UNIT;

        // 1. Albedo Map
        nextAvailableUnit = BindAlbedoTexture(nextAvailableUnit);

        // 2. Shadow Maps
        nextAvailableUnit = BindShadowMaps(nextAvailableUnit);

        // 3. Skybox
        BindSkybox(nextAvailableUnit);
    }

    private int BindAlbedoTexture(int textureUnit)
    {
        AlbedoMap?.Use(TextureUnit.Texture0 + textureUnit);
        SetInt(UNIFORM_ALBEDO, textureUnit);
        return textureUnit + 1;
    }

    private int BindShadowMaps(int startingUnit)
    {
        var shadowTextures = _sceneSystem.CurrentScene.GetShadowTextureIDs;

        SetShadowMapArray(UNIFORM_SHADOW_MAPS, shadowTextures, startingUnit);
        SetInt(UNIFORM_SHADOW_MAP_COUNT, shadowTextures.Count);

        return startingUnit + shadowTextures.Count;
    }

    private int BindSkybox(int textureUnit)
    {
        var scene = _sceneSystem.CurrentScene;

        var textureID = scene.IsSkybox ? Environment.SkyBoxTexture : 0;

        GL.ActiveTexture(TextureUnit.Texture0 + textureUnit);
        GL.BindTexture(TextureTarget.TextureCubeMap, textureID);

        SetInt(UNIFORM_SKYBOX_CUBEMAP, textureUnit);
        SetBool(UNIFORM_USE_SKYBOX, scene.IsSkybox);

        return textureUnit + 1;
    }

    #endregion

    #region Dispose

    protected override void CleanupDerivedResources()
    {
        base.CleanupDerivedResources();

        AlbedoMap?.Dispose();
        AlbedoMap = null;
    }

    #endregion
}