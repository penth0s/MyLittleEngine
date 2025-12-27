using Adapters;
using Editor.GUI;
using Engine.Core;
using Engine.Rendering;
using Engine.Systems;
using ImGuiNET;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Editor.Core;

public class Editor
{
    #region Constants

    private const float VIEWPORT_TOGGLE_WIDTH = 300f;
    private const float VIEWPORT_TOGGLE_HEIGHT = 25f;
    private const float VIEWPORT_TOGGLE_PADDING = 40f;
    private const float IMGUI_FONT_SCALE = 2.0f;
    private const float IMGUI_GLOBAL_FONT_SCALE = 1.0f;

    #endregion

    #region Fields

    // Editor Components
    private Inspector _inspector;
    private Hierarchy _hierarchy;
    private Project _project;

    //private PerformanceMonitor _performanceMonitor;
    private ImGuiController _imGuiController;

    // Systems
    private SceneSystem _sceneSystem;

    // State
    private GameObject _selectedObject;
    private IGameEngineInfoProvider _engineInfoProvider;
    private EditorInfoProvider _editorInfoProvider;
    private bool _isSceneViewHovered;

    // View Settings
    private bool _isGridEnabled;
    private bool _isDebugViewEnabled;
    private bool _isWireframeEnabled;

    #endregion

    #region Properties

    public EditorInfoProvider EditorInfoProvider => _editorInfoProvider;

    #endregion

    #region Initialization

    public void InitEditor(IGameEngineInfoProvider engineInfoProvider)
    {
        _engineInfoProvider = engineInfoProvider;
        _editorInfoProvider = new EditorInfoProvider();

        InitializeEditorComponents();
        SubscribeToHierarchyEvents();
        SubscribeToProjectEvents();
        SubscribeToEngineEvents();

        Console.Write("Editor initialized successfully!\n");
    }

    private void InitializeEditorComponents()
    {
        _inspector = new Inspector();
        _hierarchy = new Hierarchy();
        _project = new Project();
        _sceneSystem = SystemManager.GetSystem<SceneSystem>();
    }

    private void SubscribeToHierarchyEvents()
    {
        _hierarchy.ObjectSelected += OnHierarchyObjectSelected;
        _hierarchy.ObjectFocused += OnHierarchyObjectFocused;
        _hierarchy.FileDropped += OnHierarchyFileDropped;
    }

    private void SubscribeToProjectEvents()
    {
        _project.FileSelected += OnProjectFileSelected;
    }

    private void SubscribeToEngineEvents()
    {
        _engineInfoProvider.ResizeEditor += OnEditorResized;
        _engineInfoProvider.DrawEditor += OnEditorDraw;
        _engineInfoProvider.TextInput += OnTextInput;
        _engineInfoProvider.MouseWheel += OnMouseWheel;
        _engineInfoProvider.ResestInspector += OnInspectorReset;
        _engineInfoProvider.EngineInitialized += OnEngineInitialized;
        _engineInfoProvider.GameFrameUpdate += OnGameFrameUpdate;
        _engineInfoProvider.SelectObject += OnEngineObjectSelected;
        _engineInfoProvider.ConsoleMessage += OnEngineConsoleMessage;
        _engineInfoProvider.EngineShutdown += OnEngineShutdown;

        LoadEditorSettings();
    }

    private void OnEngineShutdown()
    {
        Dispose();
    }

    private void OnEngineInitialized(object sender, EventArgs e)
    {
        Console.WriteLine("Engine initialized, setting up ImGui...");

        InitializeImGui();
        DrawViewport(0);
    }

    private void InitializeImGui()
    {
        ImGui.CreateContext();
        ImGui.GetIO().FontGlobalScale = IMGUI_GLOBAL_FONT_SCALE;

        var resolution = _engineInfoProvider.GetResolution();
        _imGuiController = new ImGuiController(resolution.X, resolution.Y);
    }

