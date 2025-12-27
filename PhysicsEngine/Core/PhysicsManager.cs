using Adapters;
using Jitter2;
using Jitter2.Dynamics;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using PhysicsEngine.Utilities;

namespace PhysicsEngine.Core
{
    /// <summary>
    /// Manages the physics simulation world and rigid body lifecycle
    /// </summary>
    public sealed class PhysicsManager
    {
        #region Constants

        private const int DEFAULT_SUBSTEP_COUNT = 4;

        #endregion

        #region Properties

        public static World GetWorld => _physicsWorld;

        #endregion

        #region Fields

        private static World _physicsWorld;
        private readonly Dictionary<Guid, PhysicsBody> _bodies;
        private readonly object _bodyLock = new object();
        private bool _isRemoving;
        private IGameEngineInfoProvider _gameEngineInfoProvider;
        private bool _isInitialized;

        #endregion

        #region Constructor

        public PhysicsManager()
        {
            _bodies = new Dictionary<Guid, PhysicsBody>();
        }

        #endregion

        #region Initialization

        public void Init(IGameEngineInfoProvider gameEngineInfoProvider)
        {
            if (_isInitialized)
            {
                throw new InvalidOperationException("PhysicsManager is already initialized");
            }

            ValidateGameEngineInfoProvider(gameEngineInfoProvider);

            InitializePhysicsWorld();
            InitializeRaycastHelper();
            RegisterEventHandlers(gameEngineInfoProvider);

            _gameEngineInfoProvider = gameEngineInfoProvider;
            _isInitialized = true;
        }

        private void ValidateGameEngineInfoProvider(IGameEngineInfoProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider), "GameEngineInfoProvider cannot be null");
            }
        }

        private void InitializePhysicsWorld()
        {
            _physicsWorld = new World
            {
                SubstepCount = DEFAULT_SUBSTEP_COUNT
            };
        }

        private void InitializeRaycastHelper()
        {
            RaycastHelper.Initialize(_physicsWorld);
        }

        private void RegisterEventHandlers(IGameEngineInfoProvider provider)
        {
            provider.RigidBodyCreated += OnRigidBodyCreated;
            provider.RigidBodyDestroyed += OnRigidBodyDestroyed;
            provider.RayCast += OnRayCast;
            provider.GameFrameUpdate += OnUpdate;
            provider.EngineShutdown += OnEngineShutdown;
        }

        private void OnEngineShutdown()
        {
            Cleanup();
        }

        #endregion

        #region Event Handlers

        private void OnRigidBodyCreated(object sender, IPhysicsBodyListener listener)
        {
            ValidateListener(listener);

            var physicsBody = RegisterBody(listener);
            listener.SetPhysicsBody(physicsBody);
        }

        private void OnRigidBodyDestroyed(object sender, IPhysicsBody physicsBody)
        {
            UnregisterBody(physicsBody);
        }

        private Guid OnRayCast(Vector3 start, Vector3 end)
        {
            var hitRigidBody = PerformRaycast(start, end);

            if (hitRigidBody == null)
            {
                return Guid.Empty;
            }

            return FindBodyIdByRigidBody(hitRigidBody);
        }

        private void OnUpdate(FrameEventArgs frameArgs, KeyboardState keyboardState, MouseState mouseState)
        {
            StepPhysicsSimulation(frameArgs);
            NotifyBodiesUpdated();
        }

        #endregion

        #region Body Registration

        private IPhysicsBody RegisterBody(IPhysicsBodyListener listener)
        {
            var rigidBody = CreateRigidBody();
            var physicsBody = CreatePhysicsBody(rigidBody);

            AddBodyToDictionary(listener.Id, physicsBody);

            return physicsBody;
        }

        private RigidBody CreateRigidBody()
        {
            var rigidBody = _physicsWorld.CreateRigidBody();
            rigidBody.IsStatic = true;
            return rigidBody;
        }

        private PhysicsBody CreatePhysicsBody(RigidBody rigidBody)
        {
            return new PhysicsBody
            {
                RigidBody = rigidBody
            };
        }

        private void AddBodyToDictionary(Guid id, PhysicsBody body)
        {
            lock (_bodyLock)
            {
                if (_bodies.ContainsKey(id))
                {
                    throw new InvalidOperationException($"Body with ID {id} already exists");
                }

                _bodies.Add(id, body);
            }
        }

        private void UnregisterBody(IPhysicsBody physicsBody)
        {
            if (physicsBody == null)
            {
                return;
            }

            _isRemoving = true;

            try
            {
                RemoveBodyFromWorld(physicsBody.Id);
                RemoveBodyFromDictionary(physicsBody.Id);
            }
            finally
            {
                _isRemoving = false;
            }
        }

        private void RemoveBodyFromWorld(Guid bodyId)
        {
            lock (_bodyLock)
            {
                if (_bodies.TryGetValue(bodyId, out var body))
                {
                    _physicsWorld.Remove(body.RigidBody);
                }
            }
        }

        private void RemoveBodyFromDictionary(Guid bodyId)
        {
            lock (_bodyLock)
            {
                _bodies.Remove(bodyId);
            }
        }

        #endregion

        #region Raycast

        private RigidBody PerformRaycast(Vector3 start, Vector3 end)
        {
            return RaycastHelper.Cast(start.ToJitter(), end.ToJitter());
        }

        private Guid FindBodyIdByRigidBody(RigidBody rigidBody)
        {
            lock (_bodyLock)
            {
                foreach (var bodyPair in _bodies)
                {
                    if (bodyPair.Value.RigidBody == rigidBody)
                    {
                        return bodyPair.Value.Id;
                    }
                }
            }

            return Guid.Empty;
        }

        #endregion

        #region Physics Simulation

        private void StepPhysicsSimulation(FrameEventArgs frameArgs)
        {
            var deltaTime = (float)frameArgs.Time;
            _physicsWorld.Step(deltaTime);
        }

        private void NotifyBodiesUpdated()
        {
            if (_isRemoving)
            {
                return;
            }

            lock (_bodyLock)
            {
                foreach (var bodyPair in _bodies)
                {
                    NotifyBodyUpdated(bodyPair.Value);
                }
            }
        }

        private void NotifyBodyUpdated(PhysicsBody body)
        {
            body.BodyUpdated?.Invoke(body, EventArgs.Empty);
        }

        #endregion

        #region Validation

        private void ValidateListener(IPhysicsBodyListener listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener), "Physics body listener cannot be null");
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Cleanup all physics resources
        /// </summary>
        public void Cleanup()
        {
            if (!_isInitialized)
            {
                return;
            }

            UnregisterEventHandlers();
            ClearAllBodies();

            _isInitialized = false;
        }

        private void UnregisterEventHandlers()
        {
            if (_gameEngineInfoProvider != null)
            {
                _gameEngineInfoProvider.RigidBodyCreated -= OnRigidBodyCreated;
                _gameEngineInfoProvider.RigidBodyDestroyed -= OnRigidBodyDestroyed;
                _gameEngineInfoProvider.RayCast -= OnRayCast;
                _gameEngineInfoProvider.GameFrameUpdate -= OnUpdate;
            }
        }

        private void ClearAllBodies()
        {
            lock (_bodyLock)
            {
                foreach (var body in _bodies.Values)
                {
                    try
                    {
                        _physicsWorld.Remove(body.RigidBody);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error removing body during cleanup: {ex.Message}");
                    }
                }

                _bodies.Clear();
            }
        }

        #endregion
    }
}