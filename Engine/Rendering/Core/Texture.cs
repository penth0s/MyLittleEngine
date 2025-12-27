using System.Text.Json.Serialization;
using OpenTK.Graphics.OpenGL4;

namespace Engine.Rendering;

/// <summary>
/// A helper class meant to simplify loading and using textures
/// </summary>
public class Texture : IDisposable
{
    #region Properties

    [JsonIgnore] public int Handle { get; }

    public string Path { get; set; }

    [JsonIgnore] public string Name => System.IO.Path.GetFileName(Path);

    [JsonIgnore] public bool IsDisposed { get; private set; }

    #endregion

    #region Constructor

    public Texture(int glHandle)
    {
        Handle = glHandle;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Activate texture on the specified texture unit.
    /// Multiple textures can be bound if your shader needs more than just one.
    /// If you want to do that, use GL.ActiveTexture to set which slot GL.BindTexture binds to.
    /// The OpenGL standard requires that there be at least 16, but there can be more depending on your graphics card.
    /// </summary>
    public void Use(TextureUnit unit)
    {
        ValidateNotDisposed();

        GL.ActiveTexture(unit);
        GL.BindTexture(TextureTarget.Texture2D, Handle);
    }

    #endregion

    #region Validation

    private void ValidateNotDisposed()
    {
        if (IsDisposed) throw new ObjectDisposedException(nameof(Texture));
    }

    #endregion

    #region IDisposable Implementation

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (IsDisposed) return;

        if (Handle > 0)
            try
            {
                GL.DeleteTexture(Handle);
            }
            catch
            {
                // Silently catch - GL context may already be destroyed
            }

        IsDisposed = true;
    }

    ~Texture()
    {
        Dispose(false);
    }

    #endregion
}