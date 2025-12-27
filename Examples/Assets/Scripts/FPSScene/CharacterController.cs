using Engine.Components;
using Engine.Core;
using OpenTK.Mathematics;

namespace Project.Assets.Scripts.FPSScene;

public class CharacterController
{
    private readonly Rigidbody _body;

    public float Radius = 0.5f;
    public float Lenght = 2;

    private readonly float _moveSpeed = 7f;
    private readonly float _jumpForce = 5.7f;
    private readonly float _friction = 2.4f;
    private readonly float _rotationSpeed = 180f;

    private float _eulerAngles;
    private readonly Vector3 _startPosition = new(0, 1.8f, -3);

    public CharacterController(Rigidbody body)
    {
        _body = body;
        _body.Friction = _friction;
        _eulerAngles = _body.PhysicsBody.Orientation.ToEulerAngles().Y;
        body.Position = _startPosition;
    }

    public void Move(Vector3 inputDir)
    {
        if (MathF.Abs(inputDir.X) > 0.001f) _eulerAngles += -inputDir.X * _rotationSpeed * Time.DeltaTime;

        if (MathF.Abs(inputDir.Z) > 0.001f)
        {
            var forward = Vector3.Transform(Vector3.UnitZ, _body.PhysicsBody.Orientation);
            forward.Y = 0;
            forward = forward.Normalized();

            var desiredVelocity = forward * (inputDir.Z * _moveSpeed);
            var currentVel = _body.Velocity;

            desiredVelocity.Y = currentVel.Y;
            _body.Velocity = desiredVelocity;
        }
        else
        {
            var vel = _body.Velocity;
            vel.X = 0;
            vel.Z = 0;
            _body.Velocity = vel;
        }

        _body.AngularVelocity = Vector3.Zero;
        _body.PhysicsBody.Orientation = Quaternion.FromEulerAngles(0, MathHelper.DegreesToRadians(_eulerAngles), 0);
    }

    public void Jump()
    {
        var vel = _body.Velocity;
        vel.Y = _jumpForce;
        _body.Velocity = vel;
    }
}