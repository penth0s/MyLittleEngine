using Adapters;
using Assimp;
using Engine.Components;
using Engine.Core;
using Engine.Database.Implementations;
using Engine.Systems;
using OpenTK.Mathematics;

namespace Engine.Animation;

/// <summary>
/// Handles skeletal animation playback and blending for 3D models.
/// Supports smooth transitions between animations using linear blending.
/// </summary>
public sealed class Animator : Component
{
    #region Constants

    private const float DEFAULT_TICKS_PER_SECOND = 60.0f;

    #endregion

    #region Fields

    private static readonly Dictionary<string, Assimp.Animation> _animationCache = new();
    private readonly List<Bone> _rigBones = new();

    private Assimp.Animation _currentAnimation;
    private double _currentAnimationTime;

    private Assimp.Animation _targetAnimation;
    private double _targetAnimationTime;
    private double _blendDuration;
    private double _blendTimer;

    private string _nextAnimationName;
    private double _nextAnimationBlendDuration;

    private Node _rootNode;

    #endregion

    #region Initialization

    public void InitAnimator(Node rootNode, List<string> animationNames)
    {
        _rootNode = rootNode;
        LoadAnimations(animationNames);
    }

    private void LoadAnimations(List<string> animationNames)
    {
        var modelDatabase = SystemManager.GetSystem<DatabaseSystem>()
            .GetDatabase<ModelDataBase>();

        foreach (var animationName in animationNames)
            if (!_animationCache.ContainsKey(animationName))
            {
                var animation = modelDatabase.GetAnimation(animationName);
                _animationCache[animationName] = animation;
            }
    }

    #endregion

    #region Animation Playback

    public void PlayAnimation(string animationName, double blendDuration = 0.0, string nextAnimation = null)
    {
        if (!ValidatePlayRequest(animationName))
            return;

        EnsureRigInitialized();

        var animation = _animationCache[animationName];

        // Set next animation transition if provided
        _nextAnimationName = nextAnimation;
        _nextAnimationBlendDuration = blendDuration;

        if (ShouldBlendAnimation(blendDuration))
            StartAnimationBlend(animation, blendDuration);
        else
            SetCurrentAnimation(animation);
    }

    private bool ValidatePlayRequest(string animationName)
    {
        if (string.IsNullOrEmpty(animationName))
        {
            Console.WriteLine("Animation name is null or empty.");
            return false;
        }

        if (!_animationCache.ContainsKey(animationName))
        {
            Console.WriteLine($"Animation '{animationName}' not found.");
            return false;
        }

        if (_rootNode == null)
        {
            Console.WriteLine("Root node not found in the model.");
            return false;
        }

        return true;
    }

    private void EnsureRigInitialized()
    {
        if (_rigBones.Count == 0) BuildRigFromHierarchy();
    }

    private bool ShouldBlendAnimation(double blendDuration)
    {
        return blendDuration > 0.0 && _currentAnimation != null;
    }

    private void StartAnimationBlend(Assimp.Animation targetAnimation, double blendDuration)
    {
        _targetAnimation = targetAnimation;
        _targetAnimationTime = 0.0;
        _blendDuration = blendDuration;
        _blendTimer = 0.0;
    }

    private void SetCurrentAnimation(Assimp.Animation animation)
    {
        _currentAnimation = animation;
        _currentAnimationTime = 0.0;
    }

    #endregion

    #region Update Loop

    public void Update(double deltaTime)
    {
        if (_currentAnimation == null)
            return;

        if (IsBlending())
            UpdateBlending(deltaTime);
        else
            UpdateAnimation(deltaTime);
    }

    private bool IsBlending()
    {
        return _targetAnimation != null;
    }

    private void UpdateBlending(double deltaTime)
    {
        _blendTimer += deltaTime;
        _targetAnimationTime += deltaTime;

        var blendWeight = CalculateBlendWeight();

        var currentPose = SampleAnimation(_currentAnimation!, _currentAnimationTime);
        var targetPose = SampleAnimation(_targetAnimation!, _targetAnimationTime);

        ApplyBlendedPose(currentPose, targetPose, blendWeight);

        if (IsBlendComplete(blendWeight)) CompleteBlend();
    }

    private float CalculateBlendWeight()
    {
        return (float)Math.Clamp(_blendTimer / _blendDuration, 0.0, 1.0);
    }

    private bool IsBlendComplete(float blendWeight)
    {
        return blendWeight >= 1.0f;
    }

    private void CompleteBlend()
    {
        _currentAnimation = _targetAnimation;
        _currentAnimationTime = _targetAnimationTime;
        _targetAnimation = null;
    }

