using Adapters;
using Engine.Components;
using Engine.Core;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Project.Assets.Scripts.Utility;

namespace Project.Assets.Scripts.FPSScene;

public class FPSSceneController : BeroBehaviour
{
    private TargetFollower _targetFollower;

    private Vector3 _zoomTargetOffset = new(-1.6f, 1, 3.3f);
    private Vector3 _followTargetOffset = new(6.75f, 5.6f, -3.25f);

    protected override void Awake()
    {
        base.Awake();

        _targetFollower = GetComponent<TargetFollower>();

        if (_targetFollower == null) return;

        _targetFollower.FollowOffset = _zoomTargetOffset.ToNumerics();
        _targetFollower.ForceToSnap();
    }

    public override void Update()
    {
        base.Update();

        if (_targetFollower == null) return;

        if (Input.Keybord.IsKeyDown(Keys.Q)) _targetFollower.FollowOffset = _followTargetOffset.ToNumerics();
    }
}