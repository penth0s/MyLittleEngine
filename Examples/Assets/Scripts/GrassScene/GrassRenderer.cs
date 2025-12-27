using Engine.Components;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Project.Assets.Scripts.GrassScene;

/// <summary>
/// Manages GPU-based grass rendering with interaction system
/// </summary>
public class GrassRenderer
{
    #region Properties

    // Wind Properties
    public float WindStrength
    {
        get => _gpuGrassShader?.WindStrength ?? 0.25f;
        set
        {
            if (_gpuGrassShader != null) _gpuGrassShader.WindStrength = value;
            if (_gpuGrassShadowShader != null) _gpuGrassShadowShader.WindStrength = value;
        }
    }

    public Vector2 WindDirection
    {
        get => _gpuGrassShader?.WindDirection ?? new Vector2(1.0f, 0.0f);
        set
        {
            if (_gpuGrassShader != null) _gpuGrassShader.WindDirection = value;
            if (_gpuGrassShadowShader != null) _gpuGrassShadowShader.WindDirection = value;
        }
    }

    public float WindSpeed
    {
        get => _gpuGrassShader?.WindSpeed ?? 4.5f;
        set
        {
            if (_gpuGrassShader != null) _gpuGrassShader.WindSpeed = value;
            if (_gpuGrassShadowShader != null) _gpuGrassShadowShader.WindSpeed = value;
        }
    }

    // Grass Appearance
    public float GrassHeight
    {
        get => _gpuGrassShader?.GrassHeight ?? 1.0f;
        set
        {
            if (_gpuGrassShader != null) _gpuGrassShader.GrassHeight = value;
            if (_gpuGrassShadowShader != null) _gpuGrassShadowShader.GrassHeight = value;
        }
    }

    public float GrassWidth
    {
        get => _gpuGrassShader?.GrassWidth ?? 0.1f;
        set
        {
            if (_gpuGrassShader != null) _gpuGrassShader.GrassWidth = value;
            if (_gpuGrassShadowShader != null) _gpuGrassShadowShader.GrassWidth = value;
        }
    }

    // Interaction Properties
    public float InteractionRadius
    {
        get => _gpuGrassShader?.InteractionRadius ?? 1.0f;
        set
        {
            if (_gpuGrassShader != null) _gpuGrassShader.InteractionRadius = value;
            if (_gpuGrassShadowShader != null) _gpuGrassShadowShader.InteractionRadius = value;
        }
    }

    public float InteractionStrength
    {
        get => _gpuGrassShader?.InteractionStrength ?? 5.5f;
        set
        {
            if (_gpuGrassShader != null) _gpuGrassShader.InteractionStrength = value;
            if (_gpuGrassShadowShader != null) _gpuGrassShadowShader.InteractionStrength = value;
        }
    }

    // Shadow Properties
    public float ShadowScaleFactor
    {
        get => _gpuGrassShadowShader?.ShadowScaleFactor ?? 1.0f;
        set
        {
            if (_gpuGrassShadowShader != null)
                _gpuGrassShadowShader.ShadowScaleFactor = value;
        }
    }

    #endregion

    #region Fields

    private int _grassVAO;
    private int _grassVBO;
    private float[] _grassPositions;
    private List<Transform> _interactionObjects;

    // Shader instances
    private GpuGrassShader _gpuGrassShader;
    private GpuGrassShaderShadow _gpuGrassShadowShader;

    #endregion

    #region Initialization

    public void Initialize()
    {
        _interactionObjects = new List<Transform>();

        InitializeShaders();
        InitGrassPositions();
        SetupGrassBuffers();
    }

    private void InitializeShaders()
    {
        _gpuGrassShader = new GpuGrassShader();
        _gpuGrassShadowShader = new GpuGrassShaderShadow();
    }

    private void InitGrassPositions()
    {
        // Grid density - how many grass patches per unit
        var density = 8.0f;

        // Calculate grid dimensions
        var gridWidth = (int)(10 * density) + 1; // From 0 to 10
        var gridHeight = (int)(10 * density) + 1; // From 0 to 10

        // Create array to hold all positions (x, y, z for each point)
        _grassPositions = new float[gridWidth * gridHeight * 3];

        var index = 0;
        for (var z = 0; z < gridHeight; z++)
        for (var x = 0; x < gridWidth; x++)
        {
            // Calculate world position
            var worldX = x / density;
            var worldZ = z / density;

            // Add slight random offset to make grass look more natural
            var rand = new Random(x * 1000 + z); // Seeded for consistency
            var offsetX = (float)(rand.NextDouble() - 0.5) * 0.3f; // ±0.15 units
            var offsetZ = (float)(rand.NextDouble() - 0.5) * 0.3f;

            // Store position (y is always 0 for ground level)
            _grassPositions[index++] = worldX + offsetX;
            _grassPositions[index++] = 0.0f; // Ground level
            _grassPositions[index++] = worldZ + offsetZ;
        }
    }

