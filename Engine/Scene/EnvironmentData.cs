using System.Numerics;
using Engine.Shaders;

namespace Engine.Scene;

/// <summary>
/// Contains all environment settings including lighting, fog, and skybox configuration.
/// Used to store and transfer environment state.
/// </summary>
public class EnvironmentData
{
    #region Lighting Properties

    /// <summary>
    /// The ambient light color and intensity for the scene.
    /// </summary>
    public Vector4 AmbientColor { get; set; }

    #endregion

    #region Fog Properties

    /// <summary>
    /// Gets or sets whether fog is enabled in the scene.
    /// </summary>
    public bool UseFog { get; set; }

    /// <summary>
    /// Gets or sets the fog color.
    /// </summary>
    public Vector4 FogColor { get; set; }

    /// <summary>
    /// Gets or sets the distance at which fog starts to appear.
    /// </summary>
    public float FogStart { get; set; }

    /// <summary>
    /// Gets or sets the distance at which fog reaches maximum density.
    /// </summary>
    public float FogEnd { get; set; }

    /// <summary>
    /// The skybox instance.
    /// </summary>

    public ShaderBase SkyboxShader { get; set; }

    #endregion


    #region Constructor

    /// <summary>
    /// Creates a new EnvironmentData with default values.
    /// </summary>
    public EnvironmentData()
    {
        AmbientColor = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);
        UseFog = false;
        FogColor = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);
        FogStart = 10.0f;
        FogEnd = 150.0f;
        SkyboxShader = null;
    }

    #endregion
}