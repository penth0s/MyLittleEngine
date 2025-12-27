using Jitter2.Collision;
using Jitter2.Collision.Shapes;

namespace Project.Assets.Scripts.MonsterCarScene;

public static class CollisionHelper
{
    public class IgnoreCollisionBetweenFilter : IBroadPhaseFilter
    {
        private readonly struct Pair : IEquatable<Pair>
        {
            private readonly RigidBodyShape shapeA, shapeB;

            public Pair(RigidBodyShape shapeA, RigidBodyShape shapeB)
            {
                this.shapeA = shapeA;
                this.shapeB = shapeB;
            }

            public bool Equals(Pair other)
            {
                return shapeA.Equals(other.shapeA) && shapeB.Equals(other.shapeB);
            }

            public override bool Equals(object obj)
            {
                return obj is Pair other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(shapeA, shapeB);
            }
        }

        private readonly HashSet<Pair> ignore = new();

        public bool Filter(IDynamicTreeProxy proxyA, IDynamicTreeProxy proxyB)
        {
            if (proxyA is not RigidBodyShape shapeA || proxyB is not RigidBodyShape shapeB) return false;

            if (shapeB.ShapeId < shapeA.ShapeId) (shapeA, shapeB) = (shapeB, shapeA);
            return !ignore.Contains(new Pair(shapeA, shapeB));
        }

        public void IgnoreCollisionBetween(RigidBodyShape shapeA, RigidBodyShape shapeB)
        {
            if (shapeB.ShapeId < shapeA.ShapeId) (shapeA, shapeB) = (shapeB, shapeA);
            ignore.Add(new Pair(shapeA, shapeB));
        }
    }
}