using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using System;
using System.Linq;
using System.Numerics;

namespace PluginList.Ui
{
    public class SettingsWindow : Window
    {
        private string searchQuery = string.Empty;
        private string newCommandName = "";
        private string newCommandAction = "";
        private int editingCommandIndex = -1;

        public SettingsWindow() : base("PluginList Settings")
        {
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(350, 450),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };
        }

        public override void Draw()
        {
            if (ImGui.BeginTabBar("SettingsTabBar"))
            {
                if (ImGui.BeginTabItem("General & Plugins"))
                {
                    ImGui.Spacing(); DrawGeneralTab(); ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Custom Commands"))
                {
                    ImGui.Spacing(); DrawCustomCommandsTab(); ImGui.EndTabItem();
                }

                // --- NEW TAB ---
                if (ImGui.BeginTabItem("In-Game Macros"))
                {
                    ImGui.Spacing(); DrawGameMacrosTab(); ImGui.EndTabItem();
                }
                // ---------------

                if (ImGui.BeginTabItem("Organization"))
                {
                    ImGui.Spacing(); OrganizationTab.Draw(); ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
        }

        private void DrawGeneralTab()
        {
            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), "GENERAL");
            ImGui.Spacing();

            bool isEnabled = Plugin.Config.IsEnabled;
            if (ImGui.Checkbox("Enable Hover Dock", ref isEnabled))
            {
                Plugin.Config.IsEnabled = isEnabled;
                Plugin.Config.Save();
                Plugin.HoverDock.IsOpen = isEnabled;
            }

            ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), "APPEARANCE");
            ImGui.Spacing();

            int selectedEdge = (int)Plugin.Config.CurrentEdge;
            string[] edgeNames = Enum.GetNames(typeof(DockEdge));
            ImGui.SetNextItemWidth(150f * ImGuiHelpers.GlobalScale);

