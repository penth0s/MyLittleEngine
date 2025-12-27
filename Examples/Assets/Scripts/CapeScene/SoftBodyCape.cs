using Jitter2;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using Jitter2.SoftBodies;

namespace Project.Assets.Scripts.CapeScene;

public class SoftBodyCape : SoftBody
{
    private readonly Dictionary<int, int> _pinnedBoneToBodyIndex;
    private List<RigidBody> PinnedBodies { get; } = new();

    public SoftBodyCape(World world, JVector[] boneTransforms, int[] pinnedIndices) : base(world)
    {
        _pinnedBoneToBodyIndex = new Dictionary<int, int>();
        Build(boneTransforms, pinnedIndices);
    }

    private void Build(JVector[] boneTransforms, int[] pinnedIndices)
    {
        var pinnedSet = new HashSet<int>(pinnedIndices);

        CreateVertices(boneTransforms, pinnedSet);
        CreateSprings(boneTransforms, pinnedSet);
    }

    private void CreateVertices(JVector[] boneTransforms, HashSet<int> pinnedSet)
    {
        for (var i = 0; i < boneTransforms.Length; i++)
        {
            var body = World.CreateRigidBody();
            body.Position = boneTransforms[i];

            if (pinnedSet.Contains(i))
            {
                ConfigurePinnedBody(body, i);
            }
            else
            {
                ConfigureDynamicBody(body);
            }

            Vertices.Add(body);
        }
    }

    private void ConfigurePinnedBody(RigidBody body, int index)
    {
        body.SetMassInertia(JMatrix.Zero, 0.0f, true);
        body.IsStatic = true;

        _pinnedBoneToBodyIndex[index] = PinnedBodies.Count;
        PinnedBodies.Add(body);
    }

    private void ConfigureDynamicBody(RigidBody body)
    {
        body.SetMassInertia(JMatrix.Zero, 1.0f, true);
        body.Damping = (0.02f, 0.02f);
        body.AffectedByGravity = true;
        body.AddShape(new Jitter2.Collision.Shapes.SphereShape(0.06f), false);
    }

    private void CreateSprings(JVector[] boneTransforms, HashSet<int> pinnedSet)
    {
        for (var i = 0; i < boneTransforms.Length - 1; i++)
        {
            var constraint = World.CreateConstraint<SpringConstraint>(Vertices[i], Vertices[i + 1]);
            constraint.Initialize(boneTransforms[i], boneTransforms[i + 1]);

            var bothPinned = pinnedSet.Contains(i) && pinnedSet.Contains(i + 1);
            constraint.Softness = bothPinned ? 0.01f : 0.02f;

            Springs.Add(constraint);
        }
    }

    public void UpdatePinnedBone(int boneIndex, JVector position)
    {
        if (_pinnedBoneToBodyIndex.TryGetValue(boneIndex, out var bodyIndex))
        {
            PinnedBodies[bodyIndex].Position = position;
            PinnedBodies[bodyIndex].Velocity = JVector.Zero;
        }
    }

    public void ApplyWind(JVector windForce)
    {
        foreach (var vertex in Vertices)
        {
            if (!vertex.IsStatic)
                vertex.AddForce(windForce * 0.5f, vertex.Position);
        }
    }
}