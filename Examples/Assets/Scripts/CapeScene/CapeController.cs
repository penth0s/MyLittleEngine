using System.Numerics;
using Adapters;
using Engine.Components;
using Engine.Core;
using Jitter2.LinearMath;
using OpenTK.Windowing.GraphicsLibraryFramework;
using PhysicsEngine.Core;

namespace Project.Assets.Scripts.CapeScene;

public class CapeController : BeroBehaviour
{
    public float MoveSpeed = 0.7f;
    public Vector3 Wind;
    
    private SoftBodyCape _cape;
    private List<Transform> _bones;
    private List<int> _pinnedBoneIndices = new();

    private readonly List<string> _pinnedBoneNames = new()
    {
        "Cape_bone",
        "Cape_bone.007",
        "Cape_bone.014",
        "Cape_bone.021",
        "Cape_bone.028"
    };

    protected override void Start()
    {
        base.Start();

        InitializeBones();
        InitializeCape();
    }

    public override void Update()
    {
        base.Update();

        UpdatePinnedBones();
        SyncBonesFromCape();
        HandleInput();
    }

    private void InitializeBones()
    {
        _bones = new List<Transform>();
        CollectBones(Transform);

        for (var i = 0; i < _bones.Count; i++)
        {
            if (_pinnedBoneNames.Contains(_bones[i].GameObject.Name))
                _pinnedBoneIndices.Add(i);
        }
    }

    private void InitializeCape()
    {
        var boneTransforms = new JVector[_bones.Count];
        for (var i = 0; i < _bones.Count; i++)
        {
            boneTransforms[i] = _bones[i].Transform.WorldPosition.ToJitter();
        }

        _cape = new SoftBodyCape(
            PhysicsManager.GetWorld, 
            boneTransforms, 
            _pinnedBoneIndices.ToArray()
        );
    }

    private void UpdatePinnedBones()
    {
        for (var i = 0; i < _pinnedBoneIndices.Count; i++)
        {
            var boneIndex = _pinnedBoneIndices[i];
            var pinnedPosition = _bones[boneIndex].Transform.WorldPosition.ToJitter();
            _cape.UpdatePinnedBone(boneIndex, pinnedPosition);
        }
    }

    private void HandleInput()
    {
        if (Input.Keybord.IsKeyDown(Keys.W))
        {
            Transform.WorldPosition -= Transform.Forward * MoveSpeed * Time.DeltaTime;
            _cape.ApplyWind(Wind);
        }

        if (Input.Keybord.IsKeyDown(Keys.S))
        {
            Transform.WorldPosition += Transform.Forward * MoveSpeed * Time.DeltaTime;
            _cape.ApplyWind(-Wind);
        }
    }

    private void SyncBonesFromCape()
    {
        for (var i = 0; i < _bones.Count; i++)
        {
            if (_pinnedBoneIndices.Contains(i))
                continue;

            var softBodyPos = _cape.Vertices[i].Position;
            _bones[i].Transform.WorldPosition = softBodyPos.ToOpenTK();
        }
    }

    private void CollectBones(Transform parent)
    {
        foreach (var child in parent.Children)
        {
            if (child.GameObject.Name.Contains("bone", StringComparison.OrdinalIgnoreCase))
                _bones.Add(child);

            CollectBones(child);
        }
    }
}