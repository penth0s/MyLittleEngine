using Engine.Systems;
using OpenTK.Graphics.OpenGL4;

namespace Engine.Rendering;

/// <summary>
/// Framebuffer specifically configured for shadow map rendering.
/// Provides depth-only rendering at high resolution for shadow mapping.
/// </summary>
internal class ShadowFramebuffer : IDisposable
{
    #region Constants

    private const int SHADOW_MAP_WIDTH = 1024 * 4;
    private const int SHADOW_MAP_HEIGHT = 1024 * 4;
    private const float SHADOW_BORDER_COLOR_VALUE = 1.0f;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the OpenGL texture handle for the shadow map depth texture.
    /// </summary>
    public int ShadowMap { get; private set; }

    /// <summary>
    /// Gets the OpenGL texture handle for the debug color visualization.
    /// </summary>
    public int DebugColorTexture { get; private set; }

    #endregion

    #region Fields

    private int _frameBufferObject;
    private int _debugFrameBufferObject;
    private readonly RenderSystem _renderSystem;
    private bool _isDisposed;

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes a new instance of the ShadowFramebuffer class.
    /// </summary>
    public ShadowFramebuffer()
    {
        _renderSystem = SystemManager.GetSystem<RenderSystem>();

        CreateShadowFrameBuffer();
        CreateDebugFrameBuffer();
    }

    private void CreateShadowFrameBuffer()
    {
        CreateFrameBufferObject();
        CreateShadowMapTexture();
        ConfigureFrameBufferForDepthOnly();
        ValidateFrameBuffer();
        UnbindFrameBuffer();
    }

