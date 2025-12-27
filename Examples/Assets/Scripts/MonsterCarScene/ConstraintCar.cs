using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;

namespace Project.Assets.Scripts.MonsterCarScene;

public class ConstraintCar
{
    public RigidBody GetCarBody() => _car;
    public RigidBody[] GetWheels() => _wheels;
    public float GetSteer() => _steer;
    
    private RigidBody _car = null!;
    private readonly RigidBody[] _damper = new RigidBody[4];
    private readonly RigidBody[] _wheels = new RigidBody[4];
    private readonly HingeJoint[] _sockets = new HingeJoint[4];
    private readonly PrismaticJoint[] _damperJoints = new PrismaticJoint[4];
    private readonly AngularMotor[] _steerMotor = new AngularMotor[2];

    private const int FrontLeft = 0;
    private const int FrontRight = 1;
    private const int BackLeft = 2;
    private const int BackRight = 3;

    private readonly float _maxSteerAngle;
    private float _steer;

    private readonly float _carWidth;
    private readonly float _carHeight;
    private readonly float _carLength;
    private readonly float _carMass;

    private readonly float _wheelRadius;
    private readonly float _wheelWidth;
    private readonly float _wheelMass;

    private readonly JVector[] _wheelPositions;
    private readonly float _damperMass;

    public ConstraintCar(
        float carWidth,
        float carHeight,
        float carLength,
        float carMass,
        float wheelRadius,
        float wheelWidth,
        float wheelMass,
        JVector[] wheelPositions,
        float damperMass = 0.1f,
        float maxSteerAngleDegrees = 40f)
    {
        if (wheelPositions.Length != 4)
            throw new ArgumentException("wheelPositions must have 4 elements!", nameof(wheelPositions));

        _carWidth = carWidth;
        _carHeight = carHeight;
        _carLength = carLength;
        _carMass = carMass;
        _wheelRadius = wheelRadius;
        _wheelWidth = wheelWidth;
        _wheelMass = wheelMass;
        _wheelPositions = wheelPositions;
        _damperMass = damperMass;
        _maxSteerAngle = maxSteerAngleDegrees;
    }

    public void BuildCar(World world, JVector carCenterPosition, Action<RigidBody> action = null)
    {
        var bodies = new List<RigidBody>(9);

        CreateCarBody(world, carCenterPosition, bodies);
        CreateWheelsAndDampers(world, bodies);
        CreateDamperJoints(world);
        CreateWheelHinges(world);
        SetupCollisionFilters(world);
        CreateSteeringMotors(world);

        action?.Invoke(_car);
        bodies.ForEach(b => action?.Invoke(b));
    }

    private void CreateCarBody(World world, JVector carCenterPosition, List<RigidBody> bodies)
    {
        _car = world.CreateRigidBody();
        bodies.Add(_car);

        var tfs1 = new TransformedShape(
            new BoxShape(_carWidth, _carHeight, _carLength),
            new JVector(0, -_carHeight * 1.4f, 0.0f));

        _car.AddShape(tfs1);
        _car.Position = carCenterPosition;
        _car.DeactivationTime = TimeSpan.MaxValue;
    }

    private void CreateWheelsAndDampers(World world, List<RigidBody> bodies)
    {
        for (var i = 0; i < 4; i++)
        {
            var wheelWorldPosition = _wheelPositions[i];
            var damperWorldPosition = wheelWorldPosition + JVector.UnitY * 1.1f;

            _damper[i] = world.CreateRigidBody();
            _damper[i].AddShape(new BoxShape(0.2f));
            _damper[i].SetMassInertia(_damperMass);
            _damper[i].Position = damperWorldPosition;

            _wheels[i] = world.CreateRigidBody();
            var shape = new CylinderShape(_wheelRadius, _wheelWidth);
            var tf = new TransformedShape(shape, JVector.Zero,
                JMatrix.CreateRotationZ(MathF.PI / 2.0f));

            _wheels[i].AddShape(tf);
            _wheels[i].SetMassInertia(_wheelMass);
            _wheels[i].Position = wheelWorldPosition;

            bodies.Add(_wheels[i]);
            bodies.Add(_damper[i]);
        }
    }

