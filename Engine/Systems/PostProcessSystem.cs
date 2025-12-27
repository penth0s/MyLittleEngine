using Engine.Configs;
using Engine.Core;
using Engine.Database.Implementations;
using Engine.Rendering;
using Engine.Shaders.Implementations;
using OpenTK.Graphics.OpenGL4;

namespace Engine.Systems;

/// <summary>
/// Handles all post-processing effects in the render pipeline,
/// such as screen-space reflections, bloom, vignette, etc.
/// </summary>
public sealed class PostProcessSystem : ISystem<RenderConfig>, IDisposable
{
    public ColorGradingShader ColorGradingShader => _colorGradingShader;
    public VignetteShader VignetteShader => _vignetteShader;

    private FrameBuffer _postProcessBuffer1;
    private FrameBuffer _postProcessBuffer2;
    private RenderSystem _renderSystem;
    private ShaderDataBase _shaderDatabase;

    private VignetteShader _vignetteShader;
    private BlitShader _blitShader;
    private ColorGradingShader _colorGradingShader;

    public bool EnableVignette { get; set; } = false;
    public bool EnableColorGrading { get; set; } = false;

    private bool _isInitialized;

    #region Initialization

    public void Initialize(RenderConfig config)
    {
        EngineWindow.EngineInitialized += OnEngineInitialized;
    }

    private void OnEngineInitialized()
    {
        if (_isInitialized)
            return;

        _isInitialized = true;

        _renderSystem = SystemManager.GetSystem<RenderSystem>();
        _shaderDatabase = SystemManager.GetSystem<DatabaseSystem>().GetDatabase<ShaderDataBase>();


        InitializeFrameBuffer();
        CacheShaders();

        _renderSystem.PostRenderPass += OnPostRenderPass;
    }

    private void InitializeFrameBuffer()
    {
        var viewportSize = Screen.GetViewportSize();
        _postProcessBuffer1 = new FrameBuffer(viewportSize.X, viewportSize.Y);
        _postProcessBuffer2 = new FrameBuffer(viewportSize.X, viewportSize.Y);
    }

    private void CacheShaders()
    {
        _vignetteShader = _shaderDatabase.Get<VignetteShader>();
        _blitShader = _shaderDatabase.Get<BlitShader>();
        _colorGradingShader = _shaderDatabase.Get<ColorGradingShader>();
    }

    #endregion

    #region Post Processing

    private void OnPostRenderPass()
    {
        if (!_isInitialized)
            return;

        var sourceBuffer = _renderSystem.FrameBuffer;

        // Ping-pong için buffer'ları hazırla
        FrameBuffer readBuffer = sourceBuffer; // İlk okuma source'tan
        FrameBuffer writeBuffer = _postProcessBuffer1; // İlk yazma buffer1'e

        // Aktif efekt sayısını kontrol et
        var hasAnyEffect = EnableColorGrading || EnableVignette;

        // Hiç efekt yoksa direkt return
        if (!hasAnyEffect)
            return;

        try
        {
            // Her efekt: readBuffer'dan oku -> writeBuffer'a yaz -> swap

            if (EnableColorGrading)
            {
                writeBuffer.Bind();
                RenderColorGrading(readBuffer);
                writeBuffer.Unbind();

                // Swap
                SwapBuffers(ref readBuffer, ref writeBuffer);
            }

            if (EnableVignette)
            {
                writeBuffer.Bind();
                RenderVignette(readBuffer);
                writeBuffer.Unbind();

                // Swap
                SwapBuffers(ref readBuffer, ref writeBuffer);
            }

            // readBuffer şimdi son efektin sonucunu tutuyor
            // Bunu render buffer'a kopyala
            _renderSystem.FrameBuffer.Bind();
            CopyBuffer(readBuffer);
            _renderSystem.FrameBuffer.Unbind();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PostProcess Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Ping-pong için buffer'ları swap eder
    /// </summary>
    private void SwapBuffers(ref FrameBuffer readBuffer, ref FrameBuffer writeBuffer)
    {
        // Yazdığımız buffer artık okuma buffer'ı
        FrameBuffer temp = readBuffer;
        readBuffer = writeBuffer;

        // Yeni yazma buffer'ı: eski okuma buffer'ı veya boştaki buffer
        if (temp == _renderSystem.FrameBuffer)
            // İlk swap: source'tan geldik, boştaki buffer'ı kullan
            writeBuffer = _postProcessBuffer2;
        else
            // Sonraki swap'ler: buffer1 <-> buffer2
            writeBuffer = temp == _postProcessBuffer1 ? _postProcessBuffer2 : _postProcessBuffer1;
    }

    private void RenderColorGrading(FrameBuffer inputBuffer)
    {
        _colorGradingShader.Use();
        _colorGradingShader.UpdateProperties(inputBuffer);
    }

    private void CopyBuffer(FrameBuffer source)
    {
        _blitShader.Use();
        _blitShader.SetInt("screenTexture", 0);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, source.ColorTexture);
        ScreenQuadRenderer.Draw();
    }

    private void RenderVignette(FrameBuffer inputBuffer)
    {
        _vignetteShader.Use();
        _vignetteShader.UpdateProperties(inputBuffer);
    }

    #endregion

    #region Cleanup

    public void Dispose()
    {
        _postProcessBuffer1?.Dispose();
        _postProcessBuffer2?.Dispose();
        _renderSystem.PostRenderPass -= OnPostRenderPass;
        _isInitialized = false;
    }

    #endregion
}