            if (ImGui.Combo("Screen Edge", ref selectedEdge, edgeNames, edgeNames.Length))
            {
                Plugin.Config.CurrentEdge = (DockEdge)selectedEdge;
                Plugin.Config.EdgeOffset = 0f;
                Plugin.Config.Save();
            }

            ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), "ENABLED PLUGINS");
            ImGui.Spacing();

            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputTextWithHint("##plugin_search", "Search plugins...", ref searchQuery, 100);
            ImGui.Spacing();

            var installedPlugins = Plugin.PluginInterface.InstalledPlugins
                .Where(p => p.IsLoaded)
                .Where(p => string.IsNullOrWhiteSpace(searchQuery) || p.Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.Name)
                .ToList();

            bool hasChanges = false;

            foreach (var plugin in installedPlugins)
            {
                bool isPluginEnabled = Plugin.Config.EnabledPlugins.Contains(plugin.InternalName);
                if (ImGui.Checkbox($"##check_{plugin.InternalName}", ref isPluginEnabled))
                {
                    if (isPluginEnabled) Plugin.Config.EnabledPlugins.Add(plugin.InternalName);
                    else Plugin.Config.EnabledPlugins.Remove(plugin.InternalName);
                    hasChanges = true;
                }
                ImGui.SameLine();
                ImGui.Text(plugin.Name);
            }

            if (hasChanges) Plugin.Config.Save();
        }

        private void DrawCustomCommandsTab()
        {
            bool isEditing = editingCommandIndex != -1;

            // Dynamically change the title based on what we are doing
            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), isEditing ? "EDIT COMMAND" : "ADD NEW COMMAND");
            ImGui.Spacing();

            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputTextWithHint("##new_cmd_name", "Button Name (e.g. Enter GPose)", ref newCommandName, 50);

            ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1f), "Command Text:");
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputTextMultiline("##new_cmd_action", ref newCommandAction, 1000, new Vector2(0, 75f * ImGuiHelpers.GlobalScale));

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("You can write multiple lines!\nUse '//m 0' for Individual Macro #0\nUse '//m 100' for Shared Macro #0");
            }

            // Determine button width so we can fit a Cancel button next to it when editing
            float buttonWidth = isEditing ? (ImGui.GetContentRegionAvail().X / 2f) - (4f * ImGuiHelpers.GlobalScale) : ImGui.GetContentRegionAvail().X;

            if (ImGui.Button(isEditing ? "Save Changes" : "Add Command", new Vector2(buttonWidth, 0)))
            {
                if (!string.IsNullOrWhiteSpace(newCommandName) && !string.IsNullOrWhiteSpace(newCommandAction))
                {
                    if (isEditing)
                    {
                        var cmd = Plugin.Config.CustomCommands[editingCommandIndex];
                        string oldPinTag = "CMD|" + cmd.Name;
                        string newPinTag = "CMD|" + newCommandName;

                        // 1. Update the actual command
                        cmd.Name = newCommandName;
                        cmd.Command = newCommandAction;

                        // 2. If they changed the name, update the pinned list so it doesn't vanish from the dock!
                        if (oldPinTag != newPinTag)
                        {
                            int pinIndex = Plugin.Config.EnabledPlugins.IndexOf(oldPinTag);
                            if (pinIndex != -1) Plugin.Config.EnabledPlugins[pinIndex] = newPinTag;
                        }

                        editingCommandIndex = -1; // Exit edit mode
                    }
                    else
                    {
                        Plugin.Config.CustomCommands.Add(new CustomCommand { Name = newCommandName, Command = newCommandAction });
                    }

                    Plugin.Config.Save();
                    newCommandName = "";
                    newCommandAction = "";
                }
            }

            if (isEditing)
            {
                ImGui.SameLine();
                if (ImGui.Button("Cancel", new Vector2(ImGui.GetContentRegionAvail().X, 0)))
                {
                    editingCommandIndex = -1;
                    newCommandName = "";
                    newCommandAction = "";
                }
            }

            ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), "YOUR COMMANDS");
            ImGui.Spacing();

            bool hasChanges = false;

            // Loop backwards to safely allow deletion
            for (int i = Plugin.Config.CustomCommands.Count - 1; i >= 0; i--)
            {
                var cmd = Plugin.Config.CustomCommands[i];

                // 1. Delete Button
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.6f, 0.2f, 0.2f, 0.8f));
                if (ImGui.Button($"X##del_{i}"))
                {
                    Plugin.Config.EnabledPlugins.Remove("CMD|" + cmd.Name);
                    Plugin.Config.CustomCommands.RemoveAt(i);

                    // If they delete the command they are currently editing, reset the editor!
                    if (editingCommandIndex == i)
                    {
                        editingCommandIndex = -1;
                        newCommandName = "";
                        newCommandAction = "";
                    }
                    else if (editingCommandIndex > i) editingCommandIndex--; // Shift the tracker

                    hasChanges = true;
                }
                ImGui.PopStyleColor();

                ImGui.SameLine();

                // 2. Edit Button (Pencil Icon)
                ImGui.PushID($"edit_btn_{i}");
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Edit))
                {
                    editingCommandIndex = i;
                    newCommandName = cmd.Name;
                    newCommandAction = cmd.Command;
                }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Edit Command");
                ImGui.PopID();

                ImGui.SameLine();

                // 3. Display Name and Command
                ImGui.AlignTextToFramePadding();
                ImGui.Text(cmd.Name);
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), $"({cmd.Command.Replace("\n", " ")})");
            }

            if (hasChanges) Plugin.Config.Save();
        }

        private void DrawGameMacrosTab()
        {
            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), "IN-GAME MACROS");
            ImGui.TextWrapped("Check the box to pin a macro directly to your Hover Dock.");
            ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

            var macros = ChatExecutor.GetAvailableMacros();

            if (macros.Count == 0)
            {
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "No named or icon-assigned macros found.");
                return;
            }

            bool hasChanges = false;

            if (ImGui.BeginChild("MacroScrollArea", new Vector2(0, 0), true))
            {
                foreach (var macro in macros)
                {
                    string macroTag = $"MACRO|{macro.Index}";

                    // --- FIXED: Check our new SavedMacros list! ---
                    bool isEnabled = Plugin.Config.SavedMacros.Contains(macro.Index);

                    ImGui.PushID($"macro_chk_{macro.Index}");
                    if (ImGui.Checkbox("", ref isEnabled))
                    {
                        if (isEnabled)
                        {
                            Plugin.Config.SavedMacros.Add(macro.Index);
                            Plugin.Config.EnabledPlugins.Add(macroTag); // Auto-pin to the main dock when first checked!
                        }
                        else
                        {
                            Plugin.Config.SavedMacros.Remove(macro.Index);
                            Plugin.Config.EnabledPlugins.Remove(macroTag);
                        }
                        hasChanges = true;
                    }
                    ImGui.PopID();

                    ImGui.SameLine();
                    ImGui.Text(macro.Name);
                }
                ImGui.EndChild();
            }

            if (hasChanges) Plugin.Config.Save();
        }

        private void DrawOrganizationTab()
        {
            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), "SORTING MODE");
            ImGui.Spacing();

            int sortMode = (int)Plugin.Config.ShortcutSortMode;
            if (ImGui.RadioButton("Alphabetical", ref sortMode, 0))
            {
                Plugin.Config.ShortcutSortMode = (SortMode)sortMode;
                Plugin.Config.Save();
            }
            ImGui.SameLine();
            if (ImGui.RadioButton("Manual", ref sortMode, 1))
            {
                Plugin.Config.ShortcutSortMode = (SortMode)sortMode;
                Plugin.Config.Save();
            }

            ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

            if (Plugin.Config.ShortcutSortMode == SortMode.Manual)
            {
                ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), "MANUAL SORTING");
                ImGui.TextWrapped("Use the arrows to arrange your shortcuts. Use the bottom arrows to remove items from the list.");
                ImGui.Spacing();

                bool hasChanges = false;
                float buttonSize = 24f * ImGuiHelpers.GlobalScale;

                ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1f), "Main Shortcuts");
                for (int i = 0; i < Plugin.Config.EnabledPlugins.Count; i++)
                {
                    string item = Plugin.Config.EnabledPlugins[i];
                    bool isCmd = item.StartsWith("CMD|");
                    bool isMacro = item.StartsWith("MACRO|");
                    string displayName = item;

                    if (isCmd)
                    {
                        var cmdData = Plugin.Config.CustomCommands.FirstOrDefault(c => c.Name == item.Substring(4));
                        displayName = cmdData != null ? $"{cmdData.Name} ({cmdData.Command.Replace("\n", " ")})" : item;
                    }
                    else if (isMacro)
                    {
                        if (int.TryParse(item.Substring(6), out int mIdx))
                            displayName = $"[Macro] {ChatExecutor.GetMacroName(mIdx)}";
                    }
                    else
                    {
                        var plugin = Plugin.PluginInterface.InstalledPlugins.FirstOrDefault(p => p.InternalName == item);
                        if (plugin != null) displayName = plugin.Name;
                    }

                    ImGui.PushID($"main_sort_{i}");

                    if (i > 0)
                    {
                        if (ImGuiComponents.IconButton(FontAwesomeIcon.ArrowUp))
                        {
                            (Plugin.Config.EnabledPlugins[i], Plugin.Config.EnabledPlugins[i - 1]) = (Plugin.Config.EnabledPlugins[i - 1], Plugin.Config.EnabledPlugins[i]);
                            hasChanges = true;
                        }
                    }
                    else ImGui.Dummy(new Vector2(buttonSize, buttonSize));

                    ImGui.SameLine();

                    if (i < Plugin.Config.EnabledPlugins.Count - 1)
                    {
                        if (ImGuiComponents.IconButton(FontAwesomeIcon.ArrowDown))
                        {
                            (Plugin.Config.EnabledPlugins[i], Plugin.Config.EnabledPlugins[i + 1]) = (Plugin.Config.EnabledPlugins[i + 1], Plugin.Config.EnabledPlugins[i]);
                            hasChanges = true;
                        }
                    }
                    else if (isCmd) // Unpin commands/macros!
                    {
                        if (ImGuiComponents.IconButton(FontAwesomeIcon.ArrowDown))
                        {
                            Plugin.Config.EnabledPlugins.RemoveAt(i);
                            hasChanges = true;
                        }
                        if (ImGui.IsItemHovered()) ImGui.SetTooltip(isCmd ? "Demote to Unpinned Commands" : "Unpin Macro");
                    }
                    else ImGui.Dummy(new Vector2(buttonSize, buttonSize));

                    ImGui.SameLine();
                    ImGui.AlignTextToFramePadding();

                    if (isCmd) ImGui.TextColored(new Vector4(0.85f, 0.9f, 1f, 1f), displayName);
                    else if (isMacro) ImGui.TextColored(new Vector4(0.85f, 1f, 0.85f, 1f), displayName); // Give macros a slight green tint!
                    else ImGui.Text(displayName);

                    ImGui.PopID();
                }

                ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

                // --- UNPINNED COMMANDS LIST ---
                ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1f), "Unpinned Custom Commands");
                var unpinnedCmds = Plugin.Config.CustomCommands.Where(c => !Plugin.Config.EnabledPlugins.Contains("CMD|" + c.Name)).ToList();

                for (int i = 0; i < unpinnedCmds.Count; i++)
                {
                    var cmd = unpinnedCmds[i];
                    ImGui.PushID($"cmd_sort_{i}");

                    if (i > 0)
                    {
                        if (ImGuiComponents.IconButton(FontAwesomeIcon.ArrowUp))
                        {
                            int idx1 = Plugin.Config.CustomCommands.IndexOf(unpinnedCmds[i]);
                            int idx2 = Plugin.Config.CustomCommands.IndexOf(unpinnedCmds[i - 1]);
                            (Plugin.Config.CustomCommands[idx1], Plugin.Config.CustomCommands[idx2]) = (Plugin.Config.CustomCommands[idx2], Plugin.Config.CustomCommands[idx1]);
                            hasChanges = true;
                        }
                    }
                    else // Promote to Main List!
                    {
                        if (ImGuiComponents.IconButton(FontAwesomeIcon.ArrowUp))
                        {
                            Plugin.Config.EnabledPlugins.Add("CMD|" + cmd.Name);
                            hasChanges = true;
                        }
                        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Promote to Main Shortcuts");
                    }

                    ImGui.SameLine();

                    if (i < unpinnedCmds.Count - 1)
                    {
                        if (ImGuiComponents.IconButton(FontAwesomeIcon.ArrowDown))
                        {
                            int idx1 = Plugin.Config.CustomCommands.IndexOf(unpinnedCmds[i]);
                            int idx2 = Plugin.Config.CustomCommands.IndexOf(unpinnedCmds[i + 1]);
                            (Plugin.Config.CustomCommands[idx1], Plugin.Config.CustomCommands[idx2]) = (Plugin.Config.CustomCommands[idx2], Plugin.Config.CustomCommands[idx1]);
                            hasChanges = true;
                        }
                    }
                    else ImGui.Dummy(new Vector2(buttonSize, buttonSize));

                    ImGui.SameLine();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text($"{cmd.Name} ({cmd.Command.Replace("\n", " ")})");

                    ImGui.PopID();
                }

                if (hasChanges) Plugin.Config.Save();
            }
        }
    }
}
