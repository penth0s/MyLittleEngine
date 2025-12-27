using System.Reflection;

namespace Engine.Systems;

/// <summary>
/// Manages system lifecycle including automatic discovery, initialization, and retrieval
/// </summary>
public static class SystemManager
{
    #region Events

    /// <summary>
    /// Fired when all systems have been initialized
    /// </summary>
    public static event Action SystemInitializeCompleted;

    #endregion

    #region Fields

    private static readonly Dictionary<Type, object> _systems = new();
    private static readonly List<object> _allSystems = new();
    private static readonly object _lock = new();
    private static bool _isInitialized = false;

    #endregion


    #region Initialization

    /// <summary>
    /// Automatically discover and initialize all systems
    /// </summary>
    public static void InitializeAllSystems()
    {
        lock (_lock)
        {
            if (_isInitialized)
            {
                LogWarning("Systems already initialized. Skipping re-initialization.");
                return;
            }

            try
            {
                LogHeader("System Initialization Started");

                var configTypes = DiscoverConfigTypes();
                var systemTypes = DiscoverSystemTypes();

                LogInfo($"Discovered {configTypes.Count} config types");
                LogInfo($"Discovered {systemTypes.Count} system types");
                LogSeparator();

                InitializeAllDiscoveredSystems(systemTypes, configTypes);

                _isInitialized = true;
                LogHeader("System Initialization Completed");

                SystemInitializeCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                LogError($"Critical error during system initialization: {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>
    /// Initialize all discovered systems with their configs
    /// </summary>
    private static void InitializeAllDiscoveredSystems(List<Type> systemTypes, List<Type> configTypes)
    {
        var successCount = 0;
        var failureCount = 0;

        foreach (var systemType in systemTypes)
            try
            {
                InitializeSystem(systemType, configTypes);
                successCount++;
            }
            catch (Exception ex)
            {
                failureCount++;
                LogError($"Failed to initialize {systemType.Name}: {ex.Message}");
            }

        LogSeparator();
        LogInfo($"Initialization complete: {successCount} succeeded, {failureCount} failed");
    }

    /// <summary>
    /// Initialize a single system with its configuration
    /// </summary>
    private static void InitializeSystem(Type systemType, List<Type> availableConfigs)
    {
        var configType = GetConfigTypeForSystem(systemType);

        if (configType == null)
            InitializeParameterlessSystem(systemType);
        else
            InitializeSystemWithConfig(systemType, configType, availableConfigs);
    }

    /// <summary>
    /// Initialize a system that doesn't require configuration
    /// </summary>
    private static void InitializeParameterlessSystem(Type systemType)
    {
        if (!HasParameterlessInterface(systemType))
        {
            LogWarning($"Could not determine initialization method for {systemType.Name}");
            return;
        }

        var system = CreateSystemInstance(systemType);
        var initMethod = systemType.GetMethod("Initialize", Type.EmptyTypes);

        if (initMethod == null)
        {
            LogWarning($"No parameterless Initialize method found for {systemType.Name}");
            return;
        }

        initMethod.Invoke(system, null);
        RegisterSystem(systemType, system);

        LogSuccess($"{systemType.Name} initialized (no config)");
    }

    /// <summary>
    /// Initialize a system with its configuration
    /// </summary>
    private static void InitializeSystemWithConfig(Type systemType, Type configType, List<Type> availableConfigs)
    {
        if (!availableConfigs.Contains(configType))
        {
            LogWarning($"Config type {configType.Name} not found for {systemType.Name}");
            return;
        }

        var config = CreateConfigInstance(configType);
        var system = CreateSystemInstance(systemType);
        var initMethod = systemType.GetMethod("Initialize", new[] { configType });

        if (initMethod == null)
        {
            LogWarning($"No Initialize method with {configType.Name} parameter found for {systemType.Name}");
            return;
        }

        initMethod.Invoke(system, new[] { config });
        RegisterSystem(systemType, system);

        LogSuccess($"{systemType.Name} initialized with {configType.Name}");
    }

    /// <summary>
    /// Create an instance of a system
    /// </summary>
    private static object CreateSystemInstance(Type systemType)
    {
        try
        {
            return Activator.CreateInstance(systemType);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create instance of {systemType.Name}", ex);
        }
    }

    /// <summary>
    /// Create an instance of a configuration
    /// </summary>
    private static object CreateConfigInstance(Type configType)
    {
        try
        {
            return Activator.CreateInstance(configType);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create instance of {configType.Name}", ex);
        }
    }

    /// <summary>
    /// Register a system in the manager
    /// </summary>
    private static void RegisterSystem(Type systemType, object system)
    {
        lock (_lock)
        {
            _systems[systemType] = system;
            _allSystems.Add(system);
        }
    }

    #endregion

    #region Discovery

    /// <summary>
    /// Discover all config types in the assembly
    /// </summary>
    private static List<Type> DiscoverConfigTypes()
    {
        return Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(IsValidConfigType)
            .ToList();
    }

    /// <summary>
    /// Discover all system types in the assembly
    /// </summary>
    private static List<Type> DiscoverSystemTypes()
    {
        return Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(IsValidSystemType)
            .ToList();
    }

    /// <summary>
    /// Check if a type is a valid config type
    /// </summary>
    private static bool IsValidConfigType(Type type)
    {
        return typeof(ISystemConfig).IsAssignableFrom(type) &&
               !type.IsInterface &&
               !type.IsAbstract;
    }

    /// <summary>
    /// Check if a type is a valid system type
    /// </summary>
    private static bool IsValidSystemType(Type type)
    {
        return !type.IsInterface &&
               !type.IsAbstract &&
               IsSystemType(type);
    }

    /// <summary>
    /// Check if a type implements ISystem or ISystem<T>
    /// </summary>
    private static bool IsSystemType(Type type)
    {
        return type.GetInterfaces().Any(IsSystemInterface);
    }

    /// <summary>
    /// Check if an interface is ISystem or ISystem<T>
    /// </summary>
    private static bool IsSystemInterface(Type interfaceType)
    {
        return interfaceType == typeof(ISystem) ||
               (interfaceType.IsGenericType &&
                interfaceType.GetGenericTypeDefinition() == typeof(ISystem<>));
    }

    /// <summary>
    /// Check if a system implements the parameterless ISystem interface
    /// </summary>
    private static bool HasParameterlessInterface(Type systemType)
    {
        return systemType.GetInterfaces().Any(i => i == typeof(ISystem));
    }

    /// <summary>
    /// Get the config type required by a system
    /// </summary>
    private static Type GetConfigTypeForSystem(Type systemType)
    {
        var systemInterface = systemType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType &&
                                 i.GetGenericTypeDefinition() == typeof(ISystem<>));

        return systemInterface?.GetGenericArguments()[0];
    }

    #endregion

    #region System Retrieval

    /// <summary>
    /// Get a specific system by type
    /// </summary>
    /// <typeparam name="T">The system type</typeparam>
    /// <returns>The system instance or null if not found</returns>
    public static T GetSystem<T>() where T : class
    {
        lock (_lock)
        {
            if (_systems.TryGetValue(typeof(T), out var system)) return system as T;

            return null;
        }
    }


    /// <summary>
    /// Get all render systems sorted by priority
    /// </summary>
    internal static IEnumerable<IRenderSystem> GetAllRenderSystemsSorted()
    {
        lock (_lock)
        {
            return _allSystems
                .OfType<IRenderSystem>()
                .OrderBy(rs => rs.RenderPriority)
                .ToList();
        }
    }

    /// <summary>
    /// Get all game update systems
    /// </summary>
    internal static IEnumerable<IGameUpdateSystem> GetAllGameUpdateSystemsSorted()
    {
        lock (_lock)
        {
            return _allSystems.OfType<IGameUpdateSystem>().ToList();
        }
    }

    #endregion


    #region Logging

    private static void LogHeader(string message)
    {
        Console.WriteLine($"\n=== {message} ===\n");
    }

    private static void LogSeparator()
    {
        Console.WriteLine();
    }

    private static void LogSuccess(string message)
    {
        Console.WriteLine($"✓ {message}");
    }

    private static void LogInfo(string message)
    {
        Console.WriteLine($"ℹ {message}");
    }

    private static void LogWarning(string message)
    {
        Console.WriteLine($"⚠ {message}");
    }

    private static void LogError(string message)
    {
        Console.WriteLine($"✗ {message}");
    }

    #endregion
}