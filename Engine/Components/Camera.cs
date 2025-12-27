using Engine.Core;
using OpenTK.Mathematics;

namespace Engine.Components;

/// <summary>
/// Represents a camera in the scene that defines the viewpoint for rendering.
/// Supports both perspective and orthographic projection modes.
/// </summary>
public class Camera : Component
{
    #region Enumerations

    /// <summary>
    /// Defines how the camera clears the background before rendering.
    /// </summary>
    public enum ClearFlags
    {
        /// <summary>Clears to a solid color.</summary>
        SolidColor,

        /// <summary>Renders a skybox as the background.</summary>
        Skybox
    }

    /// <summary>
    /// Defines the camera's projection mode.
    /// </summary>
    public enum ProjectionType
    {
        /// <summary>Perspective projection with depth perception.</summary>
        Perspective,

        /// <summary>Orthographic projection without perspective distortion.</summary>
        Orthographic
    }

    #endregion

    #region Camera Properties

    /// <summary>
    /// The near clipping plane distance.
    /// Objects closer than this distance will not be rendered.
    /// </summary>
    public float Near = 0.01f;

    /// <summary>
    /// The far clipping plane distance.
    /// Objects further than this distance will not be rendered.
    /// </summary>
    public float Far = 1000f;

    /// <summary>
    /// The vertical size of the orthographic view volume.
    /// Only used when projection type is Orthographic.
    /// </summary>
    public float OrthoSize { get; set; } = 60f;

    /// <summary>
    /// Determines how the camera clears the background.
    /// </summary>
    public ClearFlags ClearFlag { get; set; } = ClearFlags.SolidColor;

    /// <summary>
    /// The color used to clear the background when ClearFlag is SolidColor.
    /// </summary>
    public Color4 ClearColor { get; set; } = Color4.CornflowerBlue;

    /// <summary>
    /// The type of projection (Perspective or Orthographic).
    /// </summary>
    public ProjectionType Projection { get; set; } = ProjectionType.Perspective;

    /// <summary>
    /// The vertical field of view in degrees for perspective projection.
    /// </summary>
    public float FieldOfView { get; set; } = MathHelper.RadiansToDegrees(MathHelper.PiOver2);

    /// <summary>
    /// The aspect ratio (width / height) of the camera viewport.
    /// </summary>
    public float AspectRatio { get; set; }

    #endregion

    #region View Matrix

    /// <summary>
    /// Calculates and returns the view matrix for this camera.
    /// The view matrix transforms world space coordinates to camera space.
    /// </summary>
    /// <returns>The view matrix.</returns>
    public Matrix4 GetViewMatrix()
    {
        var cameraPosition = Transform.LocalPosition;
        var targetPosition = cameraPosition + Transform.Forward;
        var upDirection = Transform.Up;

        return Matrix4.LookAt(cameraPosition, targetPosition, upDirection);
    }

    #endregion

    #region Projection Matrix

    /// <summary>
    /// Calculates and returns the appropriate projection matrix based on the current projection type.
    /// </summary>
    /// <returns>The projection matrix (perspective or orthographic).</returns>
    public Matrix4 GetProjectionMatrix()
    {
        return Projection == ProjectionType.Orthographic
            ? GetOrthographicProjectionMatrix()
            : GetPerspectiveProjectionMatrix();
    }

    /// <summary>
    /// Calculates the orthographic projection matrix.
    /// </summary>
    /// <returns>The orthographic projection matrix.</returns>
    private Matrix4 GetOrthographicProjectionMatrix()
    {
        var orthoHeight = OrthoSize;
        var orthoWidth = OrthoSize * AspectRatio;

        return Matrix4.CreateOrthographic(orthoWidth, orthoHeight, Near, Far);
    }

    /// <summary>
    /// Calculates the perspective projection matrix.
    /// </summary>
    /// <returns>The perspective projection matrix.</returns>
    private Matrix4 GetPerspectiveProjectionMatrix()
    {
        var fieldOfViewRadians = MathHelper.DegreesToRadians(FieldOfView);
        var aspectRatio = Screen.GetAspectRatio();

        return Matrix4.CreatePerspectiveFieldOfView(fieldOfViewRadians, aspectRatio, Near, Far);
    }

    #endregion

    #region Raycasting

    /// <summary>
    /// Calculates a ray in world space from the camera through the current mouse position.
    /// Used for mouse picking and interaction with 3D objects.
    /// </summary>
    /// <returns>A normalized direction vector representing the ray from the camera through the mouse cursor.</returns>
    public Vector3 GetCameraRay()
    {
        var viewportNdc = Screen.GetViewportNdc();

        // Convert NDC to clip space
        var nearPointClipSpace = ConvertNdcToClipSpace(viewportNdc, true);
        var farPointClipSpace = ConvertNdcToClipSpace(viewportNdc, false);

        // Transform to world space
        var inverseViewProjection = CalculateInverseViewProjection();
        var nearPointWorld = TransformToWorldSpace(nearPointClipSpace, inverseViewProjection);
        var farPointWorld = TransformToWorldSpace(farPointClipSpace, inverseViewProjection);

        // Calculate and return ray direction
        return CalculateRayDirection(nearPointWorld, farPointWorld);
    }

    /// <summary>
    /// Converts normalized device coordinates to clip space coordinates.
    /// </summary>
    private Vector4 ConvertNdcToClipSpace(Vector2 ndcCoords, bool atNearPlane)
    {
        // NDC: (0,0) at bottom-left, (1,1) at top-right
        // Clip space: (-1,-1) at bottom-left, (1,1) at top-right
        var clipX = ndcCoords.X * 2.0f - 1.0f;
        var clipY = ndcCoords.Y * 2.0f - 1.0f;
        var clipZ = atNearPlane ? -1.0f : 1.0f;

        return new Vector4(clipX, clipY, clipZ, 1.0f);
    }

    /// <summary>
    /// Calculates the inverse of the view-projection matrix.
    /// </summary>
    private Matrix4 CalculateInverseViewProjection()
    {
        var viewMatrix = GetViewMatrix();
        var projectionMatrix = GetProjectionMatrix();
        var viewProjectionMatrix = viewMatrix * projectionMatrix;

        return viewProjectionMatrix.Inverted();
    }

    /// <summary>
    /// Transforms a point from clip space to world space using perspective division.
    /// </summary>
    private Vector3 TransformToWorldSpace(Vector4 clipSpacePoint, Matrix4 inverseViewProjection)
    {
        var worldSpacePoint = clipSpacePoint * inverseViewProjection;

        // Perform perspective division
        worldSpacePoint /= worldSpacePoint.W;

        return worldSpacePoint.Xyz;
    }

    /// <summary>
    /// Calculates the normalized ray direction from the near point to the far point.
    /// </summary>
    private Vector3 CalculateRayDirection(Vector3 nearPoint, Vector3 farPoint)
    {
        var rayDirection = farPoint - nearPoint;
        return rayDirection.Normalized();
    }

    #endregion
}