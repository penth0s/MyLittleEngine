using Adapters;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using OpenTK.Mathematics;
using ShapeHelper = PhysicsEngine.Utilities.ShapeHelper;

namespace PhysicsEngine.Core
{
    /// <summary>
    /// Wrapper for Jitter2 RigidBody providing a cleaner interface with OpenTK types
    /// </summary>
    internal sealed class PhysicsBody : IPhysicsBody
    {
        #region Constants

        private const float DEBUG_SHAPE_SCALE = 1.025f;

        #endregion

        #region Properties

        public Guid Id { get; set; } = Guid.NewGuid();

        public Vector3 Position
        {
            get => GetPosition();
            set => SetPosition(value);
        }

        public Vector3 Velocity
        {
            get => GetVelocity();
            set => SetVelocity(value);
        }

        public Vector3 AngularVelocity
        {
            get => GetAngularVelocity();
            set => SetAngularVelocity(value);
        }

        public Quaternion Orientation
        {
            get => GetOrientation();
            set => SetOrientation(value);
        }

        public float Friction
        {
            get => GetFriction();
            set => SetFriction(value);
        }

        public bool IsStatic
        {
            get => GetIsStatic();
            set => SetIsStatic(value);
        }

        public EventHandler BodyUpdated { get; set; }

        #endregion

        #region Fields

        internal RigidBody RigidBody;

        #endregion

        #region Position & Orientation

        private Vector3 GetPosition()
        {
            ValidateRigidBody();
            return RigidBody.Position.ToOpenTK();
        }

        private void SetPosition(Vector3 value)
        {
            ValidateRigidBody();
            RigidBody.Position = value.ToJitter();
        }

        private Quaternion GetOrientation()
        {
            ValidateRigidBody();
            return RigidBody.Orientation.ToOpenTK();
        }

        private void SetOrientation(Quaternion value)
        {
            ValidateRigidBody();
            RigidBody.Orientation = value.ToJitter();
        }

        #endregion

        #region Velocity

        private Vector3 GetVelocity()
        {
            ValidateRigidBody();
            return RigidBody.Velocity.ToOpenTK();
        }

        private void SetVelocity(Vector3 value)
        {
            ValidateRigidBody();
            RigidBody.Velocity = value.ToJitter();
        }

        private Vector3 GetAngularVelocity()
        {
            ValidateRigidBody();
            return RigidBody.AngularVelocity.ToOpenTK();
        }

        private void SetAngularVelocity(Vector3 value)
        {
            ValidateRigidBody();
            RigidBody.AngularVelocity = value.ToJitter();
        }

        #endregion

        #region Physics Properties

        private float GetFriction()
        {
            ValidateRigidBody();
            return RigidBody.Friction;
        }

        private void SetFriction(float value)
        {
            ValidateRigidBody();
            RigidBody.Friction = Math.Max(0f, value);
        }

        private bool GetIsStatic()
        {
            ValidateRigidBody();
            return RigidBody.IsStatic;
        }

        private void SetIsStatic(bool value)
        {
            ValidateRigidBody();
            RigidBody.IsStatic = value;
        }

        #endregion

        #region Shape Management

        /// <summary>
        /// Adds a collision shape to the rigid body based on vertices
        /// </summary>
        /// <param name="vertices">List of vertices defining the shape</param>
        public void AddShape(List<Vector3> vertices)
        {
            ValidateRigidBody();
            ValidateVertices(vertices);

            var shape = ShapeHelper.GetShape(vertices);
            RigidBody.AddShape(shape);
        }

        public void AddCapsuleShape(float radius, float lenght)
        {
            ValidateRigidBody();
            
            var shape = new CapsuleShape(radius, lenght);
            RigidBody.AddShape(shape);
        }

        /// <summary>
        /// Gets debug visualization vertices for the first shape
        /// </summary>
        /// <returns>List of triangle vertices, or null if no shapes exist</returns>
        public List<Vector3> GetDebugShape()
        {
            ValidateRigidBody();

            if (!HasShapes())
            {
                return null;
            }

            var triangles = GetShapeTriangles();
            return ConvertTrianglesToVertices(triangles);
        }

        private bool HasShapes()
        {
            return RigidBody.Shapes.Count > 0;
        }

        private List<JTriangle> GetShapeTriangles()
        {
            var triangles = new List<JTriangle>();
            var shape = RigidBody.Shapes[0];
            
            Jitter2.Collision.Shapes.ShapeHelper.MakeHull(shape, triangles);
            
            return triangles;
        }

        private List<Vector3> ConvertTrianglesToVertices(List<JTriangle> triangles)
        {
            var vertices = new List<Vector3>(triangles.Count * 3);
            Vector3 currentPosition = Position;

            foreach (var triangle in triangles)
            {
                vertices.Add(TransformDebugVertex(triangle.V0, currentPosition));
                vertices.Add(TransformDebugVertex(triangle.V1, currentPosition));
                vertices.Add(TransformDebugVertex(triangle.V2, currentPosition));
            }

            return vertices;
        }

        private Vector3 TransformDebugVertex(JVector vertex, Vector3 bodyPosition)
        {
            return vertex.ToOpenTK() * DEBUG_SHAPE_SCALE + bodyPosition;
        }

        #endregion

        #region Validation

        private void ValidateRigidBody()
        {
            if (RigidBody == null)
            {
                throw new InvalidOperationException(
                    "RigidBody is not initialized. Ensure the PhysicsBody is properly created."
                );
            }
        }

        private void ValidateVertices(List<Vector3> vertices)
        {
            if (vertices == null || vertices.Count == 0)
            {
                throw new ArgumentException(
                    "Vertices list cannot be null or empty",
                    nameof(vertices)
                );
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the number of shapes attached to this body
        /// </summary>
        public int GetShapeCount()
        {
            ValidateRigidBody();
            return RigidBody.Shapes.Count;
        }

        /// <summary>
        /// Checks if the body has any shapes
        /// </summary>
        public bool HasAnyShapes()
        {
            ValidateRigidBody();
            return RigidBody.Shapes.Count > 0;
        }

        /// <summary>
        /// Applies a force to the rigid body
        /// </summary>
        public void ApplyForce(Vector3 force)
        {
            ValidateRigidBody();
            
            if (IsStatic)
            {
                return; // Static bodies don't respond to forces
            }

            Velocity += force;
        }

        /// <summary>
        /// Applies an impulse to the rigid body
        /// </summary>
        public void ApplyImpulse(Vector3 impulse)
        {
            ValidateRigidBody();
            
            if (IsStatic)
            {
                return;
            }

            Velocity += impulse;
        }

        /// <summary>
        /// Applies torque to the rigid body
        /// </summary>
        public void ApplyTorque(Vector3 torque)
        {
            ValidateRigidBody();
            
            if (IsStatic)
            {
                return;
            }

            AngularVelocity += torque;
        }

        #endregion
    }
}