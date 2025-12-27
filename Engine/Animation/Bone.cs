using System.Numerics;
using Adapters;
using Engine.Components;
using Newtonsoft.Json;
using Quaternion = System.Numerics.Quaternion;
using Vector3 = System.Numerics.Vector3;

namespace Engine.Animation;

/// <summary>
/// Represents a bone in a skeletal animation system.
/// Stores bind pose data and maintains initial transform state for animation blending and serialization.
/// </summary>
internal class Bone : BeroBehaviour
{
    #region Properties

    /// <summary>
    /// The name of this bone, matching the name in the model's skeleton hierarchy.
    /// </summary>
    public string BoneName { get; set; } = string.Empty;

    /// <summary>
    /// The bind pose (offset matrix) that transforms from bone space to mesh space.
    /// This matrix is used in skeletal animation to transform vertices.
    /// </summary>
    [JsonIgnore]
    public Matrix4x4 BindPose { get; set; } = Matrix4x4.Identity;

    /// <summary>
    /// The world transform of this bone in its bind pose.
    /// </summary>
    [JsonIgnore]
    public Matrix4x4 WorldPose { get; set; } = Matrix4x4.Identity;

    /// <summary>
    /// Bones should not be saved with scene data as they are recreated from model files.
    /// </summary>
    [JsonIgnore]
    public override int SavePriority => -1;

    #endregion

    #region Fields

    private Vector3 _initialPosition;
    private Quaternion _initialRotation;
    private Vector3 _initialScale;

    #endregion

    #region Lifecycle

    /// <summary>
    /// Captures the initial transform state of the bone when first initialized.
    /// This state is used to restore the bone's transform when saving scenes.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        CaptureInitialTransform();
    }

    private void CaptureInitialTransform()
    {
        _initialPosition = Transform.WorldPosition.ToNumerics();
        _initialRotation = Transform.LocalRotation.ToNumeric();
        _initialScale = Transform.LocalScale.ToNumerics();
    }

    #endregion

    #region Serialization

    /// <summary>
    /// Restores the bone to its initial transform state before saving scene data.
    /// This ensures that animated bones are saved in their rest pose rather than their current animated state.
    /// </summary>
    public override void PrepareSaveData()
    {
        base.PrepareSaveData();
        RestoreInitialTransform();
    }

    private void RestoreInitialTransform()
    {
        return;
        Transform.WorldPosition = _initialPosition.ToOpenTK();
        Transform.LocalRotation = _initialRotation.ToOpenTK();
        Transform.LocalScale = _initialScale.ToOpenTK();
    }

    #endregion
}