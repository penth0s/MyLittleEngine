using System.Runtime.Serialization;
using Engine.Components;
using Engine.Rendering;
using Engine.Shaders;
using Engine.Systems;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Project.Assets.Scripts.GrassScene;

/// <summary>
/// Shadow pass shader for GPU grass rendering
/// </summary>
public class GpuGrassShaderShadow : ShaderBase
{
    #region Constants

    private const string DEFAULT_VERTEX_SHADER = "gpu_grass_shadow.vert";
    private const string DEFAULT_GEOMETRY_SHADER = "gpu_grass_shadow.geom";
    private const string DEFAULT_FRAGMENT_SHADER = "gpu_grass_shadow.frag";

    // Uniform Names
    private const string UNIFORM_LIGHT_SPACE_MATRIX = "lightSpaceMatrix";
    private const string UNIFORM_LIGHT_DIRECTION = "lightDirection";
    private const string UNIFORM_GRASS_HEIGHT = "grassHeight";
    private const string UNIFORM_GRASS_WIDTH = "grassWidth";
    private const string UNIFORM_TIME = "time";
    private const string UNIFORM_WIND_STRENGTH = "windStrength";
    private const string UNIFORM_WIND_DIRECTION = "windDirection";
    private const string UNIFORM_WIND_SPEED = "windSpeed";
    private const string UNIFORM_NUM_INTERACTION_OBJECTS = "numInteractionObjects";
    private const string UNIFORM_INTERACTION_OBJECTS = "interactionObjects";
    private const string UNIFORM_INTERACTION_RADIUS = "interactionRadius";
    private const string UNIFORM_INTERACTION_STRENGTH = "interactionStrength";

    #endregion

    #region Properties

    // Grass Appearance Properties
    public float GrassHeight { get; set; } = 1.0f;
    public float GrassWidth { get; set; } = 0.1f;
    public float ShadowScaleFactor { get; set; } = 1.0f;

    // Wind Properties (should match main shader for consistent shadows)
    public float WindStrength { get; set; } = 0.25f;
    public Vector2 WindDirection { get; set; } = new(1.0f, 0.0f);
    public float WindSpeed { get; set; } = 4.5f;

    // Interaction Properties
    public float InteractionRadius { get; set; } = 1.0f;
    public float InteractionStrength { get; set; } = 5.5f;

    // Light Properties
    public Matrix4 LightSpaceMatrix { get; set; }
    public Vector3 LightDirection { get; set; }

    // Shader Properties
    protected override string VertexShaderName { get; } = DEFAULT_VERTEX_SHADER;
    protected override string FragmentShaderName { get; } = DEFAULT_FRAGMENT_SHADER;
    protected override string GeometryShaderName { get; } = DEFAULT_GEOMETRY_SHADER;

    #endregion

    #region Fields

    private SceneSystem _sceneSystem;
    private Vector3[] _interactionPositions;
    private int _interactionObjectCount;

    #endregion

    #region Constructors

    public GpuGrassShaderShadow() : this(DEFAULT_VERTEX_SHADER, DEFAULT_FRAGMENT_SHADER, DEFAULT_GEOMETRY_SHADER)
    {
        RenderingMode = RenderingMode.OPAQUE;
    }

    public GpuGrassShaderShadow(string vertexName, string fragmentName, string geometryName)
        : base(vertexName, fragmentName, geometryName)
    {
        Initialize();
    }

    #endregion

    #region Initialization

    private void Initialize()
    {
        CacheSystems();
        _interactionPositions = new Vector3[32]; // Max 32 interaction objects
        _interactionObjectCount = 0;
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
        CacheSystems();
        Initialize();
    }

    #endregion

    #region Interaction System

    /// <summary>
    /// Sets the interaction objects for grass displacement
    /// </summary>
    /// <param name="positions">Array of world positions that interact with grass</param>
    public void SetInteractionObjects(Vector3[] positions)
    {
        if (positions == null)
        {
            _interactionObjectCount = 0;
            return;
        }

        var count = Math.Min(positions.Length, 32); // Limit to 32
        for (var i = 0; i < count; i++) _interactionPositions[i] = positions[i];
        _interactionObjectCount = count;
    }

    /// <summary>
    /// Clears all interaction objects
    /// </summary>
    public void ClearInteractionObjects()
    {
        _interactionObjectCount = 0;
    }

    #endregion

    #region Shader Updates

    public override void UpdateProperties(Camera camera, Matrix4 transform, int pass = 0)
    {
        base.UpdateProperties(camera, transform, pass);

        ValidateRenderState();

        UpdateLightUniforms();
        UpdateGrassProperties();
        UpdateWindUniforms();
        UpdateInteractionUniforms();
    }

    private void ValidateRenderState()
    {
        if (_sceneSystem?.CurrentScene == null) throw new InvalidOperationException("No active scene available");
    }

    #endregion

    #region Light Uniforms

    private void UpdateLightUniforms()
    {
        SetMatrix4(UNIFORM_LIGHT_SPACE_MATRIX, LightSpaceMatrix);
        SetVector3(UNIFORM_LIGHT_DIRECTION, LightDirection);
    }

    #endregion

    #region Grass Properties

    private void UpdateGrassProperties()
    {
        // Apply shadow scale factor to grass dimensions
        SetFloat(UNIFORM_GRASS_HEIGHT, GrassHeight * ShadowScaleFactor);
        SetFloat(UNIFORM_GRASS_WIDTH, GrassWidth * ShadowScaleFactor);
    }

    #endregion

    #region Wind Uniforms

    private void UpdateWindUniforms()
    {
        // Important: Match main shader's wind animation for consistent shadows
        SetFloat(UNIFORM_TIME, (float)GLFW.GetTime());
        SetFloat(UNIFORM_WIND_STRENGTH, WindStrength);
        SetVector2(UNIFORM_WIND_DIRECTION, WindDirection);
        SetFloat(UNIFORM_WIND_SPEED, WindSpeed);
    }

    #endregion

    #region Interaction Uniforms

    private void UpdateInteractionUniforms()
    {
        SetInt(UNIFORM_NUM_INTERACTION_OBJECTS, _interactionObjectCount);

        if (_interactionObjectCount > 0)
        {
            // Convert Vector3[] to float[] for OpenGL
            var objectPositions = new float[_interactionObjectCount * 3];
            for (var i = 0; i < _interactionObjectCount; i++)
            {
                objectPositions[i * 3] = _interactionPositions[i].X;
                objectPositions[i * 3 + 1] = _interactionPositions[i].Y;
                objectPositions[i * 3 + 2] = _interactionPositions[i].Z;
            }

            // Get the current shader program (assuming shader is already in use via Use() call)
            int currentProgram;
            OpenTK.Graphics.OpenGL4.GL.GetInteger(OpenTK.Graphics.OpenGL4.GetPName.CurrentProgram, out currentProgram);

            var location = OpenTK.Graphics.OpenGL4.GL.GetUniformLocation(currentProgram, UNIFORM_INTERACTION_OBJECTS);
            if (location != -1)
                unsafe
                {
                    fixed (float* ptr = objectPositions)
                    {
                        OpenTK.Graphics.OpenGL4.GL.Uniform3(location, _interactionObjectCount, ptr);
                    }
                }

            SetFloat(UNIFORM_INTERACTION_RADIUS, InteractionRadius);
            SetFloat(UNIFORM_INTERACTION_STRENGTH, InteractionStrength);
        }
    }

    #endregion

    #region Dispose

    protected override void CleanupDerivedResources()
    {
        base.CleanupDerivedResources();
        _interactionPositions = null;
    }

    #endregion
}