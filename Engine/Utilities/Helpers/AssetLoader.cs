using System.Numerics;
using Adapters;
using Assimp;
using Engine.Components;
using Engine.Core;
using Engine.Database.Implementations;
using Engine.Systems;
using Bone = Engine.Animation.Bone;
using Material = Engine.Rendering.Material;

namespace Engine.Utilities;

/// <summary>
/// Handles loading and instantiation of 3D models using the Assimp library.
/// Supports FBX and OBJ formats with skeletal animation data.
/// </summary>
internal static class AssetLoader
{
    #region Constants

    private const string MODELS_FOLDER_PATH = "Assets/Models";
    private const string FBX_EXTENSION = ".fbx";
    private const string OBJ_EXTENSION = ".obj";
    private const string FBX_SEARCH_PATTERN = "*.fbx";
    private const string OBJ_SEARCH_PATTERN = "*.obj";

    #endregion

    #region Model Loading

    /// <summary>
    /// Loads a 3D model file using Assimp.
    /// </summary>
    /// <param name="fileName">Name of the model file (e.g., "character.fbx").</param>
    /// <returns>Loaded Assimp scene.</returns>
    /// <exception cref="Exception">Thrown if the model fails to load.</exception>
    public static Assimp.Scene LoadFromFile(string fileName)
    {
        var fullPath = BuildModelPath(fileName);

        var importer = new AssimpContext();
        var scene = importer.ImportFile(fullPath, GetImportFlags());

        ValidateLoadedScene(scene, fileName);

        return scene;
    }

    private static string BuildModelPath(string fileName)
    {
        var projectPath = ProjectPathHelper.GetExamplePath();
        return Path.Combine(projectPath, MODELS_FOLDER_PATH, fileName);
    }

    private static PostProcessSteps GetImportFlags()
    {
        return PostProcessSteps.Triangulate | PostProcessSteps.GenerateNormals;
    }

    private static void ValidateLoadedScene(Assimp.Scene scene, string fileName)
    {
        if (scene == null || scene.MeshCount == 0) throw new Exception($"Failed to load the model: {fileName}");
    }

    #endregion

    #region Model Discovery

    /// <summary>
    /// Gets a list of all available model files in the models directory.
    /// </summary>
    /// <returns>List of model file names.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown if the models directory doesn't exist.</exception>
    public static List<string> GetModelNames()
    {
        var modelsDirectory = GetModelsDirectoryPath();
        ValidateModelsDirectory(modelsDirectory);

        var modelFiles = FindModelFiles(modelsDirectory);
        return ExtractFileNames(modelFiles);
    }

    private static string GetModelsDirectoryPath()
    {
        var projectPath = ProjectPathHelper.GetExamplePath();
        return Path.Combine(projectPath, MODELS_FOLDER_PATH);
    }

