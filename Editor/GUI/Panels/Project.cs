using Engine.Utilities;
using ImGuiNET;

namespace Editor.GUI;

/// <summary>
/// Manages the project/examples panel in the editor, displaying the asset folder structure.
/// Handles file browsing, folder creation, and drag-drop operations for project files.
/// </summary>
internal class Project
{
    #region Constants

    private const float WINDOW_POSITION_X_RATIO = 0.7f;
    private const float WINDOW_POSITION_Y_RATIO = 0.41f;
    private const float WINDOW_WIDTH_RATIO = 0.15f;
    private const float WINDOW_HEIGHT_RATIO = 0.6f;
    private const float IMGUI_FONT_SCALE = 2.0f;
    private const string DRAG_DROP_PAYLOAD_TYPE = "PROJECT_FILE";
    private const string CONTEXT_MENU_ID = "project_right_click";
    private const string DEFAULT_FOLDER_NAME = "New Folder";
    private const string ASSETS_FOLDER_NAME = "Assets";

    #endregion

    #region Events

    /// <summary>
    /// Triggered when a file is double-clicked in the project panel.
    /// </summary>
    public event Action<string> FileSelected;

    #endregion

    #region Fields

    private readonly string _rootPath;
    private string _currentPath;

    #endregion

    #region Initialization

    public Project()
    {
        _rootPath = Path.Combine(ProjectPathHelper.GetExamplePath(), ASSETS_FOLDER_NAME);
        _currentPath = _rootPath;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Draws the project window and its contents.
    /// </summary>
    public void Draw()
    {
        SetupProjectWindow();

        if (ImGui.Begin("Examples"))
        {
            ImGui.SetWindowFontScale(IMGUI_FONT_SCALE);
            DrawDirectoryTree(_rootPath);
        }

        DrawContextMenu();

        ImGui.End();
    }

    #endregion

    #region Window Setup

    private void SetupProjectWindow()
    {
        ImGui.SetNextWindowPos(CalculateWindowPosition(), ImGuiCond.Always);
        ImGui.SetNextWindowSize(CalculateWindowSize(), ImGuiCond.Always);
    }

    private System.Numerics.Vector2 CalculateWindowPosition()
    {
        var io = ImGui.GetIO();
        var screenSize = io.DisplaySize;

        return new System.Numerics.Vector2(
            screenSize.X * WINDOW_POSITION_X_RATIO,
            screenSize.Y * WINDOW_POSITION_Y_RATIO
        );
    }

    private System.Numerics.Vector2 CalculateWindowSize()
    {
        var io = ImGui.GetIO();
        var screenSize = io.DisplaySize;

        return new System.Numerics.Vector2(
            screenSize.X * WINDOW_WIDTH_RATIO,
            screenSize.Y * WINDOW_HEIGHT_RATIO
        );
    }

    #endregion

    #region Context Menu

    private void DrawContextMenu()
    {
        if (ImGui.BeginPopupContextWindow(CONTEXT_MENU_ID, ImGuiPopupFlags.MouseButtonRight))
        {
            if (ImGui.BeginMenu("Create"))
            {
                if (ImGui.MenuItem("Folder")) CreateNewFolder(_currentPath);

                ImGui.EndMenu();
            }

            ImGui.EndPopup();
        }
    }

    private void CreateNewFolder(string parentPath)
    {
        var newFolderPath = GenerateUniqueFolderPath(parentPath);
        Directory.CreateDirectory(newFolderPath);
    }

    private string GenerateUniqueFolderPath(string parentPath)
    {
        var folderPath = Path.Combine(parentPath, DEFAULT_FOLDER_NAME);
        var counter = 1;

        while (Directory.Exists(folderPath))
        {
            folderPath = Path.Combine(parentPath, $"{DEFAULT_FOLDER_NAME} ({counter})");
            counter++;
        }

        return folderPath;
    }

    #endregion

    #region Directory Tree Drawing

    private void DrawDirectoryTree(string directoryPath)
    {
        var subDirectories = Directory.GetDirectories(directoryPath);

        foreach (var subDirectory in subDirectories) DrawDirectoryNode(subDirectory);
    }

    private void DrawDirectoryNode(string directoryPath)
    {
        var directoryName = Path.GetFileName(directoryPath);
        var isNodeOpen = ImGui.TreeNode(directoryName);

        if (ImGui.IsItemClicked()) _currentPath = directoryPath;

        if (isNodeOpen)
        {
            DrawDirectoryContents(directoryPath);
            ImGui.TreePop();
        }
    }

    private void DrawDirectoryContents(string directoryPath)
    {
        // Draw subdirectories recursively
        DrawDirectoryTree(directoryPath);

        // Draw files in this directory
        var files = Directory.GetFiles(directoryPath);
        foreach (var filePath in files) DrawFileNode(filePath);
    }

    #endregion

    #region File Drawing

    private void DrawFileNode(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var treeNodeFlags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;

        ImGui.TreeNodeEx(fileName, treeNodeFlags);

        HandleFileInteraction(filePath);
        HandleFileDragDrop(filePath, fileName);
    }

    private void HandleFileInteraction(string filePath)
    {
        if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)) OnFileDoubleClicked(filePath);
    }

    private void OnFileDoubleClicked(string filePath)
    {
        FileSelected?.Invoke(filePath);
    }

    #endregion

    #region Drag and Drop

    private void HandleFileDragDrop(string filePath, string fileName)
    {
        if (ImGui.BeginDragDropSource())
        {
            SetDragDropPayload(filePath);
            ImGui.Text($"Dragging: {fileName}");
            ImGui.EndDragDropSource();
        }
    }

    private void SetDragDropPayload(string filePath)
    {
        var pathBytes = ConvertFilePathToBytes(filePath);

        unsafe
        {
            fixed (byte* dataPtr = pathBytes)
            {
                ImGui.SetDragDropPayload(
                    DRAG_DROP_PAYLOAD_TYPE,
                    (IntPtr)dataPtr,
                    (uint)pathBytes.Length
                );
            }
        }
    }

    private byte[] ConvertFilePathToBytes(string filePath)
    {
        // Add null terminator for C-style string
        return System.Text.Encoding.UTF8.GetBytes(filePath + "\0");
    }

    #endregion
}