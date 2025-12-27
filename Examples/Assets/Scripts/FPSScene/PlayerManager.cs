using Engine.Animation;
using Engine.Components;
using Engine.Core;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Project.Assets.Scripts.FPSScene;

public class PlayerManager : BeroBehaviour
{
    private AnimationController _animationController;
    private CharacterController _characterController;

    protected override void Start()
    {
        base.Start();

        var skinnedMeshRenderer = GameObject.GetComponentInChildren<SkinnedMeshRenderer>();
        var animator = GameObject.GetComponentInChildren<Animator>();
        var _rigidbody = GameObject.GetComponent<Rigidbody>();

        if (skinnedMeshRenderer == null || animator == null || _rigidbody == null) return;

        _animationController = new AnimationController();
        _animationController.InitAnimationController(animator, skinnedMeshRenderer.RootNode);
        _animationController.PlayIdleAnimation();

        _characterController = new CharacterController(_rigidbody);
        _rigidbody.PhysicsBody.AddCapsuleShape(_characterController.Radius, _characterController.Lenght);
    }

    public override void Update()
    {
        base.Update();

        UpdateAnimations();
        UpdateCharacterController();
    }

    private void UpdateCharacterController()
    {
        var inputDir = new Vector3(0, 0, 0);

        if (Input.Keybord.IsKeyDown(Keys.W)) inputDir.Z += 1;

        if (Input.Keybord.IsKeyDown(Keys.S)) inputDir.Z -= 1;

        if (Input.Keybord.IsKeyDown(Keys.A)) inputDir.X -= 1;

        if (Input.Keybord.IsKeyDown(Keys.D)) inputDir.X += 1;

        if (inputDir.LengthSquared > 0) inputDir = inputDir.Normalized();

        _characterController.Move(inputDir);

        if (Input.Keybord.IsKeyPressed(Keys.Space)) _characterController.Jump();
    }

    private void UpdateAnimations()
    {
        if (Input.Keybord.IsKeyPressed(Keys.W)) _animationController.PlayWalkAnimation();

        if (Input.Keybord.IsKeyReleased(Keys.W)) _animationController.PlayIdleAnimation();

        if (Input.Keybord.IsKeyPressed(Keys.Space)) _animationController.PlayJumpAnimation();

        if (Input.Keybord.IsKeyPressed(Keys.E)) _animationController.PlayFloatAnimation();

        if (Input.Keybord.IsKeyPressed(Keys.R)) _animationController.PlayRollAnimation();

        _animationController.Update(Time.DeltaTime);
    }
}