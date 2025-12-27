using System.Numerics;
using Adapters;
using Engine.Components;
using Engine.Core;
using Engine.Scripts;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Environment = Engine.Scene.Environment;

namespace Project.Assets.Scripts;

public class ToonSkyboxController : BeroBehaviour
{
    public float RotationSpeed;
    public float Rotation;

    public bool UseFog;

    public FogPreset DayPreset;
    public FogPreset SunsetPreset;
    public FogPreset NightPreset;
    public float PresetBlendValue; // 0-2 arası: 0=Day, 1=Sunset, 2=Night

    public ToonSkyboxShader _toonSkyboxShader;

    // Animasyon değişkenleri
    private bool _isAnimating = false;
    private float _animationDuration = 10.0f; // Varsayılan süre
    private float _animationTimer = 0.0f;
    private float _animationStartValue = 0.0f;
    private float _animationTargetValue = 2.0f;

    protected override void Awake()
    {
        base.Awake();

        _toonSkyboxShader = (ToonSkyboxShader)Environment.GetDataReference().SkyboxShader;
        PresetBlendValue = 0.0f;

        //DayPreset = new FogPreset();
        //SunsetPreset = new FogPreset();
        //NightPreset = new FogPreset();
    }

    public override void Update()
    {
        base.Update();

        if (_toonSkyboxShader == null) return;

        if (Input.Keybord.IsKeyDown(Keys.Q)) AnimateDayToNight(5);

        // Animasyon güncelleme
        if (_isAnimating)
        {
            _animationTimer += Time.DeltaTime;
            var t = _animationTimer / _animationDuration;

            if (t >= 1.0f)
            {
                // Animasyon tamamlandı
                PresetBlendValue = _animationTargetValue;
                _isAnimating = false;
            }
            else
            {
                // Lerp ile yumuşak geçiş
                PresetBlendValue = _animationStartValue + (_animationTargetValue - _animationStartValue) * t;
            }
        }

        _toonSkyboxShader.BlendValue = PresetBlendValue / 2;
        _toonSkyboxShader.EnableRotation = true;
        _toonSkyboxShader.Rotation = Rotation;
        _toonSkyboxShader.RotationSpeed = RotationSpeed;

        Environment.UseFog = UseFog;
        UpdateFogPresets();
    }

    /// <summary>
    /// Belirtilen süre içinde blend değerini 0'dan 2'ye çıkarır (Day -> Night)
    /// </summary>
    /// <param name="duration">Animasyon süresi (saniye)</param>
    private void AnimateDayToNight(float duration)
    {
        _animationStartValue = 0.0f;
        _animationTargetValue = 2.0f;
        _animationDuration = duration;
        _animationTimer = 0.0f;
        _isAnimating = true;
    }

    private void UpdateFogPresets()
    {
        if (!UseFog) return;

        Vector4 blendedFogColor;
        float blendedFogStart;
        float blendedFogEnd;
        float blendedExposure;
        float blendedFogIntensity;
        float blendedFogSmoothness;
        float blendedFogFill;

        // FogBlendValue 0-2 arası değer alıyor
        // 0-1: Day -> Sunset
        // 1-2: Sunset -> Night
        if (PresetBlendValue <= 1.0f)
        {
            // Day ile Sunset arası (0-1)
            var t = PresetBlendValue; // 0-1 arası normalize
            blendedFogColor = Vector4.Lerp(DayPreset.FogColor, SunsetPreset.FogColor, t);
            blendedFogStart = DayPreset.FogStart + (SunsetPreset.FogStart - DayPreset.FogStart) * t;
            blendedFogEnd = DayPreset.FogEnd + (SunsetPreset.FogEnd - DayPreset.FogEnd) * t;
            blendedExposure = DayPreset.Exposure + (SunsetPreset.Exposure - DayPreset.Exposure) * t;
            blendedFogIntensity = DayPreset.FogIntensity + (SunsetPreset.FogIntensity - DayPreset.FogIntensity) * t;
            blendedFogSmoothness = DayPreset.FogSmoothness + (SunsetPreset.FogSmoothness - DayPreset.FogSmoothness) * t;
            blendedFogFill = DayPreset.FogFill + (SunsetPreset.FogFill - DayPreset.FogFill) * t;
        }
        else
        {
            // Sunset ile Night arası (1-2)
            var t = PresetBlendValue - 1.0f; // 0-1 arası normalize
            blendedFogColor = Vector4.Lerp(SunsetPreset.FogColor, NightPreset.FogColor, t);
            blendedFogStart = SunsetPreset.FogStart + (NightPreset.FogStart - SunsetPreset.FogStart) * t;
            blendedFogEnd = SunsetPreset.FogEnd + (NightPreset.FogEnd - SunsetPreset.FogEnd) * t;
            blendedExposure = SunsetPreset.Exposure + (NightPreset.Exposure - SunsetPreset.Exposure) * t;
            blendedFogIntensity =
                SunsetPreset.FogIntensity + (NightPreset.FogIntensity - SunsetPreset.FogIntensity) * t;
            blendedFogSmoothness =
                SunsetPreset.FogSmoothness + (NightPreset.FogSmoothness - SunsetPreset.FogSmoothness) * t;
            blendedFogFill = SunsetPreset.FogFill + (NightPreset.FogFill - SunsetPreset.FogFill) * t;
        }

        // Environment fog değerlerini güncelle
        Environment.FogColor = blendedFogColor;
        Environment.FogStart = blendedFogStart;
        Environment.FogEnd = blendedFogEnd;

        // Skybox shader değerlerini güncelle
        _toonSkyboxShader.Exposure = blendedExposure;
        _toonSkyboxShader.FogIntensity = blendedFogIntensity;
        _toonSkyboxShader.FogSmoothness = blendedFogSmoothness;
        _toonSkyboxShader.FogFill = blendedFogFill;
    }
}

public class FogPreset
{
    public Vector4 FogColor;
    public float FogStart;
    public float FogEnd;

    public float Exposure = 1.0f;
    public float FogIntensity = 0.5f;
    public float FogSmoothness = 0.0f;
    public float FogFill = 0.5f;
}