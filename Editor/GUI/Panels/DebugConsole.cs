using System.Numerics;
using Adapters;
using ImGuiNET;

namespace Editor.GUI;

internal static class DebugConsole
{
    private static readonly List<ConsoleMessage> _messages = new();
    private static readonly List<ConsoleMessage> _filteredMessages = new();

    private static string filterText = "";
    private static bool autoScroll = true;
    private static bool showLog = true;
    private static bool showWarnings = true;
    private static bool showErrors = true;
    private static bool collapseMessages = false;
    private static bool showTimestamps = true;
    private static bool isVisible = true;

    private static readonly Vector4 LogColor = new(1.0f, 1.0f, 1.0f, 1.0f); // White
    private static readonly Vector4 WarningColor = new(1.0f, 1.0f, 0.0f, 1.0f); // Yellow
    private static readonly Vector4 ErrorColor = new(1.0f, 0.3f, 0.3f, 1.0f); // Red

    public static void Log(string message, LogType type = LogType.Log)
    {
        AddMessage(message, type);
    }

    private static void AddMessage(string message, LogType type)
    {
        if (collapseMessages)
        {
            var existingMessage = _messages.LastOrDefault(m => m.Message == message && m.Type == type);
            if (existingMessage != null)
            {
                existingMessage.Count++;
                FilterMessages();
                return;
            }
        }

        var newMessage = new ConsoleMessage(message, type);
        _messages.Add(newMessage);

        // Limit message history to prevent memory issues
        if (_messages.Count > 1000) _messages.RemoveAt(0);

        FilterMessages();
    }

    private static void FilterMessages()
    {
        _filteredMessages.Clear();

        foreach (var message in _messages)
        {
            // Type filtering
            var passesTypeFilter = (message.Type == LogType.Log && showLog) ||
                                   (message.Type == LogType.Warning && showWarnings) ||
                                   (message.Type == LogType.Error && showErrors);

            if (!passesTypeFilter) continue;

            // Text filtering
            if (!string.IsNullOrEmpty(filterText) &&
                !message.Message.ToLower().Contains(filterText.ToLower()))
                continue;

            _filteredMessages.Add(message);
        }
    }

    public static void Draw()
    {
        if (!isVisible) return;

        ImGui.SetNextWindowSize(new Vector2(800, 400), ImGuiCond.FirstUseEver);

        if (ImGui.Begin("Debug Console", ref isVisible))
        {
            ImGui.SetWindowFontScale(2.0f);

            DrawToolbar();
            ImGui.Separator();

            DrawMessageList();
            ImGui.Separator();

            // DrawCommandInput();
        }

        ImGui.End();
    }

    private static void DrawToolbar()
    {
        // Filter buttons
        if (ImGui.Button($"Clear ({_messages.Count})"))
        {
            _messages.Clear();
            FilterMessages();
        }

        ImGui.SameLine();

        // Type filters
        if (ImGui.Checkbox("Log", ref showLog))
            FilterMessages();

        ImGui.SameLine();

        if (ImGui.Checkbox("Warning", ref showWarnings))
            FilterMessages();

        ImGui.SameLine();

        if (ImGui.Checkbox("Error", ref showErrors))
            FilterMessages();

        ImGui.SameLine();
        ImGui.Checkbox("Collapse", ref collapseMessages);

        ImGui.SameLine();
        ImGui.Checkbox("Timestamps", ref showTimestamps);

        ImGui.SameLine();
        ImGui.Checkbox("Auto Scroll", ref autoScroll);

        // Search filter
        ImGui.SetNextItemWidth(200);
        if (ImGui.InputText("Filter", ref filterText, 256)) FilterMessages();
    }

    private static void DrawMessageList()
    {
        var childSize = new Vector2(0, -ImGui.GetFrameHeightWithSpacing() - 10);

        if (ImGui.BeginChild("ScrollingRegion", childSize, ImGuiChildFlags.Borders,
                ImGuiWindowFlags.HorizontalScrollbar))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 1));

            foreach (var message in _filteredMessages) DrawMessage(message);

            // Auto scroll to bottom
            if (autoScroll && ImGui.GetScrollY() >= ImGui.GetScrollMaxY()) ImGui.SetScrollHereY(1.0f);

            ImGui.PopStyleVar();
        }

        ImGui.EndChild();
    }

    private static void DrawMessage(ConsoleMessage message)
    {
        // Create unique ID for each message using timestamp and message hash
        var uniqueId = $"{message.Timestamp.Ticks}_{message.Message.GetHashCode()}";
        ImGui.PushID(uniqueId);

        // Set text color based on message type
        var color = message.Type switch
        {
            LogType.Warning => WarningColor,
            LogType.Error => ErrorColor,
            _ => LogColor
        };

        ImGui.PushStyleColor(ImGuiCol.Text, color);

        var displayText = "";

        if (showTimestamps) displayText += $"[{message.Timestamp:HH:mm:ss}] ";

        displayText += message.Message;

        if (collapseMessages && message.Count > 1) displayText += $" ({message.Count})";

        // Make the entire line selectable for copying
        if (ImGui.Selectable(displayText, false)) ImGui.SetClipboardText(message.Message);

        // Show tooltip with full message on hover
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text($"Type: {message.Type}");
            ImGui.Text($"Time: {message.Timestamp:yyyy-MM-dd HH:mm:ss}");
            ImGui.Text($"Message: {message.Message}");
            if (collapseMessages && message.Count > 1) ImGui.Text($"Count: {message.Count}");
            ImGui.Text("Click to copy to clipboard");
            ImGui.EndTooltip();
        }

        ImGui.PopStyleColor();
        ImGui.PopID();
    }
}