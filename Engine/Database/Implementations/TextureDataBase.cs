using Engine.Rendering;
using Engine.Utilities;
using OpenTK.Graphics.OpenGL4;
using StbImageSharp;

namespace Engine.Database.Implementations;

/// <summary>
/// Database for loading and managing texture resources
/// </summary>
public sealed class TextureDataBase : IDatabase
{
    #region Constants

    private const string TEXTURE_FOLDER_NAME = "Assets/Textures";

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".tga", ".bmp", ".psd", ".gif", ".tif", ".tiff"
    };

    #endregion

    #region Properties

    public List<string> TextureNames => _textureNames;

    #endregion

    #region Fields

    private List<string> _textureNames;
    private Dictionary<string, string> _textureFilePathMap;

    #endregion

    #region Initialization

    public void Initialize()
    {
        _textureFilePathMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _textureNames = DiscoverAllTextures();
    }

    #endregion

    #region Texture Discovery

    private List<string> DiscoverAllTextures()
    {
        // Discover textures in Engine path
        DiscoverTexturesInPath(ProjectPathHelper.GetEnginePath());

        // Discover textures in Example path
        DiscoverTexturesInPath(ProjectPathHelper.GetExamplePath());

        Console.WriteLine($"Total discovered textures: {_textureFilePathMap.Count}");
        return _textureFilePathMap.Keys.ToList();
    }

    private void DiscoverTexturesInPath(string basePath)
    {
        var texturePath = Path.Combine(basePath, TEXTURE_FOLDER_NAME);

        if (!Directory.Exists(texturePath))
        {
            Console.WriteLine($"Warning: Texture directory not found: {texturePath}");
            return;
        }

        try
        {
            var textureFiles = Directory.GetFiles(texturePath, "*.*", SearchOption.AllDirectories)
                .Where(file => IsSupportedFormat(file))
                .ToList();

            foreach (var filePath in textureFiles) RegisterTextureFile(filePath);

            Console.WriteLine($"Discovered {textureFiles.Count} textures in {texturePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error discovering textures in {texturePath}: {ex.Message}");
        }
    }

    private void RegisterTextureFile(string fullPath)
    {
        var fileName = Path.GetFileName(fullPath);

        if (!_textureFilePathMap.ContainsKey(fileName))
        {
            _textureFilePathMap[fileName] = fullPath;
            Console.WriteLine($"Registered texture file: {fileName} -> {fullPath}");
        }
        else
        {
            Console.WriteLine($"Warning: Duplicate texture file name: {fileName}");
        }
    }

    private static bool IsSupportedFormat(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return SupportedExtensions.Contains(extension);
    }

    #endregion

    #region Texture Loading - 2D

    public Texture LoadTexture(string path)
    {
        ValidateTexturePath(path);

        var handle = CreateTextureHandle();
        BindTexture2D(handle);

        LoadImageData(path, handle);
        ConfigureTexture2DParameters();

        return CreateTextureObject(handle, path);
    }

    private void ValidateTexturePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Texture path cannot be null or empty", nameof(path));
    }

    private int CreateTextureHandle()
    {
        return GL.GenTexture();
    }

    private void BindTexture2D(int handle)
    {
        GL.BindTexture(TextureTarget.Texture2D, handle);
    }

    private void LoadImageData(string relativePath, int handle)
    {
        var fullPath = GetFullTexturePath(relativePath);

        if (!File.Exists(fullPath)) throw new FileNotFoundException($"Texture file not found: {fullPath}");

        // OpenGL has texture origin in lower left corner instead of top left
        // So we flip the image when loading
        StbImage.stbi_set_flip_vertically_on_load(1);

        try
        {
            using (Stream stream = File.OpenRead(fullPath))
            {
                var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

                GL.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    PixelInternalFormat.Rgba,
                    image.Width,
                    image.Height,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    image.Data
                );
            }
        }
        catch (Exception ex)
        {
            GL.DeleteTexture(handle);
            throw new InvalidOperationException($"Failed to load texture: {fullPath}", ex);
        }
    }

    private void ConfigureTexture2DParameters()
    {
        // Set filtering
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        // Set wrapping
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        // Generate mipmaps
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
    }

    private Texture CreateTextureObject(int handle, string path)
    {
        return new Texture(handle)
        {
            Path = path
        };
    }

    private string GetFullTexturePath(string relativePath)
    {
        // First try to get from the path map (registered files)
        if (_textureFilePathMap.TryGetValue(relativePath, out var fullPath)) return fullPath;

        // If not found in map, try both Engine and Example paths
        var enginePath = Path.Combine(ProjectPathHelper.GetEnginePath(), TEXTURE_FOLDER_NAME, relativePath);
        if (File.Exists(enginePath)) return enginePath;

        var examplePath = Path.Combine(ProjectPathHelper.GetExamplePath(), TEXTURE_FOLDER_NAME, relativePath);
        if (File.Exists(examplePath)) return examplePath;

        // If still not found, throw exception
        throw new FileNotFoundException($"Texture not found in any path: {relativePath}");
    }

    #endregion

    #region Texture Loading - Cubemap

    public Texture LoadSkyboxCubeMap(string[] faces)
    {
        ValidateCubemapFaces(faces);

        var handle = CreateCubemapHandle();
        BindCubemap(handle);

        LoadCubemapFaces(faces);
        ConfigureCubemapParameters();

        return CreateTextureObject(handle, string.Join(", ", faces));
    }

    private void ValidateCubemapFaces(string[] faces)
    {
        if (faces == null || faces.Length == 0)
            throw new ArgumentException("Cubemap faces array cannot be null or empty", nameof(faces));

        if (faces.Length != 6) throw new ArgumentException("Cubemap requires exactly 6 faces", nameof(faces));
    }

    private int CreateCubemapHandle()
    {
        return GL.GenTexture();
    }

    private void BindCubemap(int handle)
    {
        GL.BindTexture(TextureTarget.TextureCubeMap, handle);
    }

    private void LoadCubemapFaces(string[] faces)
    {
        for (var i = 0; i < faces.Length; i++) LoadCubemapFace(faces[i], i);
    }

    private void LoadCubemapFace(string relativePath, int faceIndex)
    {
        var fullPath = GetFullTexturePath(relativePath);

        if (!File.Exists(fullPath)) throw new FileNotFoundException($"Cubemap face not found: {fullPath}");

        try
        {
            StbImage.stbi_set_flip_vertically_on_load(0);

            using (Stream stream = File.OpenRead(fullPath))
            {
                var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

                GL.TexImage2D(
                    TextureTarget.TextureCubeMapPositiveX + faceIndex,
                    0,
                    PixelInternalFormat.Rgba,
                    image.Width,
                    image.Height,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    image.Data
                );
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load cubemap face {faceIndex}: {fullPath}", ex);
        }
    }

    private void ConfigureCubemapParameters()
    {
        // Set filtering
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter,
            (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Linear);

        // Set wrapping - ClampToEdge for cubemaps
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS,
            (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT,
            (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR,
            (int)TextureWrapMode.ClampToEdge);
    }

    #endregion
}