    private void SetupGrassBuffers()
    {
        // Create VAO for grass
        _grassVAO = GL.GenVertexArray();
        GL.BindVertexArray(_grassVAO);

        // Create VBO for grass positions
        _grassVBO = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _grassVBO);
        GL.BufferData(BufferTarget.ArrayBuffer, _grassPositions.Length * sizeof(float),
            _grassPositions, BufferUsageHint.StaticDraw);

        // Position attribute (location = 0)
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        GL.BindVertexArray(0);
    }

    #endregion

    #region Rendering

    /// <summary>
    /// Renders grass for normal display
    /// </summary>
    /// <param name="camera">Camera to render from</param>
    public void RenderGrass(Camera camera)
    {
        if (_gpuGrassShader == null || camera == null)
            return;

        // Update interaction objects in shader
        UpdateInteractionObjects();

        // Use the shader and update its properties
        _gpuGrassShader.Use();
        _gpuGrassShader.UpdateProperties(camera, Matrix4.Identity);

        // Enable depth testing
        GL.DepthMask(true);
        GL.Enable(EnableCap.DepthTest);

        // Render the grass
        GL.BindVertexArray(_grassVAO);
        GL.DrawArrays(PrimitiveType.Points, 0, _grassPositions.Length / 3);
        GL.BindVertexArray(0);
    }

    /// <summary>
    /// Renders grass for shadow mapping
    /// </summary>
    /// <param name="lightSpaceMatrix">Light's view-projection matrix</param>
    /// <param name="lightDirection">Direction of the light (for billboard orientation)</param>
    /// <param name="shadowScale">Optional override for shadow scale factor</param>
    public void RenderShadow(Matrix4 lightSpaceMatrix, Vector3 lightDirection, float? shadowScale = null)
    {
        if (_gpuGrassShadowShader == null)
            return;

        // Update interaction objects in shader
        UpdateInteractionObjects();

        // Set light properties
        _gpuGrassShadowShader.LightSpaceMatrix = lightSpaceMatrix;
        _gpuGrassShadowShader.LightDirection = lightDirection;

        // Override shadow scale if provided
        if (shadowScale.HasValue)
            _gpuGrassShadowShader.ShadowScaleFactor = shadowScale.Value;

        // Use the shadow shader and update its properties
        _gpuGrassShadowShader.Use();
        _gpuGrassShadowShader.UpdateProperties(null, Matrix4.Identity);

        // Enable depth testing but disable color writes for shadow pass
        GL.DepthMask(true);
        GL.Enable(EnableCap.DepthTest);
        GL.ColorMask(false, false, false, false);

        // Render the grass for shadows
        GL.BindVertexArray(_grassVAO);
        GL.DrawArrays(PrimitiveType.Points, 0, _grassPositions.Length / 3);
        GL.BindVertexArray(0);

        // Re-enable color writes
        GL.ColorMask(true, true, true, true);
    }

    private void UpdateInteractionObjects()
    {
        if (_interactionObjects.Count == 0)
        {
            _gpuGrassShader?.ClearInteractionObjects();
            _gpuGrassShadowShader?.ClearInteractionObjects();
            return;
        }

        // Convert transforms to positions
        var positions = new Vector3[_interactionObjects.Count];
        for (var i = 0; i < _interactionObjects.Count; i++) positions[i] = _interactionObjects[i].WorldPosition;

        // Update both shaders
        _gpuGrassShader?.SetInteractionObjects(positions);
        _gpuGrassShadowShader?.SetInteractionObjects(positions);
    }

    #endregion

    #region Interaction System

    /// <summary>
    /// Adds a single interaction object
    /// </summary>
    /// <param name="target">Transform of the interaction object</param>
    public void AddInteractionObject(Transform target)
    {
        if (target != null && !_interactionObjects.Contains(target)) _interactionObjects.Add(target);
    }

    /// <summary>
    /// Removes a specific interaction object
    /// </summary>
    /// <param name="target">Transform of the interaction object to remove</param>
    public void RemoveInteractionObject(Transform target)
    {
        _interactionObjects.Remove(target);
    }

    /// <summary>
    /// Clears all interaction objects
    /// </summary>
    public void ClearInteractionObjects()
    {
        _interactionObjects.Clear();
    }

    #endregion

    #region Cleanup

    public void Cleanup()
    {
        // Cleanup OpenGL resources
        GL.DeleteVertexArray(_grassVAO);
        GL.DeleteBuffer(_grassVBO);

        // Cleanup shaders
        _gpuGrassShader?.Dispose();
        _gpuGrassShadowShader?.Dispose();

        _gpuGrassShader = null;
        _gpuGrassShadowShader = null;
    }

    #endregion
}