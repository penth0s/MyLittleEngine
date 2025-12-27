using Engine.Core;
using Engine.Systems;
using ImGuiNET;

namespace Editor.GUI;

/// <summary>
/// Manages the hierarchy view in the editor, displaying the scene's GameObject tree structure.
/// Handles object selection, expansion state, and drag-drop operations.
/// </summary>
internal class Hierarchy
{
    #region Constants

    private const float WINDOW_POSITION_X_RATIO = 0.7f;
    private const float WINDOW_WIDTH_RATIO = 0.15f;
    private const float WINDOW_HEIGHT = 800f;
    private const float IMGUI_FONT_SCALE = 2.0f;
    private const string DRAG_DROP_PAYLOAD_TYPE = "PROJECT_FILE";

    #endregion

    #region Events

    /// <summary>
    /// Triggered when a GameObject is selected in the hierarchy.
    /// </summary>
    public event Action<GameObject> ObjectSelected;

    /// <summary>
    /// Triggered when a GameObject is double-clicked (focused) in the hierarchy.
    /// </summary>
    public event Action<GameObject> ObjectFocused;

    /// <summary>
    /// Triggered when a file is dropped onto the hierarchy from the project panel.
    /// </summary>
    public event Action<string> FileDropped;

    #endregion

    #region Fields

    private GameObject _selectedObject;
    private readonly HashSet<GameObject> _expandedNodes = new();
    private SceneSystem _sceneSystem;

    #endregion

    #region Public Methods

    /// <summary>
    /// Draws the hierarchy window and its contents.
    /// </summary>
    public void Draw()
    {
        SetupHierarchyWindow();

        if (ImGui.Begin("Hierarchy"))
        {
            ImGui.SetWindowFontScale(IMGUI_FONT_SCALE);

            DrawSceneObjects();
            SetupDragDropTarget();
        }

        ImGui.End();
    }

    /// <summary>
    /// Resets the current selection and collapses all nodes.
    /// </summary>
    public void ResetSelection()
    {
        _selectedObject = null;
        _expandedNodes.Clear();
    }

    /// <summary>
    /// Selects and focuses on a GameObject by its unique ID.
    /// Automatically expands parent nodes to make the object visible.
    /// </summary>
    /// <param name="objectId">The unique identifier of the GameObject to select.</param>
    public void UpdateSelectedObject(Guid objectId)
    {
        var foundObject = FindObjectById(objectId);

        if (foundObject != null)
            SelectAndExpandToObject(foundObject);
        else
            Console.WriteLine($"No object found with ID: {objectId}");
    }

    #endregion

    #region Window Setup

    private void SetupHierarchyWindow()
    {
        ImGui.SetNextWindowPos(CalculateWindowPosition(), ImGuiCond.Always);
        ImGui.SetNextWindowSize(CalculateWindowSize(), ImGuiCond.Always);
    }

    private System.Numerics.Vector2 CalculateWindowPosition()
    {
        var io = ImGui.GetIO();
        var screenSize = io.DisplaySize;

        return new System.Numerics.Vector2(screenSize.X * WINDOW_POSITION_X_RATIO, 0);
    }

    private System.Numerics.Vector2 CalculateWindowSize()
    {
        var io = ImGui.GetIO();
        var screenSize = io.DisplaySize;

        return new System.Numerics.Vector2(screenSize.X * WINDOW_WIDTH_RATIO, WINDOW_HEIGHT);
    }

    #endregion

    #region Object Selection

    private GameObject FindObjectById(Guid objectId)
    {
        EnsureSceneSystemInitialized();

        var sceneObjects = _sceneSystem!.CurrentScene.AllSceneObjects;
        return sceneObjects.FirstOrDefault(obj => obj.ID == objectId);
    }

    private void SelectAndExpandToObject(GameObject targetObject)
    {
        _selectedObject = targetObject;
        ExpandParentNodes(targetObject);
        ObjectSelected?.Invoke(targetObject);
    }

    private void ExpandParentNodes(GameObject targetObject)
    {
        var currentParent = targetObject.Transform.Parent?.GameObject;

        while (currentParent != null)
        {
            _expandedNodes.Add(currentParent);
            currentParent = currentParent.Transform.Parent?.GameObject;
        }
    }

    #endregion

    #region Scene Drawing

