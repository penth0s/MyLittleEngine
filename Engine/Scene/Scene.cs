using Engine.Components;
using Engine.Core;
using Engine.Database.Implementations;
using Engine.Utilities;
using Newtonsoft.Json;
using OpenTK.Mathematics;
using Camera = Engine.Components.Camera;
using Light = Engine.Components.Light;
using Vector3 = OpenTK.Mathematics.Vector3;

namespace Engine.Scene;

public sealed class Scene
{
    public Camera Camera => _camera;

    public Matrix4 CameraViewMatrix => _camera.GetViewMatrix();
    public Matrix4 CameraProjectionMatrix => _camera.GetProjectionMatrix();
    public Vector3 CameraPosition => _camera.Transform.LocalPosition;

    public Vector3 CameraForward => _camera.Transform.Forward;

    public Color4 ClearColor => _camera.ClearColor;

    public bool IsSkybox => _camera.ClearFlag == Camera.ClearFlags.Skybox;

    public List<Light> GeSceneLights => _sceneLights.FindAll(L => L.GameObject.IsActive);

    public List<int> GetShadowTextureIDs =>
        _sceneLights
            .Where(l => l.GameObject.IsActive && l.CastShadow)
            .Select(l => l.ShadowTextureIndex)
            .ToList();

    public List<GameObject> ActiveSceneObjects => _gameObjects.FindAll(G => G.IsActive);
    public List<GameObject> AllSceneObjects => _gameObjects;

    private List<Light> _sceneLights = new();
    private Camera _camera;
    private List<GameObject> _gameObjects;

    // Component cache system
    private Dictionary<Type, List<Component>> _componentCache = new();
    private bool _cacheDirty = true;

    public string SceneName { get; set; }

    public Scene(string sceneName)
    {
        SceneName = sceneName;
        _gameObjects = new List<GameObject>();

        CreateCam();
        CreateLight();
    }

    public Scene(SceneSaveData saveData)
    {
        SceneName = saveData.SceneName;
        _gameObjects = new List<GameObject>();

        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            MaxDepth = 128
        };

        if (saveData.EnvironmentData != null)
        {
            var environmentData = JsonConvert.DeserializeObject<EnvironmentData>(saveData.EnvironmentData, settings);
            Environment.SetData(environmentData);
        }


        foreach (var goData in saveData.GameObjects)
        {
            var go = JsonConvert.DeserializeObject<GameObject>(goData, settings);
            AddGameObject(go);
        }

        // Set parent-child relationships
        foreach (var gameObject in _gameObjects)
            if (gameObject.Transform._parentID != Guid.Empty)
            {
                var parentGO = GetGameObjectByID(gameObject.Transform._parentID);
                gameObject.Transform.SetParent(parentGO.Transform);
            }

        // Find the camera in the loaded game objects
        _camera = _gameObjects.Find(g => g.GetComponent<Camera>() != null)?.GetComponent<Camera>();

        if (_camera == null)
        {
            // throw new Exception("No camera found in the loaded scene.");
        }

        var lightComponents = _gameObjects
            .Select(g => g.GetComponent<Light>())
            .Where(l => l != null)
            .ToList();

        _sceneLights = lightComponents;