    private void LoadEditorSettings()
    {
        _isDebugViewEnabled = _engineInfoProvider.GetIsDebugViewEnabled();
        _isGridEnabled = _engineInfoProvider.GetIsGridEnabled();
        _isWireframeEnabled = _engineInfoProvider.GetIsWireframeEnabled();
    }

    #endregion

    #region Event Handlers - Hierarchy

    private void OnHierarchyObjectSelected(GameObject selectedObject)
    {
        _inspector.UpdateInspector(selectedObject);
        _selectedObject = selectedObject;
    }

    private void OnHierarchyObjectFocused(GameObject focusedObject)
    {
        var renderer = focusedObject.GetComponent<Renderer>();

        if (renderer != null)
        {
            FocusCameraOnRenderer(renderer);
            return;
        }

        _sceneSystem.CurrentScene.UpdateCameraTarget(focusedObject);
    }

    private void FocusCameraOnRenderer(Renderer renderer)
    {
        var worldCenter = renderer.GetWorldCenter();
        _sceneSystem.CurrentScene.Camera.Transform.WorldPosition = worldCenter;
    }

    private void OnHierarchyFileDropped(string filePath)
    {
        _sceneSystem.CurrentScene.AddAssetToScene(filePath);
    }

    #endregion

    #region Event Handlers - Project

    private void OnProjectFileSelected(string filePath)
    {
        _sceneSystem.OnFileSelected(filePath);
    }

    #endregion

    #region Event Handlers - Engine

    private void OnEditorDraw(GameWindow window, int frameTextureId, FrameEventArgs frameEventArgs)
    {
        _imGuiController.Update(window, (float)frameEventArgs.Time);

        ImGui.NewFrame();
        DrawEditorUI();
        DrawViewport(frameTextureId);
        DebugConsole.Draw();
        _imGuiController.Render();
    }

    private void OnEditorResized(Vector2i newSize)
    {
        _imGuiController.WindowResized(newSize.X, newSize.Y);
    }

    private void OnTextInput(TextInputEventArgs textInputEventArgs)
    {
        _imGuiController.PressChar((char)textInputEventArgs.Unicode);
    }

    private void OnMouseWheel(MouseWheelEventArgs mouseWheelEventArgs)
    {
        _imGuiController.MouseScroll(mouseWheelEventArgs.Offset);
    }

    private void OnInspectorReset(object sender, EventArgs e)
    {
        _inspector.UpdateInspector(null);
        LoadEditorSettings();
    }

    private void OnEngineObjectSelected(object sender, Guid objectId)
    {
        _hierarchy.UpdateSelectedObject(objectId);
    }

    private void OnEngineConsoleMessage(object sender, ConsoleMessage consoleMessage)
    {
        DebugConsole.Log(consoleMessage.Message, consoleMessage.Type);
    }

    private void OnGameFrameUpdate(FrameEventArgs frameEventArgs, KeyboardState keyboardState, MouseState mouseState)
    {
        HandleKeyboardInput(keyboardState);
    }

    #endregion

    #region Input Handling

    private void HandleKeyboardInput(KeyboardState keyboardState)
    {
        // Save Scene (Cmd/Super + S)
        if (keyboardState.IsKeyPressed(Keys.S) && keyboardState.IsKeyDown(Keys.LeftSuper)) SaveCurrentScene();

        // Delete Selected Object (Cmd/Super + Backspace)
        if (keyboardState.IsKeyPressed(Keys.Backspace) && keyboardState.IsKeyDown(Keys.LeftSuper))
            DeleteSelectedObject();
    }

    private void SaveCurrentScene()
    {
        Console.WriteLine("Saving scene...");
        _sceneSystem.SaveActiveScene();
    }

    private void DeleteSelectedObject()
    {
        if (_selectedObject == null)
            return;

        Console.WriteLine("Deleting selected object...");

        _inspector.UpdateInspector(null);
        _hierarchy.ResetSelection();
        _selectedObject.Destroy();
        _selectedObject = null;
    }

    #endregion

    #region UI Drawing

