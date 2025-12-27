using System.Runtime.Serialization;
using Adapters;
using Engine.Rendering;
using Engine.Utilities;
using Newtonsoft.Json;
using OpenTK.Mathematics;

namespace Engine.Components;

/// <summary>
/// Represents the position, rotation, and scale of a GameObject in 3D space
/// Supports hierarchical transformations with parent-child relationships
/// </summary>
public sealed class Transform : Component
{
    #region Constants

    private const float RAD_TO_DEG = 180.0f / MathHelper.Pi;
    private const float DEG_TO_RAD = MathHelper.Pi / 180.0f;

    #endregion

    #region Direction Vectors

    [JsonIgnore] public Vector3 Forward => (_transform * new Vector4(0, 0, -1, 0)).Xyz.Normalized();

    [JsonIgnore] public Vector3 Up => (_transform * new Vector4(0, 1, 0, 0)).Xyz.Normalized();

    [JsonIgnore] public Vector3 Right => (_transform * new Vector4(1, 0, 0, 0)).Xyz.Normalized();

    #endregion

    #region Matrix Properties

    [JsonIgnore] public Matrix4 GetLocalTransformMatrix => _transform;

    #endregion

    #region Hierarchy

    [JsonIgnore] public Transform Parent { get; set; }

    [JsonIgnore] public List<Transform> Children { get; set; }

    [JsonProperty] public Guid _parentID;

    #endregion

    #region Serialized Fields

    [JsonIgnore] private Matrix4 _transform;

    [JsonProperty] private float[] _TransformElements { get; set; }

    [JsonProperty] private float[] _EulerAnglesElements { get; set; }

    #endregion

    #region Internal State

    private Vector3 _accumulatedEulerAngles = Vector3.Zero;

    #endregion
    
    #region Public Properties (Editor)

    public System.Numerics.Vector3 Position
    {
        get => LocalPosition.ToNumerics();
        set => LocalPosition = value.ToOpenTK();
    }

    public System.Numerics.Vector3 Rotation
    {
        get => EulerAngles.ToNumerics();
        set => EulerAngles = value.ToOpenTK();
    }

    public System.Numerics.Vector3 Scale
    {
        get => LocalScale.ToNumerics();
        set => LocalScale = value.ToOpenTK();
    }

    #endregion
    

    #region Local Transform Properties

    [JsonIgnore]
    public Vector3 LocalPosition
    {
        get => _transform.ExtractTranslation();
        set => UpdateTransform(value, LocalRotation, LocalScale);
    }

    [JsonIgnore]
    public Vector3 LocalScale
    {
        get => _transform.ExtractScale();
        set => UpdateTransform(LocalPosition, LocalRotation, value);
    }

    [JsonIgnore]
    public Quaternion LocalRotation
    {
        get => _transform.ExtractRotation();
        set
        {
            UpdateTransform(LocalPosition, value, LocalScale);
            _accumulatedEulerAngles = value.ToEulerAngles() * RAD_TO_DEG;
        }
    }

    [JsonIgnore]
    public Vector3 EulerAngles
    {
        get => _accumulatedEulerAngles;
        set
        {
            _accumulatedEulerAngles = value;
            var newRotation = Quaternion.FromEulerAngles(_accumulatedEulerAngles * DEG_TO_RAD);
            UpdateTransform(LocalPosition, newRotation, LocalScale);
        }
    }

    #endregion

    #region World Transform Properties

    [JsonIgnore]
    public Vector3 WorldPosition
    {
        get
        {
            if (Parent == null) return LocalPosition;

            return GetWorldTransformMatrix().ExtractTranslation();
        }
        set
        {
            if (Parent == null)
            {
                LocalPosition = value;
                return;
            }

            var parentWorldMatrix = Parent.GetWorldTransformMatrix();
            var parentWorldInverse = parentWorldMatrix.Inverted();
            var localPos = Vector3.TransformPosition(value, parentWorldInverse);
            LocalPosition = localPos;
        }
    }

    [JsonIgnore]
    public Vector3 PivotPosition
    {
        get
        {
            var renderer = GameObject.GetComponent<Renderer>();

            if (renderer == null) return WorldPosition;

            return renderer.GetWorldCenter();
        }
        set
        {
            var renderer = GameObject.GetComponent<Renderer>();

            if (renderer == null)
            {
                WorldPosition = value;
                return;
            }

            var moveOffset = value - renderer.GetWorldCenter();
            WorldPosition += moveOffset;
        }
    }