        // Build initial cache
        RebuildComponentCache();
    }

    #region Component Query System

    /// <summary>
    /// Gets components of the specified type in the scene.
    /// Uses caching for better performance.
    /// </summary>
    /// <typeparam name="T">Component type to search for</typeparam>
    /// <param name="includeInactive">If true, returns all components including inactive ones</param>
    /// <param name="forceRefresh">If true, rebuilds the cache before querying</param>
    /// <returns>List of components</returns>
    public List<T> GetComponents<T>(bool includeInactive = false, bool forceRefresh = false) where T : Component
    {
        if (forceRefresh || _cacheDirty) RebuildComponentCache();

        var componentType = typeof(T);

        if (!_componentCache.ContainsKey(componentType)) return new List<T>();

        if (includeInactive) return _componentCache[componentType].Cast<T>().ToList();

        return _componentCache[componentType]
            .Where(c => c.Enabled && c.GameObject.IsActive)
            .Cast<T>()
            .ToList();
    }

    /// <summary>
    /// Rebuilds the component cache from all game objects in the scene.
    /// Call this when game objects or components are added/removed.
    /// </summary>
    private void RebuildComponentCache()
    {
        _componentCache.Clear();

        foreach (var gameObject in _gameObjects)
        {
            var components = gameObject.GetAllComponents();
            UpdateComponentCache(components);
        }

        _cacheDirty = false;
    }

    private void UpdateComponentCache(List<Component> components)
    {
        foreach (var component in components)
        {
            // Cache the exact type
            var componentType = component.GetType();

            if (!_componentCache.ContainsKey(componentType)) _componentCache[componentType] = new List<Component>();

            _componentCache[componentType].Add(component);

            // Also cache all base types in the hierarchy
            var baseType = componentType.BaseType;
            while (baseType != null && baseType != typeof(object) && typeof(Component).IsAssignableFrom(baseType))
            {
                if (!_componentCache.ContainsKey(baseType)) _componentCache[baseType] = new List<Component>();

                _componentCache[baseType].Add(component);
                baseType = baseType.BaseType;
            }
        }
    }

    /// <summary>
    /// Marks the component cache as dirty, forcing a rebuild on next query.
    /// </summary>
    public void InvalidateComponentCache()
    {
        _cacheDirty = true;
    }

    #endregion

    #region GameObject Management

    private GameObject GetGameObjectByID(Guid id)
    {
        return _gameObjects.Find(t => t.ID == id);
    }

    public GameObject GetGameObjectByName(string name)
    {
        return _gameObjects.Find(t => t.Name == name);
    }

    public GameObject AddAssetToScene(string path)
    {
        var newGo = AssetLoader.Instantiate(path);
        AddGameObject(newGo);
        AddChildrenRecursively(newGo);

        return newGo;
    }

    public GameObject AddGameObject()
    {
        var newGo = new GameObject();
        AddGameObject(newGo);
        AddChildrenRecursively(newGo);
        return newGo;
    }

    private void AddChildrenRecursively(GameObject parent)
    {
        foreach (var child in parent.Transform.Children)
        {
            var childGO = child.GameObject;

            if (!_gameObjects.Contains(childGO))
            {
                AddGameObject(childGO);

                // Recursively add this child's children
                AddChildrenRecursively(childGO);
            }
        }
    }

    private void AddGameObject(GameObject gameObject)
    {
        _gameObjects.Add(gameObject);
        gameObject.GameObjectDestroyed += OnGameObjectDestroyed;
        InvalidateComponentCache();
    }

    private void OnGameObjectDestroyed(GameObject obj)
    {
        _gameObjects.Remove(obj);
        InvalidateComponentCache();
    }

    #endregion

    #region Scene Setup

    public void UpdateCameraTarget(GameObject target)
    {
        var targetPos = target.Transform.LocalPosition;
        targetPos.Y += 3.0f;
        targetPos.Z += 3.0f;
        targetPos.X += 3.0f;
        _camera.Transform.LocalPosition = targetPos;
        _camera.Transform.LookAt(target.Transform.LocalPosition, Vector3.UnitY);
    }

    private void CreateCam()
    {
        var camGO = new GameObject();
        _camera = camGO.AddComponent<Camera>();
        camGO.Name = "Camera";
        _camera.Transform.LocalPosition = new Vector3(0.0f, 5.0f, 0.0f);
        _gameObjects.Add(camGO);
    }

    private void CreateLight()
    {
        var lightGO = new GameObject();
        lightGO.Transform.LocalPosition = new Vector3(0.0f, 40f, 0.0f);
        lightGO.Transform.EulerAngles = new Vector3(90, 0, 0);

        var _light = lightGO.AddComponent<Light>();
        _sceneLights.Add(_light);
        lightGO.Name = "Light";
        _gameObjects.Add(lightGO);
    }

    #endregion

    #region Serialization

    public void SaveScene()
    {
        var saveData = new SceneSaveData(this);
        var saveJson = JsonConvert.SerializeObject(saveData);

        SceneDataBase.SaveScene(SceneName, saveJson);
    }

    public void DestroyScene()
    {
        for (var i = 0; i < _gameObjects.Count; i++)
        {
            _gameObjects[i].GameObjectDestroyed -= OnGameObjectDestroyed;
            _gameObjects[i].Destroy();
        }

        _gameObjects.Clear();
        _sceneLights.Clear();
        _componentCache.Clear();
        _camera = null;
    }

    #endregion
}

public class SceneSaveData
{
    public List<string> GameObjects { get; set; }
    public string SceneName { get; set; }
    public string EnvironmentData { get; set; }

    //Editor data
    public bool IsGridEnabled { get; set; }
    public bool IsDebugViewEnabled { get; set; }
    public bool IsWireframeEnabled { get; set; }

    public SceneSaveData()
    {
        GameObjects = [];
        SceneName = string.Empty;
    }

    public SceneSaveData(Scene scene)
    {
        GameObjects = [];
        SceneName = scene.SceneName;
        EnvironmentData = Environment.GetSaveData();

        IsGridEnabled = Screen.IsGridActive();
        IsDebugViewEnabled = Screen.IsDebugView();
        IsWireframeEnabled = Screen.IsWireFrameActive();

        foreach (var go in scene.AllSceneObjects)
        {
            var saveData = go.GetSaveData();
            GameObjects.Add(saveData);
        }
    }
}