    private void UpdateAnimation(double deltaTime)
    {
        _currentAnimationTime += deltaTime;

        // Check if should transition to next animation
        if (!string.IsNullOrEmpty(_nextAnimationName) && IsAnimationComplete())
        {
            TransitionToNextAnimation();
            return;
        }

        var pose = SampleAnimation(_currentAnimation!, _currentAnimationTime);
        ApplyPoseToBones(pose);
    }

    #endregion

    #region Rig Building

    private void BuildRigFromHierarchy()
    {
        if (Transform.Parent?.GameObject == null)
            return;

        var bones = Transform.Parent.GameObject.GetComponentsInChildren<Bone>();

        foreach (var bone in bones)
            if (bone != null && !_rigBones.Contains(bone))
                _rigBones.Add(bone);
    }

    #endregion

    #region Animation Sampling

    private Dictionary<string, BonePose> SampleAnimation(Assimp.Animation animation, double time)
    {
        var ticksPerSecond = GetTicksPerSecond(animation);
        var animationTicks = CalculateAnimationTicks(time, ticksPerSecond, animation);

        var pose = new Dictionary<string, BonePose>();
        SampleHierarchy(animation, animationTicks, _rootNode!, Transform.GetWorldTransformMatrix(), pose);

        return pose;
    }

    private float GetTicksPerSecond(Assimp.Animation animation)
    {
        return animation.TicksPerSecond != 0
            ? (float)animation.TicksPerSecond
            : DEFAULT_TICKS_PER_SECOND;
    }

    private float CalculateAnimationTicks(double time, float ticksPerSecond, Assimp.Animation animation)
    {
        var totalTicks = (float)(time * ticksPerSecond);
        return totalTicks % (float)animation.DurationInTicks;
    }

    private void SampleHierarchy(
        Assimp.Animation animation,
        float animationTime,
        Node node,
        Matrix4 parentTransform,
        Dictionary<string, BonePose> pose)
    {
        var nodeName = node.Name;
        var nodeTransform = EngineExtensions.ToOpenTK(node.Transform);

        var boneChannel = FindBoneAnimationChannel(animation, nodeName);

        if (boneChannel != null) nodeTransform = CalculateAnimatedTransform(animationTime, boneChannel);

        nodeTransform.Row3.Xyz *= EngineWindow.ScaleFactor;

        var globalTransform = Matrix4.Mult(nodeTransform, parentTransform);

        pose[nodeName] = ExtractBonePose(globalTransform);

        for (var i = 0; i < node.ChildCount; i++)
            SampleHierarchy(animation, animationTime, node.Children[i], globalTransform, pose);
    }

    private Matrix4 CalculateAnimatedTransform(float animationTime, NodeAnimationChannel channel)
    {
        var scale = InterpolateScale(animationTime, channel);
        var rotation = InterpolateRotation(animationTime, channel);
        var position = InterpolatePosition(animationTime, channel);

        var scaleMatrix = Matrix4.CreateScale(scale);
        var rotationMatrix = Matrix4.CreateFromQuaternion(rotation);
        var translationMatrix = Matrix4.CreateTranslation(position);

        return Matrix4.Mult(Matrix4.Mult(scaleMatrix, rotationMatrix), translationMatrix);
    }

    private BonePose ExtractBonePose(Matrix4 transform)
    {
        return new BonePose
        {
            Position = transform.ExtractTranslation(),
            Rotation = transform.ExtractRotation(),
            Scale = transform.ExtractScale()
        };
    }

    private NodeAnimationChannel FindBoneAnimationChannel(Assimp.Animation animation, string nodeName)
    {
        for (var i = 0; i < animation.NodeAnimationChannelCount; i++)
        {
            var channel = animation.NodeAnimationChannels[i];
            if (channel.NodeName.Equals(nodeName))
                return channel;
        }

        return null;
    }

    #endregion

    #region Pose Application

    private void ApplyBlendedPose(
        Dictionary<string, BonePose> fromPose,
        Dictionary<string, BonePose> toPose,
        float blendWeight)
    {
        foreach (var kvp in fromPose)
        {
            var boneName = kvp.Key;
            var startPose = kvp.Value;

            if (!toPose.TryGetValue(boneName, out var endPose))
                endPose = startPose;

            var blendedPose = BlendPoses(startPose, endPose, blendWeight);
            ApplyPoseToBone(boneName, blendedPose);
        }
    }

    private BonePose BlendPoses(BonePose from, BonePose to, float weight)
    {
        return new BonePose
        {
            Position = Vector3.Lerp(from.Position, to.Position, weight),
            Rotation = Quaternion.Slerp(from.Rotation, to.Rotation, weight),
            Scale = Vector3.Lerp(from.Scale, to.Scale, weight)
        };
    }

    private void ApplyPoseToBones(Dictionary<string, BonePose> pose)
    {
        foreach (var kvp in pose) ApplyPoseToBone(kvp.Key, kvp.Value);
    }

