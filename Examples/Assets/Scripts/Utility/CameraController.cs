using Engine.Components;
using Engine.Core;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Project.Assets.Scripts.Utility;

public class CameraController : BeroBehaviour
{
    protected Camera TargetCamera;
    
    private Vector3 _rotation;
    private bool _firstMove = true;

    public float CameraSpeed = 7.5f;
    public float Sensitivity = 0.5f;

    protected override void Awake()
    {
        base.Awake();

        TargetCamera = GameObject.GetComponent<Camera>();
    }

    public override void Update()
    {
        base.Update();

        if (TargetCamera == null)
            return;

        HandleMovement();
        HandleRotation();
    }

    private void HandleMovement()
    {
        var deltaTime = Time.DeltaTime;
        var speed = CameraSpeed * deltaTime;

        if (Input.Keybord.IsKeyDown(Keys.W))
            TargetCamera.Transform.WorldPosition += TargetCamera.Transform.Forward * speed;

        if (Input.Keybord.IsKeyDown(Keys.S))
            TargetCamera.Transform.WorldPosition -= TargetCamera.Transform.Forward * speed;

        if (Input.Keybord.IsKeyDown(Keys.A))
            TargetCamera.Transform.WorldPosition -= TargetCamera.Transform.Right * speed;

        if (Input.Keybord.IsKeyDown(Keys.D))
            TargetCamera.Transform.WorldPosition += TargetCamera.Transform.Right * speed;

        if (Input.Keybord.IsKeyDown(Keys.Space))
            TargetCamera.Transform.WorldPosition += TargetCamera.Transform.Up * speed;

        if (Input.Keybord.IsKeyDown(Keys.LeftShift))
            TargetCamera.Transform.WorldPosition -= TargetCamera.Transform.Up * speed;
    }

    private void HandleRotation()
    {
        if (Input.Mouse.IsButtonDown(MouseButton.Right))
        {
            if (_firstMove)
            {
                _rotation = TargetCamera.Transform.EulerAngles;
                _firstMove = false;
            }
            else
            {
                UpdateRotation();
            }
        }

        if (Input.Mouse.IsButtonReleased(MouseButton.Right))
            _firstMove = true;
    }

    private void UpdateRotation()
    {
        var deltaTime = Time.DeltaTime;

        _rotation.Y += Input.Mouse.Delta.X * Sensitivity;
        _rotation.X += Input.Mouse.Delta.Y * Sensitivity;

        var target = Vector3.Slerp(TargetCamera.Transform.EulerAngles, _rotation, 25f * deltaTime);
        TargetCamera.Transform.EulerAngles = target;
    }
}