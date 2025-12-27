using Adapters;
using Engine.Components;
using Engine.Core;
using Engine.Rendering;
using Engine.Systems;
using OpenTK.Mathematics;

namespace Project.Assets.Scripts.ShipScene;

public class ShipController : BeroBehaviour
{
    private WaveShader _targetShader;

    public float WavesStrength { get; set; } = 0.6f;
    public System.Numerics.Vector2 WavesFrequency { get; set; } = new(0.25f, 1.0f);
    public float SurfaceDistance { get; set; }

    protected override void Start()
    {
        base.Start();

        FindWaveShader();
    }

    public override void Update()
    {
        base.Update();

        WavesStrength = MathF.Max(0.1f, WavesStrength);

        UpdateShaderParameters();
        UpdateShipTransform();
    }

    private void FindWaveShader()
    {
        var sceneSystem = SystemManager.GetSystem<SceneSystem>();
        var renderers = sceneSystem.CurrentScene.GetComponents<Renderer>();

        foreach (var renderer in renderers)
        {
            if (renderer.Material.Shader is WaveShader waveShader)
            {
                _targetShader = waveShader;
                break;
            }
        }
    }

    private void UpdateShaderParameters()
    {
        if (_targetShader != null)
        {
            _targetShader.WavesFrequency = WavesFrequency;
            _targetShader.WavesStrength = WavesStrength;
        }
    }

    private void UpdateShipTransform()
    {
        var time = Time.TotalTime;
        var position = Transform.WorldPosition;
        var frequency = WavesFrequency.ToOpenTK();

        var wave = WaveUtility.Waves(position, time, WavesStrength, frequency);
        position.Y = wave.X + SurfaceDistance;

        var (pitch, roll) = CalculateShipRotation(position, time, frequency);
        
        Transform.WorldPosition = position;
        Transform.WorldRotation = Quaternion.FromEulerAngles(
            MathHelper.DegreesToRadians(roll),
            Transform.WorldRotation.Y,
            MathHelper.DegreesToRadians(pitch)
        );
    }

    private (float pitch, float roll) CalculateShipRotation(Vector3 position, float time, Vector2 frequency)
    {
        const float half = 1f;

        var front = new Vector3(position.X, 0, position.Z + half);
        var back = new Vector3(position.X, 0, position.Z - half);
        var left = new Vector3(position.X - half, 0, position.Z);
        var right = new Vector3(position.X + half, 0, position.Z);

        front.Y = WaveUtility.Waves(front, time, WavesStrength, frequency).X;
        back.Y = WaveUtility.Waves(back, time, WavesStrength, frequency).X;
        left.Y = WaveUtility.Waves(left, time, WavesStrength, frequency).X;
        right.Y = WaveUtility.Waves(right, time, WavesStrength, frequency).X;

        return WaveUtility.ComputePitchRoll(front, back, left, right);
    }
}