    [JsonIgnore]
    public Quaternion WorldRotation
    {
        get
        {
            if (Parent == null) return LocalRotation;

            var parentWorldRotation = Parent.WorldRotation;
            return parentWorldRotation * LocalRotation;
        }
        set
        {
            if (Parent == null)
            {
                LocalRotation = value;
                return;
            }

            var parentWorldRotation = Parent.WorldRotation;
            var localRotation = Quaternion.Invert(parentWorldRotation) * value;
            LocalRotation = localRotation;
        }
    }

    [JsonIgnore]
    public Vector3 WorldScale
    {
        get
        {
            if (Parent == null) return LocalScale;

            var parentWorldScale = Parent.WorldScale;
            return ComponentMultiply(LocalScale, parentWorldScale);
        }
        set
        {
            if (Parent == null)
            {
                LocalScale = value;
                return;
            }

            var parentWorldScale = Parent.WorldScale;
            LocalScale = ComponentDivide(value, parentWorldScale);
        }
    }

    #endregion

    #region Constructors

    public Transform()
    {
        _transform = Matrix4.CreateTranslation(Vector3.Zero);
        Children = new List<Transform>();
    }

    public Transform(Vector3 position, Vector3 scale, Quaternion rotation)
    {
        UpdateTransform(position, rotation, scale);
        Children = new List<Transform>();
    }

    public Transform(Matrix4 transform)
    {
        _transform = transform;
        _accumulatedEulerAngles = LocalRotation.ToEulerAngles() * RAD_TO_DEG;
        Children = new List<Transform>();
    }

    public Transform(Transform transform)
    {
        _transform = transform._transform;
        _accumulatedEulerAngles = LocalRotation.ToEulerAngles() * RAD_TO_DEG;
        Children = new List<Transform>();
    }

    #endregion

    #region Serialization

    [OnDeserialized]
    internal void OnDeserialized(StreamingContext context)
    {
        _transform = _TransformElements.ToMatrix4();
        _accumulatedEulerAngles = new Vector3(
            _EulerAnglesElements[0],
            _EulerAnglesElements[1],
            _EulerAnglesElements[2]
        );
        Children = new List<Transform>();
    }

    public override void PrepareSaveData()
    {
        base.PrepareSaveData();

        _TransformElements = _transform.GetSaveData().Elements;
        _EulerAnglesElements =
        [
            _accumulatedEulerAngles.X,
            _accumulatedEulerAngles.Y,
            _accumulatedEulerAngles.Z
        ];
        _parentID = Parent?.GameObject.ID ?? Guid.Empty;
    }

    #endregion

    #region Hierarchy Management

    public void SetParent(Transform parent)
    {
        Parent?.RemoveChild(this);

        if (parent != null) parent.AddChild(this);
    }

    private void AddChild(Transform child)
    {
        ValidateChildOperation(child, "add");

        if (Children.Contains(child)) throw new InvalidOperationException("Child already exists in the GameObject.");

        Children.Add(child);
        child.Parent = this;
    }

    public void RemoveChild(Transform child)
    {
        ValidateChildOperation(child, "remove");

        if (!Children.Contains(child)) throw new InvalidOperationException("Child does not exist in the GameObject.");

        Children.Remove(child);
        child.Parent = null;
    }

    private void ValidateChildOperation(Transform child, string operation)
    {
        if (child == null) throw new ArgumentNullException(nameof(child), $"Cannot {operation} null child");
    }

    public Transform FindChild(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        foreach (var child in Children)
        {
            if (child.GameObject.Name == name) return child;

            var foundInDescendant = child.FindChild(name);
            if (foundInDescendant != null) return foundInDescendant;
        }

        return null;
    }

    #endregion

    #region Transform Operations

    public void LookAt(Vector3 target, Vector3 up)
    {
        var newRotation = Matrix4.LookAt(LocalPosition, target, up).ExtractRotation();
        LocalRotation = newRotation;
    }

    public Matrix4 GetWorldTransformMatrix()
    {
        if (Parent == null) return GetLocalTransformMatrix;

        return _transform * Parent.GetWorldTransformMatrix();
    }

    #endregion

    #region Static Transform Utility Methods

    /// <summary>
    /// Transforms a point from local space to world space using a transform matrix.
    /// </summary>
    /// <param name="localPoint">Point in local space</param>
    /// <param name="transformMatrix">Transform matrix to apply</param>
    /// <returns>Point in world space</returns>
    public static Vector3 TransformPoint(Vector3 localPoint, Matrix4 transformMatrix)
    {
        var point = new Vector4(localPoint, 1.0f);
        var transformed = transformMatrix * point;
        return transformed.Xyz;
    }

