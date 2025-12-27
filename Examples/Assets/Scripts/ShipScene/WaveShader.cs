using Adapters;
using Engine.Components;
using Engine.Core;
using Engine.Shaders;
using Engine.Systems;
using OpenTK.Mathematics;
using RenderingMode = Engine.Rendering.RenderingMode;

namespace Project.Assets.Scripts.ShipScene;

public class WaveShader : ShaderBase
{
    private const string DEFAULT_VERTEX_SHADER = "wave.vert";
    private const string DEFAULT_FRAGMENT_SHADER = "wave.frag";

    private const string UNIFORM_MODEL = "uModel";
    private const string UNIFORM_VIEW = "uView";
    private const string UNIFORM_PROJ = "uProj";

    private const string UNIFORM_TIME = "uTime";
    private const string UNIFORM_WAVE_STRENGTH = "uWavesStrength";
    private const string UNIFORM_WAVE_FREQ = "uWavesFreq";
    private const string UNIFORM_COLOR_A = "uColorA";
    private const string UNIFORM_COLOR_B = "uColorB";

    public float WavesStrength { get; set; } = 0.6f;
    public System.Numerics.Vector2 WavesFrequency { get; set; } = new(0.25f, 1.0f);

    public System.Numerics.Vector4 ColorA { get; set; } = new(1.0f, 1.0f, 1.0f, 1.0f);
    public System.Numerics.Vector4 ColorB { get; set; } = new(0.0f, 1.0f, 0.0f, 1.0f);

    public override int PassCount => 1;

    protected override string VertexShaderName => DEFAULT_VERTEX_SHADER;
    protected override string FragmentShaderName => DEFAULT_FRAGMENT_SHADER;
    protected override string GeometryShaderName { get; } = null;

    private SceneSystem _scene;

    public WaveShader() : base(DEFAULT_VERTEX_SHADER, DEFAULT_FRAGMENT_SHADER)
    {
        RenderingMode = RenderingMode.OPAQUE;
        Initialize();
    }

    private void Initialize()
    {
        _scene = SystemManager.GetSystem<SceneSystem>();
    }

    public override void UpdateProperties(Camera camera, Matrix4 transform, int pass = 0)
    {
        base.UpdateProperties(camera, transform, pass);

        SetMatrix4(UNIFORM_MODEL, transform);
        SetMatrix4(UNIFORM_VIEW, camera.GetViewMatrix());
        SetMatrix4(UNIFORM_PROJ, camera.GetProjectionMatrix());

        SetFloat(UNIFORM_TIME, Time.TotalTime);
        SetFloat(UNIFORM_WAVE_STRENGTH, WavesStrength);
        SetVector2(UNIFORM_WAVE_FREQ, WavesFrequency.ToOpenTK());
        SetVector3(UNIFORM_COLOR_A, ColorA.ToVector3().ToOpenTK());
        SetVector3(UNIFORM_COLOR_B, ColorB.ToVector3().ToOpenTK());
    }
}