    private void DrawEditorUI()
    {
        _hierarchy.Draw();
        _project.Draw();
        _inspector.Draw();
    }

    private void DrawViewport(int frameTextureId)
    {
        ImGui.Begin("Scene View");
        ImGui.SetWindowFontScale(IMGUI_FONT_SCALE);

        var contentSize = ImGui.GetContentRegionAvail();

        DrawViewportControls();

        if (frameTextureId >= 0)
            ImGui.Image(
                frameTextureId,
                contentSize,
                new System.Numerics.Vector2(0, 1),
                new System.Numerics.Vector2(1, 0)
            );

        UpdateViewportState(contentSize);

        ImGui.End();
    }

    private void DrawViewportControls()
    {
        var controlPosition = new System.Numerics.Vector2(
            0,
            VIEWPORT_TOGGLE_PADDING + ImGui.GetStyle().FramePadding.Y
        );

        ImGui.SetCursorPos(controlPosition);

        if (ImGui.BeginChild(
                "ViewportToggles",
                new System.Numerics.Vector2(VIEWPORT_TOGGLE_WIDTH, VIEWPORT_TOGGLE_HEIGHT),
                ImGuiChildFlags.None,
                ImGuiWindowFlags.NoScrollbar))
            DrawViewportToggles();

        ImGui.EndChild();

        UpdateEditorViewSettings();
    }

    private void DrawViewportToggles()
    {
        ImGui.Checkbox("Grid", ref _isGridEnabled);
        ImGui.SameLine();

        ImGui.Checkbox("Debug", ref _isDebugViewEnabled);
        ImGui.SameLine();

        ImGui.Checkbox("Wireframe", ref _isWireframeEnabled);
    }

    private void UpdateEditorViewSettings()
    {
        _editorInfoProvider.IsDebugViewEnabled = _isDebugViewEnabled;
        _editorInfoProvider.IsWireframeEnabled = _isWireframeEnabled;
        _editorInfoProvider.IsGridEnabled = _isGridEnabled;
    }

    private void UpdateViewportState(System.Numerics.Vector2 contentSize)
    {
        _isSceneViewHovered = ImGui.IsItemHovered();

        _editorInfoProvider.Viewport = new Vector2i((int)contentSize.X, (int)contentSize.Y);
        _editorInfoProvider.ViewportNdc = CalculateViewportNDC();

        if (_isSceneViewHovered && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)) HandleSceneViewDoubleClick();
    }

    #endregion

    #region Viewport Calculations

    private Vector2 CalculateViewportNDC()
    {
        var mousePosition = ImGui.GetMousePos();
        var viewportMin = ImGui.GetItemRectMin();
        var viewportMax = ImGui.GetItemRectMax();

        var viewportWidth = viewportMax.X - viewportMin.X;
        var viewportHeight = viewportMax.Y - viewportMin.Y;

        var localX = mousePosition.X - viewportMin.X;
        var localY = mousePosition.Y - viewportMin.Y;

        // Check if mouse is outside viewport or viewport has invalid dimensions
        if (!IsValidViewportCoordinate(viewportWidth, viewportHeight, localX, localY))
            return new Vector2(int.MinValue, int.MinValue);

        // Normalize to 0..1 range
        var normalizedX = localX / viewportWidth;
        var normalizedY = localY / viewportHeight;

        // Convert to NDC (flip Y-axis since ImGui Y increases downward)
        var ndcX = normalizedX;
        var ndcY = 1.0f - normalizedY;

        return new Vector2(ndcX, ndcY);
    }

    private bool IsValidViewportCoordinate(float width, float height, float localX, float localY)
    {
        return width > 0f
               && height > 0f
               && localX >= 0f
               && localY >= 0f
               && localX <= width
               && localY <= height;
    }

    private void HandleSceneViewDoubleClick()
    {
        _editorInfoProvider.OnSceneViewDoubleClicked();
    }

    #endregion

    #region Cleanup

    private void Dispose()
    {
        _imGuiController?.Dispose();
    }

    #endregion
}