    /// <summary>
    /// Transforms a direction vector from local space to world space using a transform matrix.
    /// Ignores translation component (W = 0).
    /// </summary>
    /// <param name="localDirection">Direction in local space</param>
    /// <param name="transformMatrix">Transform matrix to apply</param>
    /// <returns>Direction in world space</returns>
    public static Vector3 TransformDirection(Vector3 localDirection, Matrix4 transformMatrix)
    {
        var direction = new Vector4(localDirection, 0.0f);
        var transformed = transformMatrix * direction;
        return transformed.Xyz;
    }

    /// <summary>
    /// Transforms a normal vector from local space to world space using a normal matrix.
    /// Uses the inverse transpose of the transform matrix to handle non-uniform scaling correctly.
    /// </summary>
    /// <param name="localNormal">Normal in local space</param>
    /// <param name="transformMatrix">Transform matrix</param>
    /// <returns>Normal in world space (normalized)</returns>
    public static Vector3 TransformNormal(Vector3 localNormal, Matrix4 transformMatrix)
    {
        var normalMatrix = Matrix4.Transpose(Matrix4.Invert(transformMatrix));
        var normal = new Vector4(localNormal, 0.0f);
        var transformed = normalMatrix * normal;
        return Vector3.Normalize(transformed.Xyz);
    }

    /// <summary>
    /// Inverse transforms a point from world space to local space using a transform matrix.
    /// </summary>
    /// <param name="worldPoint">Point in world space</param>
    /// <param name="transformMatrix">Transform matrix</param>
    /// <returns>Point in local space</returns>
    public static Vector3 InverseTransformPoint(Vector3 worldPoint, Matrix4 transformMatrix)
    {
        var inverseMatrix = Matrix4.Invert(transformMatrix);
        return TransformPoint(worldPoint, inverseMatrix);
    }

    /// <summary>
    /// Inverse transforms a direction from world space to local space using a transform matrix.
    /// </summary>
    /// <param name="worldDirection">Direction in world space</param>
    /// <param name="transformMatrix">Transform matrix</param>
    /// <returns>Direction in local space</returns>
    public static Vector3 InverseTransformDirection(Vector3 worldDirection, Matrix4 transformMatrix)
    {
        var inverseMatrix = Matrix4.Invert(transformMatrix);
        return TransformDirection(worldDirection, inverseMatrix);
    }

    #endregion

    #region Instance Transform Methods

    /// <summary>
    /// Transforms a point from local space to world space using this transform.
    /// </summary>
    public Vector3 TransformPoint(Vector3 localPoint)
    {
        return TransformPoint(localPoint, GetWorldTransformMatrix());
    }

    /// <summary>
    /// Transforms a direction from local space to world space using this transform.
    /// </summary>
    public Vector3 TransformDirection(Vector3 localDirection)
    {
        return TransformDirection(localDirection, GetWorldTransformMatrix());
    }

    /// <summary>
    /// Transforms a normal from local space to world space using this transform.
    /// </summary>
    public Vector3 TransformNormal(Vector3 localNormal)
    {
        return TransformNormal(localNormal, GetWorldTransformMatrix());
    }

    /// <summary>
    /// Inverse transforms a point from world space to local space using this transform.
    /// </summary>
    public Vector3 InverseTransformPoint(Vector3 worldPoint)
    {
        return InverseTransformPoint(worldPoint, GetWorldTransformMatrix());
    }

    /// <summary>
    /// Inverse transforms a direction from world space to local space using this transform.
    /// </summary>
    public Vector3 InverseTransformDirection(Vector3 worldDirection)
    {
        return InverseTransformDirection(worldDirection, GetWorldTransformMatrix());
    }

    #endregion

    #region Private Helper Methods

    private void UpdateTransform(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        _transform = Matrix4.CreateScale(scale) *
                     Matrix4.CreateFromQuaternion(rotation) *
                     Matrix4.CreateTranslation(position);
    }

    private Vector3 ComponentMultiply(Vector3 a, Vector3 b)
    {
        return new Vector3(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
    }

    private Vector3 ComponentDivide(Vector3 a, Vector3 b)
    {
        return new Vector3(a.X / b.X, a.Y / b.Y, a.Z / b.Z);
    }

    #endregion
}