    private void ApplyPoseToBone(string boneName, BonePose pose)
    {
        var bone = _rigBones.Find(b => b.BoneName == boneName);

        if (bone != null)
        {
            bone.Transform.WorldPosition = pose.Position;
            bone.Transform.WorldRotation = pose.Rotation;
            bone.Transform.WorldScale = pose.Scale;
        }
    }

    #endregion

    #region Interpolation - Scale

    private Vector3 InterpolateScale(float time, NodeAnimationChannel channel)
    {
        if (channel.ScalingKeyCount == 1)
            return channel.ScalingKeys[0].Value.ToOpenTK();

        var index0 = FindScaleKeyIndex(time, channel);
        var index1 = index0 + 1;

        var interpolationFactor = CalculateInterpolationFactor(
            time,
            (float)channel.ScalingKeys[index0].Time,
            (float)channel.ScalingKeys[index1].Time
        );

        var startScale = channel.ScalingKeys[index0].Value.ToOpenTK();
        var endScale = channel.ScalingKeys[index1].Value.ToOpenTK();

        return Vector3.Lerp(startScale, endScale, interpolationFactor);
    }

    private int FindScaleKeyIndex(float time, NodeAnimationChannel channel)
    {
        for (var i = 0; i < channel.ScalingKeyCount - 1; i++)
            if (time < channel.ScalingKeys[i + 1].Time)
                return i;

        return 0;
    }

    #endregion

    #region Interpolation - Rotation

    private Quaternion InterpolateRotation(float time, NodeAnimationChannel channel)
    {
        if (channel.RotationKeyCount == 1)
            return channel.RotationKeys[0].Value.ToOpenTK();

        var index0 = FindRotationKeyIndex(time, channel);
        var index1 = index0 + 1;

        var interpolationFactor = CalculateInterpolationFactor(
            time,
            (float)channel.RotationKeys[index0].Time,
            (float)channel.RotationKeys[index1].Time
        );

        var startRotation = channel.RotationKeys[index0].Value.ToOpenTK();
        var endRotation = channel.RotationKeys[index1].Value.ToOpenTK();

        return Quaternion.Slerp(startRotation, endRotation, interpolationFactor);
    }

    private int FindRotationKeyIndex(float time, NodeAnimationChannel channel)
    {
        for (var i = 0; i < channel.RotationKeyCount - 1; i++)
            if (time < channel.RotationKeys[i + 1].Time)
                return i;

        return 0;
    }

    #endregion

    #region Interpolation - Position

    private Vector3 InterpolatePosition(float time, NodeAnimationChannel channel)
    {
        if (channel.PositionKeyCount == 1)
            return channel.PositionKeys[0].Value.ToOpenTK();

        var index0 = FindPositionKeyIndex(time, channel);
        var index1 = index0 + 1;

        var interpolationFactor = CalculateInterpolationFactor(
            time,
            (float)channel.PositionKeys[index0].Time,
            (float)channel.PositionKeys[index1].Time
        );

        var startPosition = channel.PositionKeys[index0].Value.ToOpenTK();
        var endPosition = channel.PositionKeys[index1].Value.ToOpenTK();

        return Vector3.Lerp(startPosition, endPosition, interpolationFactor);
    }

    private int FindPositionKeyIndex(float time, NodeAnimationChannel channel)
    {
        for (var i = 0; i < channel.PositionKeyCount - 1; i++)
            if (time < channel.PositionKeys[i + 1].Time)
                return i;

        return 0;
    }

    #endregion

    #region Helper Methods

    private float CalculateInterpolationFactor(float currentTime, float startTime, float endTime)
    {
        var deltaTime = endTime - startTime;

        if (deltaTime <= 0)
            return 0;

        return (currentTime - startTime) / deltaTime;
    }

    private bool IsAnimationComplete()
    {
        if (_currentAnimation == null)
            return false;

        var ticksPerSecond = GetTicksPerSecond(_currentAnimation);
        var totalTicks = (float)(_currentAnimationTime * ticksPerSecond);

        return totalTicks >= _currentAnimation.DurationInTicks;
    }

    private void TransitionToNextAnimation()
    {
        if (string.IsNullOrEmpty(_nextAnimationName))
            return;

        if (!_animationCache.ContainsKey(_nextAnimationName))
        {
            Console.WriteLine($"Next animation '{_nextAnimationName}' not found. Continuing current animation loop.");
            _nextAnimationName = null;
            return;
        }

        var targetAnimName = _nextAnimationName;
        var blendDuration = _nextAnimationBlendDuration;

        // Clear next animation before playing to avoid infinite loop
        _nextAnimationName = null;

        // Play the next animation
        PlayAnimation(targetAnimName, blendDuration);
    }

    #endregion
}

public struct BonePose
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;
}