using Engine.Shaders.Implementations;
using Engine.Systems;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine.Rendering;

/// <summary>
/// Renders a 3D grid on the ground plane (Y=0) to help with spatial awareness in the editor.
/// Features distance-based fade-out for better visual clarity.
/// </summary>
internal class Grid
{
    #region Constants

    private const float DEFAULT_GRID_SIZE = 1000f;
    private const int DEFAULT_GRID_DIVISIONS = 100;
    private const int POSITION_COMPONENT_COUNT = 3;

    #endregion

    #region Fields

    private int _gridVao;
    private int _gridVbo;
    private int _gridVertexCount;
    private Material _gridMaterial;
    private SceneSystem _sceneSystem;

    #endregion

    #region Initialization

    public Grid()
    {
        InitializeMaterial();
        GenerateGridGeometry(DEFAULT_GRID_SIZE, DEFAULT_GRID_DIVISIONS);
    }

    private void InitializeMaterial()
    {
        _gridMaterial = new Material();
        _gridMaterial.UpdateShader<GridShader>();
    }

    #endregion

    #region Grid Generation

    private void GenerateGridGeometry(float size, int divisions)
    {
        var vertices = CreateGridVertices(size, divisions);
        _gridVertexCount = vertices.Count / POSITION_COMPONENT_COUNT;

        CreateGridBuffers(vertices);
    }

    private List<float> CreateGridVertices(float size, int divisions)
    {
        List<float> vertices = new();
        var halfSize = size / 2f;
        var step = size / divisions;

        // Generate grid lines
        for (var i = 0; i <= divisions; i++)
        {
            var position = -halfSize + i * step;

            // Horizontal lines (along Z-axis)
            AddVertex(vertices, position, 0, -halfSize);
            AddVertex(vertices, position, 0, halfSize);

            // Vertical lines (along X-axis)
            AddVertex(vertices, -halfSize, 0, position);
            AddVertex(vertices, halfSize, 0, position);
        }

        return vertices;
    }

    private void AddVertex(List<float> vertices, float x, float y, float z)
    {
        vertices.Add(x);
        vertices.Add(y);
        vertices.Add(z);
    }

    private void CreateGridBuffers(List<float> vertices)
    {
        _gridVao = GL.GenVertexArray();
        _gridVbo = GL.GenBuffer();

        GL.BindVertexArray(_gridVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _gridVbo);

        var vertexArray = vertices.ToArray();
        GL.BufferData(
            BufferTarget.ArrayBuffer,
            vertexArray.Length * sizeof(float),
            vertexArray,
            BufferUsageHint.StaticDraw
        );

        SetupVertexAttributes();

        GL.BindVertexArray(0);
    }

    private void SetupVertexAttributes()
    {
        // Position attribute (location = 0)
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(
            0,
            POSITION_COMPONENT_COUNT,
            VertexAttribPointerType.Float,
            false,
            POSITION_COMPONENT_COUNT * sizeof(float),
            0
        );
    }

    #endregion

    #region Drawing

    public void Draw()
    {
        EnsureSceneSystemInitialized();

        SetupRenderState();
        BindMaterialAndSetUniforms();
        RenderGrid();
        RestoreRenderState();
    }

    private void SetupRenderState()
    {
        // Enable blending for fade effect
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        // Adjust depth testing
        GL.DepthFunc(DepthFunction.Lequal);
        GL.DepthMask(false); // Don't write to depth buffer
    }

    private void BindMaterialAndSetUniforms()
    {
        _gridMaterial.BindShader();

        var matrices = CalculateMatrices();
        SetShaderUniforms(matrices);
    }

    private (Matrix4 mvp, Matrix4 model, Vector3 cameraPos) CalculateMatrices()
    {
        var scene = _sceneSystem!.CurrentScene;

        // Grid is at world origin with no transformation
        var model = Matrix4.Identity;

        // Calculate view matrix
        var cameraPosition = scene.CameraPosition;
        var cameraTarget = cameraPosition + scene.CameraForward;
        var view = Matrix4.LookAt(cameraPosition, cameraTarget, Vector3.UnitY);

        // Get projection matrix
        var projection = scene.CameraProjectionMatrix;

        // Calculate MVP matrix
        var mvp = model * view * projection;

        return (mvp, model, cameraPosition);
    }

    private void SetShaderUniforms((Matrix4 mvp, Matrix4 model, Vector3 cameraPos) matrices)
    {
        _gridMaterial.Shader.SetMatrix4("uMVP", matrices.mvp);
        _gridMaterial.Shader.SetMatrix4("uModel", matrices.model);
        _gridMaterial.Shader.SetVector3("uCameraPos", matrices.cameraPos);
    }

    private void RenderGrid()
    {
        GL.BindVertexArray(_gridVao);
        GL.DrawArrays(PrimitiveType.Lines, 0, _gridVertexCount);
        GL.BindVertexArray(0);
    }

    private void RestoreRenderState()
    {
        // Restore default render state
        GL.Disable(EnableCap.Blend);
        GL.DepthFunc(DepthFunction.Less);
        GL.DepthMask(true);
    }

    #endregion

    #region Helper Methods

    private void EnsureSceneSystemInitialized()
    {
        _sceneSystem ??= SystemManager.GetSystem<SceneSystem>();
    }

    #endregion

    #region Cleanup

    public void Dispose()
    {
        // Cleanup OpenGL resources
        if (_gridVao != 0) GL.DeleteVertexArray(_gridVao);

        if (_gridVbo != 0) GL.DeleteBuffer(_gridVbo); 
    }

    #endregion
}