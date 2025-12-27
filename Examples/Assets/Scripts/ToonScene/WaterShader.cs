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
using Project.Assets.Scripts.ToonScene;

namespace Engine.Scripts;

/// <summary>
/// Water shader with depth-based coloring, foam, reflections, and animated surface distortion
/// </summary>
public class WaterShader : ShaderBase
{
    #region Constants

    private const string DEFAULT_VERTEX_SHADER = "water.vert";
    private const string DEFAULT_FRAGMENT_SHADER = "water.frag";

    public float TIME_SCALE = 3.0f;

    // Texture Units
    private const int DEPTH_TEXTURE_UNIT = 0;
    private const int NORMAL_TEXTURE_UNIT = 1;
    private const int NOISE_TEXTURE_UNIT = 2;
    private const int DISTORTION_TEXTURE_UNIT = 3;
    private const int REFLECTION_TEXTURE_UNIT = 4;

    // Uniform Names
    private const string UNIFORM_MODEL = "uModel";
    private const string UNIFORM_VIEW = "uView";
    private const string UNIFORM_PROJ = "uProj";
    private const string UNIFORM_SCREEN_SIZE = "uScreenSize";
    private const string UNIFORM_PLANE_HEIGHT = "uPlaneHeight";
    private const string UNIFORM_CAMERA_POS = "uCameraPos";
    private const string UNIFORM_LIGHT_DIR = "uLightDir";
    private const string UNIFORM_LIGHT_COLOR = "uLightColor";
    private const string UNIFORM_DEPTH_TEX = "uDepthTex";
    private const string UNIFORM_NORMAL_TEX = "uNormalTex";
    private const string UNIFORM_NOISE_TEX = "uNoiseTex";
    private const string UNIFORM_DISTORTION_TEX = "uDistortionTex";
    private const string UNIFORM_REFLECTION_TEX = "uReflectionTex";
    private const string UNIFORM_DEPTH_GRADIENT_SHALLOW = "uDepthGradientShallow";
    private const string UNIFORM_DEPTH_GRADIENT_DEEP = "uDepthGradientDeep";
    private const string UNIFORM_FOAM_COLOR = "uFoamColor";
    private const string UNIFORM_DEPTH_MAX_DISTANCE = "uDepthMaxDistance";
    private const string UNIFORM_FOAM_MAX_DISTANCE = "uFoamMaxDistance";
    private const string UNIFORM_FOAM_MIN_DISTANCE = "uFoamMinDistance";
    private const string UNIFORM_SURFACE_NOISE_CUTOFF = "uSurfaceNoiseCutoff";
    private const string UNIFORM_SURFACE_DISTORTION_AMOUNT = "uSurfaceDistortionAmount";
    private const string UNIFORM_REFLECTION_STRENGTH = "uReflectionStrength";
    private const string UNIFORM_SURFACE_NOISE_SCROLL = "uSurfaceNoiseScroll";
    private const string UNIFORM_TIME = "uTime";

    #endregion

    #region Properties

    // Textures
    public Texture SurfaceNoise { get; set; }
    public Texture SurfaceDistortion { get; set; }

    // Color Properties
    public System.Numerics.Vector4 DepthGradientShallow { get; set; } = new(0.325f, 0.807f, 0.971f, 0.725f);
    public System.Numerics.Vector4 DepthGradientDeep { get; set; } = new(0.086f, 0.407f, 1.0f, 0.749f);
    public System.Numerics.Vector4 FoamColor { get; set; } = System.Numerics.Vector4.One;

    // Water Properties
    public float PlaneHeight
    {
        get => _planeHeight;
        set
        {
            _planeHeight = value;
            if (_planarReflection != null) _planarReflection.PlaneHeight = value;
        }
    }

    public float DepthMaxDistance
    {
        get => _depthMaxDistance;
        set => _depthMaxDistance = Math.Max(0f, value);
    }

    public float FoamMaxDistance
    {
        get => _foamMaxDistance;
        set => _foamMaxDistance = Math.Max(0f, value);
    }

    public float FoamMinDistance
    {
        get => _foamMinDistance;
        set => _foamMinDistance = Math.Max(0f, value);
    }

    public float SurfaceNoiseCutoff
    {
        get => _surfaceNoiseCutoff;
        set => _surfaceNoiseCutoff = Math.Clamp(value, 0f, 20f);
    }

    public float SurfaceDistortionAmount
    {
        get => _surfaceDistortionAmount;
        set => _surfaceDistortionAmount = Math.Max(0f, value);
    }