    private static void ValidateModelsDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"The directory {directoryPath} does not exist.");
    }

    private static IEnumerable<string> FindModelFiles(string directoryPath)
    {
        var fbxFiles = Directory.GetFiles(directoryPath, FBX_SEARCH_PATTERN);
        var objFiles = Directory.GetFiles(directoryPath, OBJ_SEARCH_PATTERN);

        return fbxFiles.Concat(objFiles);
    }

    private static List<string> ExtractFileNames(IEnumerable<string> filePaths)
    {
        return filePaths.Select(Path.GetFileName).ToList();
    }

    #endregion

    #region Model Instantiation

    /// <summary>
    /// Instantiates a GameObject from a model file path.
    /// </summary>
    /// <param name="modelPath">Full or relative path to the model file.</param>
    /// <returns>Root GameObject of the instantiated model, or null if the format is unsupported.</returns>
    public static GameObject Instantiate(string modelPath)
    {
        if (IsValidModelPath(modelPath, out var modelName)) return LoadAndInstantiateModel(modelName);

        return null;
    }

    private static bool IsValidModelPath(string modelPath, out string modelName)
    {
        if (modelPath.EndsWith(FBX_EXTENSION, StringComparison.OrdinalIgnoreCase))
        {
            modelName = Path.GetFileNameWithoutExtension(modelPath) + FBX_EXTENSION;
            return true;
        }

        if (modelPath.EndsWith(OBJ_EXTENSION, StringComparison.OrdinalIgnoreCase))
        {
            modelName = Path.GetFileNameWithoutExtension(modelPath) + OBJ_EXTENSION;
            return true;
        }

        modelName = string.Empty;
        return false;
    }

    private static GameObject LoadAndInstantiateModel(string modelName)
    {
        var modelDatabase = SystemManager.GetSystem<DatabaseSystem>()
            .GetDatabase<ModelDataBase>();

        var scene = modelDatabase.GetModel(modelName);

        return BuildGameObjectHierarchy(scene.RootNode, scene, modelName);
    }

    #endregion

    #region Hierarchy Building

    /// <summary>
    /// Recursively builds a GameObject hierarchy from an Assimp node tree.
    /// </summary>
    private static GameObject BuildGameObjectHierarchy(
        Node node,
        Assimp.Scene scene,
        string modelName,
        Transform parentTransform = null)
    {
        var gameObject = CreateGameObjectFromNode(node, parentTransform);

        var boneBone = FindBoneByName(node.Name, scene);
        if (boneBone != null) AttachBoneComponent(gameObject, node, scene, boneBone);

        AttachMeshComponents(gameObject, node, scene, modelName);
        BuildChildHierarchy(node, scene, modelName, gameObject.Transform);

        return gameObject;
    }

    private static GameObject CreateGameObjectFromNode(Node node, Transform parentTransform)
    {
        var nodeTransform = EngineExtensions.ToOpenTK(node.Transform);
        nodeTransform.Row3.Xyz *= EngineWindow.ScaleFactor;

        var gameObject = new GameObject(nodeTransform)
        {
            Name = node.Name
        };

        gameObject.Transform.SetParent(parentTransform);

        return gameObject;
    }

    private static void AttachBoneComponent(
        GameObject gameObject,
        Node node,
        Assimp.Scene scene,
        Assimp.Bone assimpBone)
    {
        var bone = gameObject.AddComponent<Bone>();
        bone.BoneName = node.Name;
        bone.BindPose = assimpBone.OffsetMatrix;
        bone.WorldPose = CalculateBoneWorldTransform(scene, node);
    }

    private static void AttachMeshComponents(
        GameObject gameObject,
        Node node,
        Assimp.Scene scene,
        string modelName)
    {
        foreach (var meshIndex in node.MeshIndices)
        {
            var mesh = scene.Meshes[meshIndex];
            var material = CreateMaterial(scene, mesh);

            if (mesh.HasBones)
                gameObject.AddComponent<SkinnedMeshRenderer>(material, modelName, meshIndex);
            else
                gameObject.AddComponent<MeshRenderer>(material, modelName, meshIndex);
        }
    }

    private static Material CreateMaterial(Assimp.Scene scene, Mesh mesh)
    {
        var materialIndex = mesh.MaterialIndex;
        var assimpMaterial = scene.Materials[materialIndex];

        return new Material(assimpMaterial);
    }

    private static void BuildChildHierarchy(
        Node parentNode,
        Assimp.Scene scene,
        string modelName,
        Transform parentTransform)
    {
        foreach (var childNode in parentNode.Children)
            BuildGameObjectHierarchy(childNode, scene, modelName, parentTransform);
    }

    #endregion

    #region Bone Handling

    /// <summary>
    /// Searches for a bone with the given name in the scene's meshes.
    /// </summary>
    private static Assimp.Bone FindBoneByName(string boneName, Assimp.Scene scene)
    {
        foreach (var mesh in scene.Meshes)
        {
            if (!mesh.HasBones)
                continue;

            foreach (var bone in mesh.Bones)
                if (bone.Name == boneName)
                    return bone;
        }

        return null;
    }

    /// <summary>
    /// Calculates the world transform for a bone node.
    /// </summary>
    private static Matrix4x4 CalculateBoneWorldTransform(Assimp.Scene scene, Node boneNode)
    {
        var boneGlobalTransform = CalculateGlobalTransform(boneNode);
        return scene.RootNode.Transform * boneGlobalTransform;
    }

    /// <summary>
    /// Recursively calculates the global transform of a node by accumulating parent transforms.
    /// </summary>
    private static Matrix4x4 CalculateGlobalTransform(Node node)
    {
        var localTransform = node.Transform;

        if (node.Parent == null)
            return localTransform;

        var parentGlobalTransform = CalculateGlobalTransform(node.Parent);
        return parentGlobalTransform * localTransform;
    }

    #endregion
}