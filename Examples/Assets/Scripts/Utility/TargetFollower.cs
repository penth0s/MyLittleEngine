using Adapters;
using Engine.Components;
using Engine.Core;
using Engine.Systems;
using OpenTK.Mathematics;

namespace Project.Assets.Scripts.Utility;

public class TargetFollower : BeroBehaviour
{
    private GameObject _targetObject;

    public string TargetName = "";
    public System.Numerics.Vector3 FollowOffset = new(0, 5, -10);
    public float FollowSpeed = 5f;
    public float RotationSpeed = 10f;

    protected override void Start()
    {
        base.Start();

        FindTarget();
    }

    public override void Update()
    {
        base.Update();

        if (!IsTargetValid())
        {
            _targetObject = null;
            FindTarget();
            return;
        }

        FollowTarget();
    }

    public void ForceToSnap()
    {
        if (_targetObject == null)
            return;

        var targetPosition = _targetObject.Transform.WorldPosition + FollowOffset.ToOpenTK();
        Transform.WorldPosition = targetPosition;

        var targetRotation = CalculateTargetRotation();
        Transform.WorldRotation = targetRotation;
    }

    private void FindTarget()
    {
        var sceneSystem = SystemManager.GetSystem<SceneSystem>();
        _targetObject = sceneSystem.CurrentScene.GetGameObjectByName(TargetName);
    }

    private bool IsTargetValid()
    {
        return _targetObject != null && _targetObject.IsActive;
    }

    private void FollowTarget()
    {
        var targetPosition = _targetObject.Transform.WorldPosition + FollowOffset.ToOpenTK();
        var newPosition = Vector3.Lerp(
            Transform.WorldPosition,
            targetPosition,
            FollowSpeed * Time.DeltaTime
        );

        Transform.WorldPosition = newPosition;

        UpdateRotation();
    }

    private void UpdateRotation()
    {
        var targetRotation = CalculateTargetRotation();
        var newRotation = Quaternion.Slerp(
            Transform.WorldRotation, 
            targetRotation, 
            Time.DeltaTime * RotationSpeed
        );

        Transform.WorldRotation = newRotation;
    }

    private Quaternion CalculateTargetRotation()
    {
        return Matrix4
            .LookAt(Transform.WorldPosition, _targetObject.Transform.WorldPosition, Vector3.UnitY)
            .ExtractRotation();
    }
}

