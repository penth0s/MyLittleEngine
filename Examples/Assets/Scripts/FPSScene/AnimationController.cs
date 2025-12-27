using Assimp;
using Engine.Animation;

namespace Project.Assets.Scripts.FPSScene;

public class AnimationController
{
    private Animator _animator;

    private const float BlendTime = 0.15f;
    private const string WalkAnimation = "Running";
    private const string IdleAnimation = "Idle";
    private const string JumpAnimation = "Jump";
    private const string FloatAnimation = "Floating";
    private const string RollAnimation = "Roll";

    public void InitAnimationController(Animator animator, Node root)
    {
        _animator = animator;
        _animator.InitAnimator(root,
            [WalkAnimation, IdleAnimation, JumpAnimation, FloatAnimation, RollAnimation]);
    }

    public void Update(double deltaTime)
    {
        _animator.Update(deltaTime);
    }

    public void PlayWalkAnimation()
    {
        _animator.PlayAnimation(WalkAnimation, BlendTime);
    }

    public void PlayIdleAnimation()
    {
        _animator.PlayAnimation(IdleAnimation, BlendTime);
    }

    public void PlayJumpAnimation()
    {
        _animator.PlayAnimation(JumpAnimation, BlendTime, WalkAnimation);
    }

    public void PlayFloatAnimation()
    {
        _animator.PlayAnimation(FloatAnimation, BlendTime);
    }

    public void PlayRollAnimation()
    {
        _animator.PlayAnimation(RollAnimation, BlendTime, WalkAnimation);
    }
}