    private void CreateFrameBufferObject()
    {
        _frameBufferObject = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _frameBufferObject);
    }

    private void CreateShadowMapTexture()
    {
        ShadowMap = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, ShadowMap);

        AllocateDepthTextureStorage();
        ConfigureShadowMapTextureParameters();
        AttachDepthTextureToFrameBuffer();
    }

    private void AllocateDepthTextureStorage()
    {
        GL.TexImage2D(
            TextureTarget.Texture2D,
            0,
            PixelInternalFormat.DepthComponent32,
            SHADOW_MAP_WIDTH,
            SHADOW_MAP_HEIGHT,
            0,
            PixelFormat.DepthComponent,
            PixelType.Float,
            IntPtr.Zero
        );
    }

    private void ConfigureShadowMapTextureParameters()
    {
        // Filtering
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
            (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Linear);

        // Wrapping
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,
            (int)TextureWrapMode.ClampToBorder);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,
            (int)TextureWrapMode.ClampToBorder);

        // Border color (white = no shadow outside frustum)
        float[] borderColor =
        {
            SHADOW_BORDER_COLOR_VALUE,
            SHADOW_BORDER_COLOR_VALUE,
            SHADOW_BORDER_COLOR_VALUE,
            SHADOW_BORDER_COLOR_VALUE
        };
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, borderColor);
    }

    private void AttachDepthTextureToFrameBuffer()
    {
        GL.FramebufferTexture2D(
            FramebufferTarget.Framebuffer,
            FramebufferAttachment.DepthAttachment,
            TextureTarget.Texture2D,
            ShadowMap,
            0
        );
    }

    private void ConfigureFrameBufferForDepthOnly()
    {
        // No color output needed for shadow mapping
        GL.DrawBuffer(DrawBufferMode.None);
        GL.ReadBuffer(ReadBufferMode.None);
    }

    private void ValidateFrameBuffer()
    {
        var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);

        if (status != FramebufferErrorCode.FramebufferComplete)
            Console.WriteLine($"Shadow Framebuffer is not complete! Status: {status}");
    }

    private void CreateDebugFrameBuffer()
    {
        _debugFrameBufferObject = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _debugFrameBufferObject);

        CreateDebugColorTexture();
        AttachDepthTextureToDebugFrameBuffer();

        GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
        ValidateDebugFrameBuffer();
        UnbindFrameBuffer();
    }

    private void CreateDebugColorTexture()
    {
        DebugColorTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, DebugColorTexture);

        GL.TexImage2D(
            TextureTarget.Texture2D,
            0,
            PixelInternalFormat.Rgba8,
            SHADOW_MAP_WIDTH,
            SHADOW_MAP_HEIGHT,
            0,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            IntPtr.Zero
        );

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
            (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Linear);

        GL.FramebufferTexture2D(
            FramebufferTarget.Framebuffer,
            FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D,
            DebugColorTexture,
            0
        );
    }

    private void AttachDepthTextureToDebugFrameBuffer()
    {
        GL.FramebufferTexture2D(
            FramebufferTarget.Framebuffer,
            FramebufferAttachment.DepthAttachment,
            TextureTarget.Texture2D,
            ShadowMap,
            0
        );
    }

    private void ValidateDebugFrameBuffer()
    {
        var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);

        if (status != FramebufferErrorCode.FramebufferComplete)
            Console.WriteLine($"Debug Shadow Framebuffer is not complete! Status: {status}");
    }

    private void UnbindFrameBuffer()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    #endregion

    #region Framebuffer Operations

    /// <summary>
    /// Binds this shadow framebuffer for rendering and clears the depth buffer.
    /// </summary>
    public void Bind()
    {
        SetShadowMapViewport();
        BindShadowFrameBuffer();
        ClearDepthBuffer();
    }

    private void SetShadowMapViewport()
    {
        GL.Viewport(0, 0, SHADOW_MAP_WIDTH, SHADOW_MAP_HEIGHT);
    }

    private void BindShadowFrameBuffer()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _frameBufferObject);
    }

    private void ClearDepthBuffer()
    {
        GL.Clear(ClearBufferMask.DepthBufferBit);
    }

    /// <summary>
    /// Binds the debug framebuffer for rendering with color output.
    /// </summary>
    public void BindDebugWithDepth()
    {
        SetShadowMapViewport();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _debugFrameBufferObject);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    /// <summary>
    /// Unbinds the shadow framebuffer and restores the render system's main framebuffer.
    /// </summary>
    /// <param name="screenWidth">The width to restore the viewport to.</param>
    /// <param name="screenHeight">The height to restore the viewport to.</param>
    public void Unbind(int screenWidth, int screenHeight)
    {
        RestoreRenderSystemFrameBuffer();
        RestoreViewport(screenWidth, screenHeight);
    }

    private void RestoreRenderSystemFrameBuffer()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _renderSystem.FrameBuffer.FrameBufferHandle);
    }

    private void RestoreViewport(int width, int height)
    {
        GL.Viewport(0, 0, width, height);
    }

    #endregion

    #region Disposal

    /// <summary>
    /// Releases OpenGL resources used by this shadow framebuffer.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            Console.WriteLine("[ShadowFramebuffer] ⚠️ Already disposed, skipping");
            return;
        }

        Console.WriteLine(
            $"[ShadowFramebuffer] Disposing framebuffer (FBO: {_frameBufferObject}, ShadowMap: {ShadowMap}, Debug: {_debugFrameBufferObject})");

        try
        {
            DeleteShadowMapTexture();
            DeleteDebugColorTexture();
            DeleteMainFrameBuffer();
            DeleteDebugFrameBuffer();

            _isDisposed = true;
            Console.WriteLine("[ShadowFramebuffer] ✓ ShadowFramebuffer disposed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ShadowFramebuffer] ❌ Error during disposal: {ex.Message}");
            Console.WriteLine($"[ShadowFramebuffer] Stack trace: {ex.StackTrace}");
            throw;
        }
        finally
        {
            GC.SuppressFinalize(this);
        }
    }

    private void DeleteShadowMapTexture()
    {
        if (ShadowMap != 0)
        {
            Console.WriteLine($"[ShadowFramebuffer]   Deleting shadow map texture: {ShadowMap}");
            GL.DeleteTexture(ShadowMap);
            ShadowMap = 0;
            Console.WriteLine("[ShadowFramebuffer]   ✓ Shadow map texture deleted");
        }
    }

    private void DeleteDebugColorTexture()
    {
        if (DebugColorTexture != 0)
        {
            Console.WriteLine($"[ShadowFramebuffer]   Deleting debug color texture: {DebugColorTexture}");
            GL.DeleteTexture(DebugColorTexture);
            DebugColorTexture = 0;
            Console.WriteLine("[ShadowFramebuffer]   ✓ Debug color texture deleted");
        }
    }

    private void DeleteMainFrameBuffer()
    {
        if (_frameBufferObject != 0)
        {
            Console.WriteLine($"[ShadowFramebuffer]   Deleting main framebuffer: {_frameBufferObject}");
            GL.DeleteFramebuffer(_frameBufferObject);
            _frameBufferObject = 0;
            Console.WriteLine("[ShadowFramebuffer]   ✓ Main framebuffer deleted");
        }
    }

    private void DeleteDebugFrameBuffer()
    {
        if (_debugFrameBufferObject != 0)
        {
            Console.WriteLine($"[ShadowFramebuffer]   Deleting debug framebuffer: {_debugFrameBufferObject}");
            GL.DeleteFramebuffer(_debugFrameBufferObject);
            _debugFrameBufferObject = 0;
            Console.WriteLine("[ShadowFramebuffer]   ✓ Debug framebuffer deleted");
        }
    }

    /// <summary>
    /// Finalizer to warn about improper disposal.
    /// </summary>
    ~ShadowFramebuffer()
    {
        if (!_isDisposed)
            Console.WriteLine(
                $"[ShadowFramebuffer] ⚠️ WARNING: ShadowFramebuffer was not disposed properly! FBO: {_frameBufferObject}");
    }

    #endregion
}