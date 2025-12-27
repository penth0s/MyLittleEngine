using Engine.Components;
using Engine.Scripts;
using Engine.Systems;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Project.Assets.Scripts.ToonScene;

public class ToonSceneController : BeroBehaviour
{
    public bool IsColorGradingEnable;
    public float Exposure { get; set; } = 1.0f;
    public float Contrast { get; set; } = 1.0f;
    public float Saturation { get; set; } = 1.0f;
    public float Temperature { get; set; } = 0.1f;
    public float Tint { get; set; } = 0.0f;

    public bool IsVignetteEnable;
    public float Intensity { get; set; } = 1.3f;
    public float Smoothness { get; set; } = 0.5f;
    public float Roundness { get; set; } = 1.0f;


    private PostProcessSystem _postProcessSystem;
    private Vector3 _cameraStartPosition = new(-160, 22, 112);
    private Vector3 _cameraStartEulerAngles = new(10, 110, 0);

    protected override void Start()
    {
        base.Start();

        Transform.WorldPosition = _cameraStartPosition;
        Transform.EulerAngles = _cameraStartEulerAngles;

        _postProcessSystem = SystemManager.GetSystem<PostProcessSystem>();
    }

    public override void Update()
    {
        base.Update();

        _postProcessSystem.EnableColorGrading = IsColorGradingEnable;
        _postProcessSystem.EnableVignette = IsVignetteEnable;

        if (IsColorGradingEnable)
        {
            _postProcessSystem.ColorGradingShader.Contrast = Contrast;
            _postProcessSystem.ColorGradingShader.Saturation = Saturation;
            _postProcessSystem.ColorGradingShader.Tint = Tint;
            _postProcessSystem.ColorGradingShader.Temperature = Temperature;
            _postProcessSystem.ColorGradingShader.Exposure = Exposure;
        }

        if (IsVignetteEnable)
        {
            _postProcessSystem.VignetteShader.Smoothness = Smoothness;
            _postProcessSystem.VignetteShader.Intensity = Intensity;
            _postProcessSystem.VignetteShader.Roundness = Roundness;
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        _postProcessSystem.EnableColorGrading = false;
        _postProcessSystem.EnableVignette = false;
    }
}