    public float ReflectionStrength
    {
        get => _reflectionStrength;
        set => _reflectionStrength = Math.Clamp(value, 0f, 1f);
    }

    public System.Numerics.Vector2 SurfaceNoiseScroll { get; set; } = new(3.3f, 3.3f);

    // Shader Properties
    protected override string VertexShaderName { get; }
    protected override string FragmentShaderName { get; }
    protected override string GeometryShaderName { get; }

    #endregion

    #region Fields

    private float _planeHeight;
    private float _depthMaxDistance = 1.0f;
    private float _foamMaxDistance = 0.4f;
    private float _foamMinDistance = 0.04f;
    private float _surfaceNoiseCutoff = 0.777f;
    private float _surfaceDistortionAmount = 0.27f;
    private float _reflectionStrength = 0.5f;

    private PlanarReflection _planarReflection;
    private SceneSystem _sceneSystem;
    private RenderSystem _renderSystem;

    #endregion

    #region Constructors

    public WaterShader() : this(DEFAULT_VERTEX_SHADER, DEFAULT_FRAGMENT_SHADER, null)
    {
        RenderingMode = RenderingMode.TRANSPARENT;
        Initialize();
    }

    public WaterShader(string vertexName, string fragmentName, string geometryName = null)
        : base(vertexName, fragmentName, geometryName)
    {
    }

    #endregion

    #region Initialization

    private void Initialize()
    {
        CacheSystems();
        InitializePlanarReflection();
        RegisterEventHandlers();
    }

    private void CacheSystems()
    {
        _sceneSystem = SystemManager.GetSystem<SceneSystem>();
        _renderSystem = SystemManager.GetSystem<RenderSystem>();

        if (_sceneSystem == null) throw new InvalidOperationException("SceneSystem not found in SystemManager");

        if (_renderSystem == null) throw new InvalidOperationException("RenderSystem not found in SystemManager");
    }

    private void InitializePlanarReflection()
    {
        _planarReflection = new PlanarReflection
        {
            PlaneHeight = PlaneHeight
        };
    }

    private void RegisterEventHandlers()
    {
        _renderSystem.PreRenderPass += OnPreRenderPass;
    }

    #endregion

    #region Serialization

    [OnDeserialized]
    internal void OnDeserializedMethod(StreamingContext context)
    {
        LoadShader(DEFAULT_VERTEX_SHADER, DEFAULT_FRAGMENT_SHADER);
        LoadTexturesAfterDeserialization();
        Initialize();
    }

    private void LoadTexturesAfterDeserialization()
    {
        var textureDatabase = GetTextureDatabase();

        if (SurfaceNoise != null && !string.IsNullOrEmpty(SurfaceNoise.Path))
            SurfaceNoise = textureDatabase.LoadTexture(SurfaceNoise.Path);

        if (SurfaceDistortion != null && !string.IsNullOrEmpty(SurfaceDistortion.Path))
            SurfaceDistortion = textureDatabase.LoadTexture(SurfaceDistortion.Path);
    }

    private TextureDataBase GetTextureDatabase()
    {
        var databaseSystem = SystemManager.GetSystem<DatabaseSystem>();
        if (databaseSystem == null) throw new InvalidOperationException("DatabaseSystem not found");

        return databaseSystem.GetDatabase<TextureDataBase>();
    }

    #endregion

    #region Event Handlers

    private void OnPreRenderPass()
    {
        _planarReflection?.Render();
    }

    #endregion

    #region Shader Updates

    public override void UpdateProperties(Camera camera, Matrix4 transform, int pass = 0)
    {
        base.UpdateProperties(camera, transform, pass);

        ValidateRenderState();

        UpdateTransformUniforms(transform);
        UpdateScreenUniforms();
        UpdateCameraAndLightUniforms();
        UpdateTextureBindings();
        UpdateWaterUniforms();
    }

    private void ValidateRenderState()
    {
        if (_sceneSystem?.CurrentScene == null) throw new InvalidOperationException("No active scene available");

        if (_sceneSystem.CurrentScene.Camera == null)
            throw new InvalidOperationException("No active camera in current scene");
    }

    #endregion

    #region Transform Uniforms

    private void UpdateTransformUniforms(Matrix4 transform)
    {
        SetMatrix4(UNIFORM_MODEL, transform);
        SetMatrix4(UNIFORM_VIEW, _sceneSystem.CurrentScene.CameraViewMatrix);
        SetMatrix4(UNIFORM_PROJ, _sceneSystem.CurrentScene.CameraProjectionMatrix);
    }

    #endregion

