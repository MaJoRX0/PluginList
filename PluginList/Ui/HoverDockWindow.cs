using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using System;
using System.Linq;
using System.Numerics;

namespace PluginList.Ui
{
    public enum DockEdge { Top, Bottom, Left, Right }

    public class HoverDockWindow : Window
    {
        private DockEdge CurrentEdge => Plugin.Config.CurrentEdge;
        private float EdgeOffset
        {
            get => Plugin.Config.EdgeOffset;
            set => Plugin.Config.EdgeOffset = value;
        }

        private bool isDraggingMenu = false;

        private float expandProgress = 0f;
        private const float AnimationSpeed = 8f;
        private const float CollapsedPanelLength = 0f;

        private float ExpandedPanelLength => 250f * ImGuiHelpers.GlobalScale;
        private float PanelThickness => 300f * ImGuiHelpers.GlobalScale;
        private float TabRadius => 25f * ImGuiHelpers.GlobalScale;

        private bool IsVertical => CurrentEdge == DockEdge.Left || CurrentEdge == DockEdge.Right;

        public HoverDockWindow() : base("Plugin Hover Dock",
            ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoSavedSettings)
        {
            IsOpen = Plugin.Config.IsEnabled;
            RespectCloseHotkey = false;
        }

        public override void PreDraw()
        {
            var viewport = ImGui.GetMainViewport();

            float eased = 1f - (1f - expandProgress) * (1f - expandProgress);
            float visiblePanelLength = CollapsedPanelLength + (ExpandedPanelLength - CollapsedPanelLength) * eased;
            float totalWindowLength = Math.Max(TabRadius, visiblePanelLength + TabRadius);

            float centerX = viewport.Pos.X + (viewport.Size.X * 0.5f) + (!IsVertical ? EdgeOffset : 0f);
            float centerY = viewport.Pos.Y + (viewport.Size.Y * 0.5f) + (IsVertical ? EdgeOffset : 0f);

            Vector2 pos = Vector2.Zero;
            Vector2 pivot = Vector2.Zero;

            if (CurrentEdge == DockEdge.Right) { pos = new Vector2(viewport.Pos.X + viewport.Size.X, centerY); pivot = new Vector2(1f, 0.5f); }
            if (CurrentEdge == DockEdge.Left) { pos = new Vector2(viewport.Pos.X, centerY); pivot = new Vector2(0f, 0.5f); }
            if (CurrentEdge == DockEdge.Top) { pos = new Vector2(centerX, viewport.Pos.Y); pivot = new Vector2(0.5f, 0f); }
            if (CurrentEdge == DockEdge.Bottom) { pos = new Vector2(centerX, viewport.Pos.Y + viewport.Size.Y); pivot = new Vector2(0.5f, 1f); }

            Vector2 nextSize = IsVertical
                ? new Vector2(totalWindowLength, PanelThickness)
                : new Vector2(PanelThickness, totalWindowLength);

            ImGui.SetNextWindowPos(pos, ImGuiCond.Always, pivot);
            ImGui.SetNextWindowSize(nextSize, ImGuiCond.Always);

            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0f, 10f * ImGuiHelpers.GlobalScale));

            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0f, 0f, 0f, 0f));
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(0.2f, 0.2f, 0.2f, 0.8f));
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(0.3f, 0.3f, 0.3f, 1.0f));
        }

        public override void Draw()
        {
            var io = ImGui.GetIO();
            float dt = io.DeltaTime;
            ChatExecutor.ProcessQueue(dt);

            var viewport = ImGui.GetMainViewport();
            var windowPos = ImGui.GetWindowPos();
            var windowSize = ImGui.GetWindowSize();
            var drawList = ImGui.GetWindowDrawList();

            float winX = windowPos.X;
            float winY = windowPos.Y;
            float winW = windowSize.X;
            float winH = windowSize.Y;

            float eased = 1f - (1f - expandProgress) * (1f - expandProgress);
            float visiblePanelLength = CollapsedPanelLength + (ExpandedPanelLength - CollapsedPanelLength) * eased;

            Vector2 panelMin = Vector2.Zero, panelMax = Vector2.Zero, tabCenter = Vector2.Zero;
            string chevronIcon = "";

            if (CurrentEdge == DockEdge.Right)
            {
                panelMin = new Vector2(winX + winW - visiblePanelLength, winY);
                panelMax = new Vector2(winX + winW, winY + winH);
                tabCenter = new Vector2(panelMin.X, winY + winH * 0.5f);
                chevronIcon = FontAwesomeIcon.ChevronLeft.ToIconString();
            }
            else if (CurrentEdge == DockEdge.Left)
            {
                panelMin = new Vector2(winX, winY);
                panelMax = new Vector2(winX + visiblePanelLength, winY + winH);
                tabCenter = new Vector2(panelMax.X, winY + winH * 0.5f);
                chevronIcon = FontAwesomeIcon.ChevronRight.ToIconString();
            }
            else if (CurrentEdge == DockEdge.Top)
            {
                panelMin = new Vector2(winX, winY);
                panelMax = new Vector2(winX + winW, winY + visiblePanelLength);
                tabCenter = new Vector2(winX + winW * 0.5f, panelMax.Y);
                chevronIcon = FontAwesomeIcon.ChevronDown.ToIconString();
            }
            else if (CurrentEdge == DockEdge.Bottom)
            {
                panelMin = new Vector2(winX, winY + winH - visiblePanelLength);
                panelMax = new Vector2(winX + winW, winY + winH);
                tabCenter = new Vector2(winX + winW * 0.5f, panelMin.Y);
                chevronIcon = FontAwesomeIcon.ChevronUp.ToIconString();
            }

            bool windowHovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup | ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
            bool tabHovered = Vector2.Distance(io.MousePos, tabCenter) <= (TabRadius + (2f * ImGuiHelpers.GlobalScale));

            bool edgeHover = false;
            float edgeTolerance = TabRadius + (8f * ImGuiHelpers.GlobalScale);

            if (CurrentEdge == DockEdge.Right)
                edgeHover = io.MousePos.X >= viewport.Pos.X + viewport.Size.X - edgeTolerance && io.MousePos.Y >= winY && io.MousePos.Y <= winY + winH;
            else if (CurrentEdge == DockEdge.Left)
                edgeHover = io.MousePos.X <= viewport.Pos.X + edgeTolerance && io.MousePos.Y >= winY && io.MousePos.Y <= winY + winH;
            else if (CurrentEdge == DockEdge.Top)
                edgeHover = io.MousePos.Y <= viewport.Pos.Y + edgeTolerance && io.MousePos.X >= winX && io.MousePos.X <= winX + winW;
            else if (CurrentEdge == DockEdge.Bottom)
                edgeHover = io.MousePos.Y >= viewport.Pos.Y + viewport.Size.Y - edgeTolerance && io.MousePos.X >= winX && io.MousePos.X <= winX + winW;

            if (windowHovered || tabHovered || edgeHover || isDraggingMenu)
                expandProgress += AnimationSpeed * dt;
            else
                expandProgress -= AnimationSpeed * dt;

            expandProgress = Math.Clamp(expandProgress, 0f, 1f);

            eased = 1f - (1f - expandProgress) * (1f - expandProgress);
            visiblePanelLength = CollapsedPanelLength + (ExpandedPanelLength - CollapsedPanelLength) * eased;

            if (CurrentEdge == DockEdge.Right) { panelMin.X = winX + winW - visiblePanelLength; tabCenter.X = panelMin.X; }
            else if (CurrentEdge == DockEdge.Left) { panelMax.X = winX + visiblePanelLength; tabCenter.X = panelMax.X; }
            else if (CurrentEdge == DockEdge.Top) { panelMax.Y = winY + visiblePanelLength; tabCenter.Y = panelMax.Y; }
            else if (CurrentEdge == DockEdge.Bottom) { panelMin.Y = winY + winH - visiblePanelLength; tabCenter.Y = panelMin.Y; }

            uint bgColor = ImGui.GetColorU32(new Vector4(0.08f, 0.08f, 0.09f, 0.95f));

            if (visiblePanelLength > 0.5f)
                drawList.AddRectFilled(panelMin, panelMax, bgColor);

            float tabAlpha = 1f - eased;
            if (tabAlpha > 0.02f)
            {
                uint tabColor = ImGui.GetColorU32(new Vector4(0.08f, 0.08f, 0.09f, 0.95f * tabAlpha));
                drawList.AddCircleFilled(tabCenter, TabRadius, tabColor, 40);

                ImGui.PushFont(UiBuilder.IconFont);
                Vector2 iconSize = ImGui.CalcTextSize(chevronIcon);
                ImGui.PopFont();

                Vector2 iconPos = tabCenter;
                float offset = TabRadius * 0.45f;
                if (CurrentEdge == DockEdge.Right) iconPos.X -= offset;
                else if (CurrentEdge == DockEdge.Left) iconPos.X += offset;
                else if (CurrentEdge == DockEdge.Top) iconPos.Y += offset;
                else if (CurrentEdge == DockEdge.Bottom) iconPos.Y -= offset;

                iconPos.X -= iconSize.X * 0.5f;
                iconPos.Y -= iconSize.Y * 0.5f;

                drawList.AddText(UiBuilder.IconFont, ImGui.GetFontSize(), iconPos, ImGui.GetColorU32(new Vector4(0.75f, 0.75f, 0.75f, tabAlpha)), chevronIcon);
            }

            if (eased > 0.05f && visiblePanelLength > (40f * ImGuiHelpers.GlobalScale))
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, eased);
                ImGui.PushClipRect(panelMin, panelMax, true);

                float innerPad = 16f * ImGuiHelpers.GlobalScale;
                float currentPanelWidth = IsVertical ? visiblePanelLength : PanelThickness;
                float contentWidth = Math.Max(1f, currentPanelWidth - innerPad * 2f);

                Vector2 contentStart = new Vector2(
                    (panelMin.X - winX) + innerPad,
                    (panelMin.Y - winY) + innerPad
                );

                ImGui.SetCursorPos(contentStart);
                float gearSize = 20f * ImGuiHelpers.GlobalScale;
                float dragWidth = contentWidth - gearSize - (8f * ImGuiHelpers.GlobalScale);
                float dragHeight = 20f * ImGuiHelpers.GlobalScale;

                ImGui.PushID("header_drag_zone");
                ImGui.InvisibleButton("##drag_handle", new Vector2(dragWidth, dragHeight));

                if (ImGui.IsItemActive())
                {
                    isDraggingMenu = true;
                    if (IsVertical) EdgeOffset += io.MouseDelta.Y;
                    else EdgeOffset += io.MouseDelta.X;
                }
                else if (isDraggingMenu)
                {
                    Plugin.Config.Save();
                    isDraggingMenu = false;
                }

                if (ImGui.IsItemHovered()) ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
                ImGui.PopID();

                ImGui.SetCursorPos(contentStart);
                ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), "SHORTCUTS");

                ImGui.SameLine(contentStart.X + contentWidth - gearSize);

                ImGui.PushID("pluginlist_main_settings");
                Vector2 gearCursor = ImGui.GetCursorPos();
                ImGui.InvisibleButton("##gear_hitbox", new Vector2(gearSize, gearSize));

                bool isGearHovered = ImGui.IsItemHovered();
                bool isGearClicked = ImGui.IsItemClicked();

                ImGui.SetCursorPos(gearCursor);
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextColored(
                    isGearHovered ? new Vector4(1f, 1f, 1f, 1f) : new Vector4(0.5f, 0.5f, 0.5f, 1f),
                    FontAwesomeIcon.Cog.ToIconString());
                ImGui.PopFont();

                if (isGearClicked) Plugin.Settings.IsOpen = true;
                if (isGearHovered) ImGui.SetTooltip("PluginList Settings");
                ImGui.PopID();

                ImGui.SetCursorPosX(contentStart.X);
                float sepY = ImGui.GetCursorScreenPos().Y;
                drawList.AddLine(
                    new Vector2(winX + contentStart.X, sepY),
                    new Vector2(winX + contentStart.X + contentWidth, sepY),
                    ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.12f)),
                    1f * ImGuiHelpers.GlobalScale
                );

                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (10f * ImGuiHelpers.GlobalScale));

                // ==========================================
                // 1. RENDER MAIN SHORTCUTS (Mixed List)
                // ==========================================
                var installedPlugins = Plugin.PluginInterface.InstalledPlugins.ToList();
                System.Collections.Generic.IEnumerable<string> sortedMainList = Plugin.Config.EnabledPlugins;

                if (Plugin.Config.ShortcutSortMode == SortMode.Alphabetical)
                {
                    sortedMainList = Plugin.Config.EnabledPlugins.OrderBy(item =>
                    {
                        if (item.StartsWith("CMD|")) return item.Substring(4);
                        if (item.StartsWith("MACRO|")) return ChatExecutor.GetMacroName(int.Parse(item.Substring(6)));
                        return installedPlugins.FirstOrDefault(p => p.InternalName == item)?.Name ?? item;
                    });
                }

                foreach (var item in sortedMainList)
                {
                    ImGui.SetCursorPosX(contentStart.X);

                    Vector4 defaultColor = item.StartsWith("CMD|") ? new Vector4(0.85f, 0.9f, 1f, 1f) :
                           item.StartsWith("MACRO|") ? new Vector4(0.85f, 1f, 0.85f, 1f) :
                           new Vector4(1f, 1f, 1f, 1f);

                    Vector4 itemColor = Plugin.Config.ItemColors.ContainsKey(item) ? Plugin.Config.ItemColors[item] : defaultColor;

                    if (item.StartsWith("CMD|"))
                    {
                        string cmdName = item.Substring(4);
                        var cmd = Plugin.Config.CustomCommands.FirstOrDefault(c => c.Name == cmdName);
                        if (cmd == null) continue;

                        ImGui.PushStyleColor(ImGuiCol.Text, itemColor);
                        if (ImGui.Selectable($"{cmd.Name}##{cmd.Command}_main", false, ImGuiSelectableFlags.None, new Vector2(contentWidth, 0)))
                        {
                            ChatExecutor.ExecuteCommand(cmd.Command);
                        }
                        ImGui.PopStyleColor();

                        if (ImGui.IsItemHovered()) ImGui.SetTooltip($"Command: {cmd.Command}");
                    }
                    // --- NEW: Draw FFXIV Macros ---
                    else if (item.StartsWith("MACRO|"))
                    {
                        if (int.TryParse(item.Substring(6), out int macroIndex))
                        {
                            // Get the clean name without [Indiv] tags
                            string macroName = ChatExecutor.GetMacroName(macroIndex);

                            // Give macros a very subtle green tint to differentiate them
                            ImGui.PushStyleColor(ImGuiCol.Text, itemColor);
                            if (ImGui.Selectable($"{macroName}##macro_{macroIndex}", false, ImGuiSelectableFlags.None, new Vector2(contentWidth, 0)))
                            {
                                ChatExecutor.ExecuteCommand($"//m {macroIndex}");
                            }
                            ImGui.PopStyleColor();

                            if (ImGui.IsItemHovered()) ImGui.SetTooltip("FFXIV Game Macro");
                        }
                    }
                    // ------------------------------
                    else
                    {
                        // Draw Plugin
                        var plugin = installedPlugins.FirstOrDefault(p => p.InternalName == item);
                        if (plugin == null || !plugin.IsLoaded) continue;

                        bool hasMain = plugin.HasMainUi;
                        bool hasConfig = plugin.HasConfigUi;

                        ImGui.PushStyleColor(ImGuiCol.Text, itemColor);
                        if (ImGui.Selectable($"{plugin.Name}##{plugin.InternalName}", false, ImGuiSelectableFlags.None, new Vector2(contentWidth, 0)))
                        {
                            if (hasMain) plugin.OpenMainUi();
                            else if (hasConfig) plugin.OpenConfigUi();
                        }
                        ImGui.PopStyleColor();

                        if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && hasConfig)
                            plugin.OpenConfigUi();

                        if (ImGui.IsItemHovered())
                        {
                            string tooltipText = "";
                            if (hasMain) tooltipText += "Left-Click: Open Menu\n";
                            if (hasConfig) tooltipText += "Right-Click: Open Settings";
                            if (!string.IsNullOrEmpty(tooltipText)) ImGui.SetTooltip(tooltipText.TrimEnd('\n'));
                        }
                    }
                }

                // ==========================================
                // 2. RENDER UNPINNED COMMANDS
                // ==========================================
                var unpinnedItems = new System.Collections.Generic.List<string>();
                foreach (var cmd in Plugin.Config.CustomCommands)
                    if (!Plugin.Config.EnabledPlugins.Contains("CMD|" + cmd.Name)) unpinnedItems.Add("CMD|" + cmd.Name);

                foreach (var m in Plugin.Config.SavedMacros)
                    if (!Plugin.Config.EnabledPlugins.Contains("MACRO|" + m)) unpinnedItems.Add("MACRO|" + m);

                if (sortedMainList.Any() && unpinnedItems.Count > 0)
                {
                    ImGui.SetCursorPosX(contentStart.X);
                    ImGui.Spacing();
                    float midSepY = ImGui.GetCursorScreenPos().Y;
                    drawList.AddLine(
                        new Vector2(winX + contentStart.X, midSepY),
                        new Vector2(winX + contentStart.X + contentWidth, midSepY),
                        ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.08f)),
                        1f * ImGuiHelpers.GlobalScale
                    );
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (10f * ImGuiHelpers.GlobalScale));
                }

                // Sort alphabetically if needed!
                System.Collections.Generic.IEnumerable<string> sortedUnpinned = unpinnedItems;
                if (Plugin.Config.ShortcutSortMode == SortMode.Alphabetical)
                {
                    sortedUnpinned = unpinnedItems.OrderBy(item =>
                    {
                        if (item.StartsWith("CMD|")) return item.Substring(4);
                        if (item.StartsWith("MACRO|")) return ChatExecutor.GetMacroName(int.Parse(item.Substring(6)));
                        return item;
                    });
                }

                foreach (var item in sortedUnpinned)
                {
                    ImGui.SetCursorPosX(contentStart.X);

                    Vector4 defaultColor = item.StartsWith("CMD|") ? new Vector4(0.85f, 0.9f, 1f, 1f) : new Vector4(0.85f, 1f, 0.85f, 1f);
                    Vector4 itemColor = Plugin.Config.ItemColors.ContainsKey(item) ? Plugin.Config.ItemColors[item] : defaultColor;

                    if (item.StartsWith("CMD|"))
                    {
                        string cmdName = item.Substring(4);
                        var cmd = Plugin.Config.CustomCommands.FirstOrDefault(c => c.Name == cmdName);
                        if (cmd == null) continue;

                        ImGui.PushStyleColor(ImGuiCol.Text, itemColor);
                        if (ImGui.Selectable($"{cmd.Name}##{cmd.Command}_unpinned", false, ImGuiSelectableFlags.None, new Vector2(contentWidth, 0)))
                        {
                            ChatExecutor.ExecuteCommand(cmd.Command);
                        }
                        ImGui.PopStyleColor();

                        if (ImGui.IsItemHovered()) ImGui.SetTooltip($"Command: {cmd.Command.Replace("\n", " ")}");
                    }
                    else if (item.StartsWith("MACRO|"))
                    {
                        if (int.TryParse(item.Substring(6), out int macroIndex))
                        {
                            string macroName = ChatExecutor.GetMacroName(macroIndex);

                            ImGui.PushStyleColor(ImGuiCol.Text, itemColor);
                            if (ImGui.Selectable($"{macroName}##macro_{macroIndex}_unpinned", false, ImGuiSelectableFlags.None, new Vector2(contentWidth, 0)))
                            {
                                ChatExecutor.ExecuteCommand($"//m {macroIndex}");
                            }
                            ImGui.PopStyleColor();

                            if (ImGui.IsItemHovered()) ImGui.SetTooltip("FFXIV Game Macro");
                        }
                    }
                }

                ImGui.Spacing();
                ImGui.PopClipRect();
                ImGui.PopStyleVar();
            }
        }

        public override void PostDraw()
        {
            ImGui.PopStyleVar(4);
            ImGui.PopStyleColor(3);
        }
    }
}