    private void CreateDamperJoints(World world)
    {
        for (var i = 0; i < 4; i++)
        {
            _damperJoints[i] = new PrismaticJoint(world, _car, _damper[i], _damper[i].Position,
                JVector.UnitY, LinearLimit.Fixed, false);

            _damperJoints[i].Slider.Softness = 0.005f;
            _damperJoints[i].Slider.LimitSoftness = 0.005f;
            _damperJoints[i].Slider.Bias = 0.05f;
            _damperJoints[i].Slider.LimitBias = 0.05f;
            _damperJoints[i].HingeAngle.LimitBias = 0.6f;
            _damperJoints[i].HingeAngle.LimitSoftness = 0.01f;
        }

        _damperJoints[FrontLeft].HingeAngle.Limit = AngularLimit.FromDegree(-_maxSteerAngle, _maxSteerAngle);
        _damperJoints[FrontRight].HingeAngle.Limit = AngularLimit.FromDegree(-_maxSteerAngle, _maxSteerAngle);
        _damperJoints[BackLeft].HingeAngle.Limit = AngularLimit.Fixed;
        _damperJoints[BackRight].HingeAngle.Limit = AngularLimit.Fixed;
    }

    private void CreateWheelHinges(World world)
    {
        for (var i = 0; i < 4; i++)
        {
            _sockets[i] = new HingeJoint(world, _damper[i], _wheels[i], _wheels[i].Position,
                JVector.UnitX, true);
        }
    }

    private void SetupCollisionFilters(World world)
    {
        if (world.BroadPhaseFilter is not CollisionHelper.IgnoreCollisionBetweenFilter filter)
        {
            filter = new CollisionHelper.IgnoreCollisionBetweenFilter();
            world.BroadPhaseFilter = filter;
        }

        for (var i = 0; i < 4; i++)
        {
            filter.IgnoreCollisionBetween(_car.Shapes[0], _damper[i].Shapes[0]);
            filter.IgnoreCollisionBetween(_wheels[i].Shapes[0], _damper[i].Shapes[0]);
            filter.IgnoreCollisionBetween(_car.Shapes[0], _wheels[i].Shapes[0]);
        }
    }

    private void CreateSteeringMotors(World world)
    {
        _steerMotor[FrontLeft] = world.CreateConstraint<AngularMotor>(_car, _damper[FrontLeft]);
        _steerMotor[FrontLeft].Initialize(JVector.UnitY);
        
        _steerMotor[FrontRight] = world.CreateConstraint<AngularMotor>(_car, _damper[FrontRight]);
        _steerMotor[FrontRight].Initialize(JVector.UnitY);
    }

    public void SetSteering(float value)
    {
        _steer = Math.Clamp(value, -1.0f, 1.0f);
    }

    public void AddSteering(float value)
    {
        _steer += value;
        _steer = Math.Clamp(_steer, -1.0f, 1.0f);
    }

    public void UpdateControls(float accelerate = 0.0f)
    {
        UpdateSteering();
        UpdateWheelMotors(accelerate);
    }

    private void UpdateSteering()
    {
        var targetAngle = _steer * _maxSteerAngle / 180.0f * MathF.PI;
        var currentAngleL = (float)_damperJoints[FrontLeft].HingeAngle.Angle;
        var currentAngleR = (float)_damperJoints[FrontRight].HingeAngle.Angle;

        var angleDiffL = targetAngle - currentAngleL;
        var angleDiffR = targetAngle - currentAngleR;

        UpdateSteeringMotor(_steerMotor[FrontLeft], angleDiffL);
        UpdateSteeringMotor(_steerMotor[FrontRight], angleDiffR);
    }

    private void UpdateSteeringMotor(AngularMotor motor, float angleDiff)
    {
        if (Math.Abs(angleDiff) > 0.001f)
        {
            motor.MaximumForce = 50.0f;
            motor.TargetVelocity = 5.0f * angleDiff;
        }
        else
        {
            motor.MaximumForce = 0.0f;
            motor.TargetVelocity = 0.0f;
        }
    }

    private void UpdateWheelMotors(float accelerate)
    {
        for (var i = 0; i < 4; i++)
        {
            _wheels[i].Friction = 0.8f;

            if (Math.Abs(accelerate) > 0.001f)
            {
                _sockets[i].Motor.MaximumForce = 500.0f;
                _sockets[i].Motor.TargetVelocity = 50.0f * accelerate;
            }
            else
            {
                _sockets[i].Motor.MaximumForce = 0.0f;
                _sockets[i].Motor.TargetVelocity = 0.0f;
            }
        }
    }

    public void Destroy(World world)
    {
        for (var i = 0; i < 4; i++)
        {
            world.Remove(_wheels[i]);
            world.Remove(_damper[i]);
        }

        world.Remove(_car);
    }

 
}