    #region Screen Uniforms

    private void UpdateScreenUniforms()
    {
        var viewportSize = Screen.GetViewportSize();
        SetVector2(UNIFORM_SCREEN_SIZE, new Vector2(viewportSize.X, viewportSize.Y));
        SetFloat(UNIFORM_PLANE_HEIGHT, PlaneHeight);
    }

    #endregion

    #region Camera & Light Uniforms

    private void UpdateCameraAndLightUniforms()
    {
        var camera = _sceneSystem.CurrentScene.Camera;
        var lights = _sceneSystem.CurrentScene.GeSceneLights;

        SetVector3(UNIFORM_CAMERA_POS, camera.Transform.WorldPosition);

        if (lights != null && lights.Count > 0)
        {
            var mainLight = lights[0];
            SetVector3(UNIFORM_LIGHT_DIR, mainLight.Transform.Forward);
            SetVector3(UNIFORM_LIGHT_COLOR, mainLight.GetRGBColor());
        }
        else
        {
            // Default lighting
            SetVector3(UNIFORM_LIGHT_DIR, Vector3.UnitY);
            SetVector3(UNIFORM_LIGHT_COLOR, Vector3.One);
        }
    }

    #endregion

    #region Texture Binding

    private void UpdateTextureBindings()
    {
        BindFrameBufferTextures();
        BindWaterTextures();
        BindReflectionTexture();
    }

    private void BindFrameBufferTextures()
    {
        var frameBuffer = _renderSystem.FrameBuffer;
        if (frameBuffer != null)
        {
            SetTexture(UNIFORM_DEPTH_TEX, frameBuffer.DepthTexture, DEPTH_TEXTURE_UNIT);
            SetTexture(UNIFORM_NORMAL_TEX, frameBuffer.NormalTexture, NORMAL_TEXTURE_UNIT);
        }
    }

    private void BindWaterTextures()
    {
        if (SurfaceNoise != null) SetTexture(UNIFORM_NOISE_TEX, SurfaceNoise.Handle, NOISE_TEXTURE_UNIT);

        if (SurfaceDistortion != null)
            SetTexture(UNIFORM_DISTORTION_TEX, SurfaceDistortion.Handle, DISTORTION_TEXTURE_UNIT);
    }

    private void BindReflectionTexture()
    {
        if (_planarReflection != null)
            SetTexture(UNIFORM_REFLECTION_TEX, _planarReflection.GetReflectionTextureId(), REFLECTION_TEXTURE_UNIT);
    }

    #endregion

    #region Water Uniforms

    private void UpdateWaterUniforms()
    {
        UpdateColorUniforms();
        UpdateDistanceUniforms();
        UpdateSurfaceUniforms();
        UpdateTimeUniform();
    }

    private void UpdateColorUniforms()
    {
        SetVector4(UNIFORM_DEPTH_GRADIENT_SHALLOW, DepthGradientShallow.ToOpenTK());
        SetVector4(UNIFORM_DEPTH_GRADIENT_DEEP, DepthGradientDeep.ToOpenTK());
        SetVector4(UNIFORM_FOAM_COLOR, FoamColor.ToOpenTK());
    }

    private void UpdateDistanceUniforms()
    {
        SetFloat(UNIFORM_DEPTH_MAX_DISTANCE, DepthMaxDistance);
        SetFloat(UNIFORM_FOAM_MAX_DISTANCE, FoamMaxDistance);
        SetFloat(UNIFORM_FOAM_MIN_DISTANCE, FoamMinDistance);
    }

    private void UpdateSurfaceUniforms()
    {
        SetFloat(UNIFORM_SURFACE_NOISE_CUTOFF, SurfaceNoiseCutoff);
        SetFloat(UNIFORM_SURFACE_DISTORTION_AMOUNT, SurfaceDistortionAmount);
        SetFloat(UNIFORM_REFLECTION_STRENGTH, ReflectionStrength);
        SetVector2(UNIFORM_SURFACE_NOISE_SCROLL, SurfaceNoiseScroll.ToOpenTK());
    }

    private void UpdateTimeUniform()
    {
        SetFloat(UNIFORM_TIME, (float)GLFW.GetTime() * TIME_SCALE);
    }

    #endregion

    #region Dispose

    protected override void CleanupDerivedResources()
    {
        base.CleanupDerivedResources();

        SurfaceNoise?.Dispose();
        SurfaceNoise = null;

        SurfaceDistortion?.Dispose();
        SurfaceDistortion = null;

        _planarReflection.Dispose();
    }

    #endregion
}