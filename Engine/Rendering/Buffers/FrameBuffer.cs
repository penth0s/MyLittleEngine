using Engine.Core;
using Engine.Systems;
using OpenTK.Graphics.OpenGL4;

namespace Engine.Rendering;

/// <summary>
/// Manages an OpenGL framebuffer with multiple color attachments and a depth buffer.
/// Supports automatic resizing based on viewport changes.
/// Provides color, normal, depth, and SSR mask textures for deferred rendering and post-processing.
/// </summary>
public class FrameBuffer : IDisposable
{
    #region Constants

    private const int COLOR_ATTACHMENT_COUNT = 2;
    private const int COLOR_ATTACHMENT_INDEX = 0;
    private const int NORMAL_ATTACHMENT_INDEX = 1;
    private const float DEFAULT_CLEAR_DEPTH = 1.0f;

    #endregion

    #region Fields - OpenGL Handles

    /// <summary>
    /// The OpenGL handle for the framebuffer object.
    /// </summary>
    public int FrameBufferHandle;

    /// <summary>
    /// The OpenGL handle for the main color texture attachment.
    /// </summary>
    public int ColorTexture;

    /// <summary>
    /// The OpenGL handle for the depth buffer texture.
    /// </summary>
    public int DepthTexture;

    /// <summary>
    /// The OpenGL handle for the normal texture attachment.
    /// </summary>
    public int NormalTexture;

    /// <summary>
    /// The OpenGL handle for the SSR (Screen Space Reflections) mask texture.
    /// </summary>
    public int SsrMaskTexture;

    #endregion

    #region Fields - State

    private int _width;
    private int _height;
    private readonly SceneSystem _sceneSystem;
    private bool _isDisposed;

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes a new instance of the FrameBuffer class with the specified dimensions.
    /// </summary>
    /// <param name="width">The width of the framebuffer in pixels.</param>
    /// <param name="height">The height of the framebuffer in pixels.</param>
    public FrameBuffer(int width, int height)
    {
        _width = width;
        _height = height;
        _sceneSystem = SystemManager.GetSystem<SceneSystem>();

        CreateFrameBufferResources();
    }

    #endregion

    #region Framebuffer Creation

    private void CreateFrameBufferResources()
    {
        CreateFrameBufferObject();
        CreateColorAttachment();
        CreateNormalAttachment();
        CreateDepthAttachment();
        ConfigureDrawBuffers();
        ValidateFrameBuffer();

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    private void CreateFrameBufferObject()
    {
        FrameBufferHandle = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, FrameBufferHandle);
    }

    private void CreateColorAttachment()
    {
        ColorTexture = CreateTexture2D(
            PixelInternalFormat.Rgba8,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            TextureMinFilter.Linear,
            TextureMagFilter.Linear
        );

        GL.FramebufferTexture2D(
            FramebufferTarget.Framebuffer,
            FramebufferAttachment.ColorAttachment0 + COLOR_ATTACHMENT_INDEX,
            TextureTarget.Texture2D,
            ColorTexture,
            0
        );
    }

    private void CreateNormalAttachment()
    {
        NormalTexture = CreateTexture2D(
            PixelInternalFormat.Rgba16f,
            PixelFormat.Rgba,
            PixelType.Float,
            TextureMinFilter.Linear,
            TextureMagFilter.Linear
        );

        GL.FramebufferTexture2D(
            FramebufferTarget.Framebuffer,
            FramebufferAttachment.ColorAttachment0 + NORMAL_ATTACHMENT_INDEX,
            TextureTarget.Texture2D,
            NormalTexture,
            0
        );
    }

    private void CreateDepthAttachment()
    {
        DepthTexture = CreateTexture2D(
            PixelInternalFormat.DepthComponent32f,
            PixelFormat.DepthComponent,
            PixelType.Float,
            TextureMinFilter.Nearest,
            TextureMagFilter.Nearest
        );

        GL.FramebufferTexture2D(
            FramebufferTarget.Framebuffer,
            FramebufferAttachment.DepthAttachment,
            TextureTarget.Texture2D,
            DepthTexture,
            0
        );
    }