    private void DrawSceneObjects()
    {
        EnsureSceneSystemInitialized();

        var sceneObjects = _sceneSystem!.CurrentScene.AllSceneObjects;

        // Draw only root-level objects (objects without parents)
        foreach (var obj in sceneObjects)
            if (obj.Transform.Parent == null)
                DrawGameObjectNode(obj);
    }

    private void DrawGameObjectNode(GameObject gameObject)
    {
        if (!IsValidGameObject(gameObject))
            return;

        ImGui.PushID(gameObject!.GetHashCode());

        var isNodeOpen = DrawNodeLabel(gameObject);
        HandleNodeInteraction(gameObject);

        if (isNodeOpen)
        {
            DrawChildNodes(gameObject);
            ImGui.TreePop();
        }

        ImGui.PopID();
    }

    private bool IsValidGameObject(GameObject gameObject)
    {
        return gameObject != null; //&& gameObject.Transform != null;
    }

    private bool DrawNodeLabel(GameObject gameObject)
    {
        var hasChildren = gameObject.Transform.Children.Count > 0;
        var isNodeOpen = false;

        if (hasChildren)
        {
            isNodeOpen = DrawTreeNode(gameObject);
            UpdateNodeExpansionState(gameObject, isNodeOpen);
        }
        else
        {
            DrawLeafNode(gameObject);
        }

        return isNodeOpen;
    }

    private bool DrawTreeNode(GameObject gameObject)
    {
        var shouldBeOpen = _expandedNodes.Contains(gameObject);

        if (shouldBeOpen) ImGui.SetNextItemOpen(true);

        return ImGui.TreeNode(gameObject.Name);
    }

    private void DrawLeafNode(GameObject gameObject)
    {
        var isSelected = _selectedObject == gameObject;
        ImGui.Selectable(gameObject.Name, isSelected);
    }

    private void UpdateNodeExpansionState(GameObject gameObject, bool isOpen)
    {
        if (isOpen)
            _expandedNodes.Add(gameObject);
        else
            _expandedNodes.Remove(gameObject);
    }

    private void HandleNodeInteraction(GameObject gameObject)
    {
        if (ImGui.IsItemClicked()) OnObjectClicked(gameObject);

        if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            OnObjectDoubleClicked(gameObject);
    }

    private void OnObjectClicked(GameObject gameObject)
    {
        _selectedObject = gameObject;
        Console.WriteLine($"Selected: {gameObject.Name}");
        ObjectSelected?.Invoke(gameObject);
    }

    private void OnObjectDoubleClicked(GameObject gameObject)
    {
        Console.WriteLine($"Double Clicked: {gameObject.Name}");
        ObjectFocused?.Invoke(gameObject);
    }

    private void DrawChildNodes(GameObject parentObject)
    {
        foreach (var childTransform in parentObject.Transform.Children) DrawGameObjectNode(childTransform.GameObject);
    }

    #endregion

    #region Drag and Drop

    private void SetupDragDropTarget()
    {
        // Create invisible button covering the entire window to capture drops
        ImGui.SetCursorPos(new System.Numerics.Vector2(0, 0));
        ImGui.InvisibleButton("FullDropZone", ImGui.GetWindowSize(), ImGuiButtonFlags.None);

        if (ImGui.BeginDragDropTarget())
        {
            HandleDragDropPayload();
            ImGui.EndDragDropTarget();
        }
    }

    private void HandleDragDropPayload()
    {
        unsafe
        {
            var payload = ImGui.AcceptDragDropPayload(DRAG_DROP_PAYLOAD_TYPE);

            if (payload.NativePtr != null)
            {
                var droppedFilePath = ExtractPayloadData(payload);
                OnFileDropped(droppedFilePath);
            }
        }
    }

    private unsafe string ExtractPayloadData(ImGuiPayloadPtr payload)
    {
        var dataSize = payload.DataSize - 1; // Exclude null terminator
        return System.Text.Encoding.UTF8.GetString((byte*)payload.Data, dataSize);
    }

    private void OnFileDropped(string filePath)
    {
        FileDropped?.Invoke(filePath);
    }

    #endregion

    #region Helper Methods

    private void EnsureSceneSystemInitialized()
    {
        _sceneSystem ??= SystemManager.GetSystem<SceneSystem>();
    }

    #endregion
}