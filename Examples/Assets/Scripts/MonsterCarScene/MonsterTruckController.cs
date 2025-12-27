using Adapters;
using Engine.Components;
using Engine.Core;
using Engine.Database.Implementations;
using Engine.Rendering;
using Engine.Scripts;
using Engine.Systems;
using Jitter2.LinearMath;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using PhysicsEngine;
using PhysicsEngine.Core;

namespace Project.Assets.Scripts.MonsterCarScene;

public class MonsterTruckController : BeroBehaviour
{
    private ConstraintCar _car;
    private Transform _body;
    private List<Transform> _wheels = new();
    private Material _monsterTruckMaterial;

    private readonly string _bodyName = "Monster_Truck_12A_Body";
    private readonly string _wheelFLName = "Wheel_12A_F.L";
    private readonly string _wheelFRName = "Wheel_12A_F.R";
    private readonly string _wheelRLName = "Wheel_12A_R.L";
    private readonly string _wheelRRName = "Wheel_12A_R.R";

    private readonly float _steerSpeed = 2.0f;
    private readonly float _steerReturn = 5.0f;
    private readonly Vector3 _centerOffset = new(0, 1.1f, 0);
    private readonly Vector3 _initialPosition = Vector3.UnitY;

    public float Acceleration = 0.25f;

    protected override void Start()
    {
        base.Start();

        Transform.WorldPosition = _initialPosition;

        if (!InitializeTransforms())
            return;

        Print("Monster Truck parts found. Initializing physics...");

        InitializePhysics();
    }

    private bool InitializeTransforms()
    {
        _body = Transform.FindChild(_bodyName);
        var wheelFL = Transform.FindChild(_wheelFLName);
        var wheelFR = Transform.FindChild(_wheelFRName);
        var wheelRL = Transform.FindChild(_wheelRLName);
        var wheelRR = Transform.FindChild(_wheelRRName);

        if (_body == null || wheelFL == null || wheelFR == null || wheelRL == null || wheelRR == null)
            return false;

        _wheels.Add(wheelFL);
        _wheels.Add(wheelFR);
        _wheels.Add(wheelRL);
        _wheels.Add(wheelRR);

        return true;
    }

    private void InitializePhysics()
    {
        JVector[] wheelPositions =
        [
            GetRelativeWheelPosition(_wheels[0]).ToJitter(),
            GetRelativeWheelPosition(_wheels[1]).ToJitter(),
            GetRelativeWheelPosition(_wheels[2]).ToJitter(),
            GetRelativeWheelPosition(_wheels[3]).ToJitter()
        ];

        _car = new ConstraintCar(
            carWidth: 2.0f,
            carHeight: 1.2f,
            carLength: 5.0f,
            carMass: 500,
            wheelRadius: 0.3f,
            wheelWidth: 1f,
            wheelMass: 50.0f,
            wheelPositions: wheelPositions,
            damperMass: 25.0f,
            maxSteerAngleDegrees: 60f
        );

        HideOriginalWheels();

        var centerPos = GetWheelsCenterPosition() + _centerOffset;
        _car.BuildCar(PhysicsManager.GetWorld, centerPos.ToJitter());
    }

    private void HideOriginalWheels()
    {
        foreach (var wheel in _wheels)
        {
            var renderer = wheel.Transform.Parent.GameObject.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.Enabled = false;
        }
    }

    private Vector3 GetRelativeWheelPosition(Transform wheel)
    {
        return wheel.Transform.WorldPosition - _body.Transform.WorldPosition;
    }

    private Vector3 GetWheelsCenterPosition()
    {
        var center = Vector3.Zero;
        foreach (var wheel in _wheels)
            center += wheel.Transform.WorldPosition;
        
        return center / _wheels.Count;
    }

    public override void Update()
    {
        base.Update();

        HandleInput();
        SyncPhysics();
    }

    private void HandleInput()
    {
        var dt = Time.DeltaTime;

        HandleSteering(dt);
        HandleAcceleration();
    }

    private void HandleSteering(float dt)
    {
        if (Input.Keybord.IsKeyDown(Keys.Left))
        {
            _car.AddSteering(_steerSpeed * dt);
        }
        else if (Input.Keybord.IsKeyDown(Keys.Right))
        {
            _car.AddSteering(-_steerSpeed * dt);
        }
        else
        {
            ReturnSteeringToCenter(dt);
        }
    }

    private void ReturnSteeringToCenter(float dt)
    {
        var currentSteer = _car.GetSteer();
        if (Math.Abs(currentSteer) > 0.01f)
        {
            var returnAmount = _steerReturn * dt;
            if (currentSteer > 0)
                _car.AddSteering(-Math.Min(returnAmount, currentSteer));
            else
                _car.AddSteering(Math.Min(returnAmount, -currentSteer));
        }
        else
        {
            _car.SetSteering(0);
        }
    }

    private void HandleAcceleration()
    {
        var accelerate = 0.0f;

        if (Input.Keybord.IsKeyDown(Keys.Up))
            accelerate = Acceleration;
        else if (Input.Keybord.IsKeyDown(Keys.Down))
            accelerate = -Acceleration;

        if (Input.Keybord.IsKeyDown(Keys.RightShift))
            accelerate *= 6;

        _car.UpdateControls(accelerate);
    }

    private void SyncPhysics()
    {
        var bodyBody = _car.GetCarBody();
        _body.WorldPosition = bodyBody.Position.ToOpenTK();
        _body.Transform.WorldRotation = bodyBody.Orientation.ToOpenTK();

        for (var i = 0; i < _car.GetWheels().Length; i++)
        {
            var wheelBody = _car.GetWheels()[i];
            var wheelTransform = _wheels[i];

            wheelTransform.Transform.WorldPosition = wheelBody.Position.ToOpenTK();
            wheelTransform.Transform.WorldRotation = wheelBody.Orientation.ToOpenTK();
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        _car?.Destroy(PhysicsManager.GetWorld);
    }
}