    private int CreateTexture2D(
        PixelInternalFormat internalFormat,
        PixelFormat format,
        PixelType type,
        TextureMinFilter minFilter,
        TextureMagFilter magFilter)
    {
        var texture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, texture);

        GL.TexImage2D(
            TextureTarget.Texture2D,
            0,
            internalFormat,
            _width,
            _height,
            0,
            format,
            type,
            IntPtr.Zero
        );

        ConfigureTextureParameters(minFilter, magFilter);

        return texture;
    }

    private void ConfigureTextureParameters(TextureMinFilter minFilter, TextureMagFilter magFilter)
    {
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minFilter);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)magFilter);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
    }

    private void ConfigureDrawBuffers()
    {
        GL.DrawBuffers(COLOR_ATTACHMENT_COUNT, new[]
        {
            DrawBuffersEnum.ColorAttachment0,
            DrawBuffersEnum.ColorAttachment1
        });

        GL.ReadBuffer(ReadBufferMode.None);
    }

    private void ValidateFrameBuffer()
    {
        var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);

        if (status != FramebufferErrorCode.FramebufferComplete)
            throw new Exception($"Framebuffer is incomplete: {status}");
    }

    #endregion

    #region Framebuffer Usage

    /// <summary>
    /// Binds this framebuffer for rendering. Automatically resizes if viewport has changed.
    /// Clears color and depth buffers.
    /// </summary>
    public void Bind()
    {
        CheckAndResizeIfNeeded();
        BindAndClear();
    }

    private void BindAndClear()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, FrameBufferHandle);
        GL.Viewport(0, 0, _width, _height);

        ClearBuffers();
    }

    private void ClearBuffers()
    {
        GL.ClearDepth(DEFAULT_CLEAR_DEPTH);
        GL.ClearColor(_sceneSystem.CurrentScene.ClearColor);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    /// <summary>
    /// Unbinds this framebuffer, returning to the default framebuffer.
    /// </summary>
    public void Unbind()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    #endregion

    #region Resizing

    private void CheckAndResizeIfNeeded()
    {
        var viewportSize = Screen.GetViewportSize();

        if (ShouldResize(viewportSize)) ResizeFrameBuffer(viewportSize.X, viewportSize.Y);
    }

    private bool ShouldResize(OpenTK.Mathematics.Vector2i viewportSize)
    {
        return _width != viewportSize.X || _height != viewportSize.Y;
    }

    private void ResizeFrameBuffer(int newWidth, int newHeight)
    {
        ReleaseResources();

        _width = newWidth;
        _height = newHeight;

        CreateFrameBufferResources();

        Console.WriteLine($"FrameBuffer resized to: {_width}x{_height}");
    }

    #endregion

    #region Resource Management

    private void ReleaseResources()
    {
        DeleteTextureIfValid(ref ColorTexture);
        DeleteTextureIfValid(ref NormalTexture);
        DeleteTextureIfValid(ref DepthTexture);
        DeleteTextureIfValid(ref SsrMaskTexture);
        DeleteFrameBufferIfValid(ref FrameBufferHandle);
    }

    private void DeleteTextureIfValid(ref int texture)
    {
        if (texture != 0)
        {
            GL.DeleteTexture(texture);
            texture = 0;
        }
    }

    private void DeleteFrameBufferIfValid(ref int frameBuffer)
    {
        if (frameBuffer != 0)
        {
            GL.DeleteFramebuffer(frameBuffer);
            frameBuffer = 0;
        }
    }

    #endregion

    #region Disposal

    /// <summary>
    /// Releases all OpenGL resources used by this framebuffer.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        ReleaseResources();

        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    #endregion
}