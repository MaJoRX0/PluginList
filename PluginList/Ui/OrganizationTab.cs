using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

namespace PluginList.Ui
{
    public static class OrganizationTab
    {
        private static int activeDragIndex = -1;
        private static string activeDragType = "";

        public static void Draw()
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
            if (ImGui.RadioButton("Manual (Drag & Drop)", ref sortMode, 1))
            {
                Plugin.Config.ShortcutSortMode = (SortMode)sortMode;
                Plugin.Config.Save();
            }

            ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

            if (Plugin.Config.ShortcutSortMode == SortMode.Manual)
            {
                ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), "MANUAL SORTING");
                ImGui.TextWrapped("Click and hold any item to drag it. Click the color square to change text color! Right-click the square to reset.");
                ImGui.Spacing(); ImGui.Spacing();

                bool hasChanges = false;

                // --- NEW: Combine Custom Commands and Macros into one Unpinned List! ---
                var unpinnedItems = new List<string>();
                foreach (var cmd in Plugin.Config.CustomCommands)
                    if (!Plugin.Config.EnabledPlugins.Contains("CMD|" + cmd.Name)) unpinnedItems.Add("CMD|" + cmd.Name);

                foreach (var m in Plugin.Config.SavedMacros)
                    if (!Plugin.Config.EnabledPlugins.Contains("MACRO|" + m)) unpinnedItems.Add("MACRO|" + m);
                // -----------------------------------------------------------------------

                // ==========================================
                // 1. MAIN SHORTCUTS LIST
                // ==========================================
                ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1f), "Main Shortcuts");

                DrawDropZone("main_top_drop", 0, unpinnedItems, ref hasChanges);

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

                    Vector4 defaultColor = isCmd ? new Vector4(0.85f, 0.9f, 1f, 1f) : isMacro ? new Vector4(0.85f, 1f, 0.85f, 1f) : new Vector4(1f, 1f, 1f, 1f);
                    Vector4 itemColor = Plugin.Config.ItemColors.ContainsKey(item) ? Plugin.Config.ItemColors[item] : defaultColor;

                    ImGui.PushID($"color_main_{i}");
                    if (ImGui.ColorEdit4("##picker", ref itemColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoLabel | ImGuiColorEditFlags.AlphaPreview))
                    {
                        Plugin.Config.ItemColors[item] = itemColor;
                        hasChanges = true;
                    }
                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    {
                        Plugin.Config.ItemColors.Remove(item);
                        hasChanges = true;
                    }
                    ImGui.PopID();

                    ImGui.SameLine();

                    ImGui.PushStyleColor(ImGuiCol.Text, itemColor);
                    ImGui.Selectable($"  {FontAwesomeIcon.GripLines.ToIconString()}   {displayName}##main_{i}");
                    ImGui.PopStyleColor();

                    if (ImGui.BeginDragDropSource())
                    {
                        activeDragIndex = i;
                        activeDragType = "DND_MAIN";
                        ImGui.SetDragDropPayload("DND_MAIN", System.Array.Empty<byte>(), ImGuiCond.None);
                        ImGui.Text($"Moving: {displayName}");
                        ImGui.EndDragDropSource();
                    }

                    if (ImGui.BeginDragDropTarget())
                    {
                        if (activeDragType == "DND_MAIN") ImGui.AcceptDragDropPayload("DND_MAIN");
                        else if (activeDragType == "DND_UNPINNED") ImGui.AcceptDragDropPayload("DND_UNPINNED");

                        if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                        {
                            if (activeDragType == "DND_MAIN" && activeDragIndex != -1 && activeDragIndex != i)
                            {
                                var movedItem = Plugin.Config.EnabledPlugins[activeDragIndex];
                                Plugin.Config.EnabledPlugins.RemoveAt(activeDragIndex);
                                Plugin.Config.EnabledPlugins.Insert(i, movedItem);
                                hasChanges = true;
                            }
                            else if (activeDragType == "DND_UNPINNED" && activeDragIndex != -1)
                            {
                                var itemToPin = unpinnedItems[activeDragIndex];
                                Plugin.Config.EnabledPlugins.Insert(i, itemToPin);
                                hasChanges = true;
                            }
                            activeDragIndex = -1;
                        }
                        ImGui.EndDragDropTarget();
                    }
                }

                ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

                // ==========================================
                // 2. UNPINNED SHORTCUTS LIST
                // ==========================================
                ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1f), "Unpinned Shortcuts");

                DrawDropZone("unpinned_top_drop", 0, unpinnedItems, ref hasChanges, true);

                for (int i = 0; i < unpinnedItems.Count; i++)
                {
                    string item = unpinnedItems[i];
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

                    Vector4 defaultColor = isCmd ? new Vector4(0.85f, 0.9f, 1f, 1f) : new Vector4(0.85f, 1f, 0.85f, 1f);
                    Vector4 itemColor = Plugin.Config.ItemColors.ContainsKey(item) ? Plugin.Config.ItemColors[item] : defaultColor;

                    ImGui.PushID($"color_unpinned_{i}");
                    if (ImGui.ColorEdit4("##picker", ref itemColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoLabel | ImGuiColorEditFlags.AlphaPreview))
                    {
                        Plugin.Config.ItemColors[item] = itemColor;
                        hasChanges = true;
                    }
                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    {
                        Plugin.Config.ItemColors.Remove(item);
                        hasChanges = true;
                    }
                    ImGui.PopID();

                    ImGui.SameLine();

                    ImGui.PushStyleColor(ImGuiCol.Text, itemColor);
                    ImGui.Selectable($"  {FontAwesomeIcon.GripLines.ToIconString()}   {displayName}##unpinned_{i}");
                    ImGui.PopStyleColor();

                    if (ImGui.BeginDragDropSource())
                    {
                        activeDragIndex = i;
                        activeDragType = "DND_UNPINNED";
                        ImGui.SetDragDropPayload("DND_UNPINNED", System.Array.Empty<byte>(), ImGuiCond.None);
                        ImGui.Text($"Pinning: {displayName}");
                        ImGui.EndDragDropSource();
                    }

                    if (ImGui.BeginDragDropTarget())
                    {
                        // Accept both Main (for demoting) and Unpinned (for reordering)
                        if (activeDragType == "DND_MAIN") ImGui.AcceptDragDropPayload("DND_MAIN");
                        else if (activeDragType == "DND_UNPINNED") ImGui.AcceptDragDropPayload("DND_UNPINNED");

                        if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                        {
                            // --- RESTORED: Reordering within the Unpinned List! ---
                            if (activeDragType == "DND_UNPINNED" && activeDragIndex != -1 && activeDragIndex != i)
                            {
                                string sourceItem = unpinnedItems[activeDragIndex];
                                string targetItem = unpinnedItems[i];

                                // Reorder Custom Commands
                                if (sourceItem.StartsWith("CMD|") && targetItem.StartsWith("CMD|"))
                                {
                                    var cmd1 = Plugin.Config.CustomCommands.First(c => c.Name == sourceItem.Substring(4));
                                    var cmd2 = Plugin.Config.CustomCommands.First(c => c.Name == targetItem.Substring(4));
                                    int idx1 = Plugin.Config.CustomCommands.IndexOf(cmd1);
                                    int idx2 = Plugin.Config.CustomCommands.IndexOf(cmd2);

                                    (Plugin.Config.CustomCommands[idx1], Plugin.Config.CustomCommands[idx2]) = (Plugin.Config.CustomCommands[idx2], Plugin.Config.CustomCommands[idx1]);
                                    hasChanges = true;
                                }
                                // Reorder Saved Macros
                                else if (sourceItem.StartsWith("MACRO|") && targetItem.StartsWith("MACRO|"))
                                {
                                    int m1 = int.Parse(sourceItem.Substring(6));
                                    int m2 = int.Parse(targetItem.Substring(6));
                                    int idx1 = Plugin.Config.SavedMacros.IndexOf(m1);
                                    int idx2 = Plugin.Config.SavedMacros.IndexOf(m2);

                                    (Plugin.Config.SavedMacros[idx1], Plugin.Config.SavedMacros[idx2]) = (Plugin.Config.SavedMacros[idx2], Plugin.Config.SavedMacros[idx1]);
                                    hasChanges = true;
                                }
                            }
                            // --- Demoting from Main List ---
                            else if (activeDragType == "DND_MAIN" && activeDragIndex != -1)
                            {
                                string movedItem = Plugin.Config.EnabledPlugins[activeDragIndex];
                                if (movedItem.StartsWith("CMD|") || movedItem.StartsWith("MACRO|"))
                                {
                                    Plugin.Config.EnabledPlugins.RemoveAt(activeDragIndex);
                                    hasChanges = true;
                                }
                            }

                            activeDragIndex = -1; // Reset tracker
                        }
                        ImGui.EndDragDropTarget();
                    }
                }

                if (hasChanges) Plugin.Config.Save();
            }
        }

        private static void DrawDropZone(string id, int targetIndex, List<string> unpinnedItems, ref bool hasChanges, bool isUnpinnedZone = false)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
            ImGui.InvisibleButton(id, new Vector2(ImGui.GetContentRegionAvail().X, 4f * ImGuiHelpers.GlobalScale));
            ImGui.PopStyleVar();

            if (ImGui.BeginDragDropTarget())
            {
                if (activeDragType == "DND_MAIN") ImGui.AcceptDragDropPayload("DND_MAIN");
                else if (activeDragType == "DND_UNPINNED") ImGui.AcceptDragDropPayload("DND_UNPINNED");

                if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                {
                    if (!isUnpinnedZone)
                    {
                        if (activeDragType == "DND_MAIN" && activeDragIndex != -1)
                        {
                            var movedItem = Plugin.Config.EnabledPlugins[activeDragIndex];
                            Plugin.Config.EnabledPlugins.RemoveAt(activeDragIndex);
                            Plugin.Config.EnabledPlugins.Insert(targetIndex > Plugin.Config.EnabledPlugins.Count ? Plugin.Config.EnabledPlugins.Count : targetIndex, movedItem);
                            hasChanges = true;
                        }
                        else if (activeDragType == "DND_UNPINNED" && activeDragIndex != -1)
                        {
                            Plugin.Config.EnabledPlugins.Insert(targetIndex > Plugin.Config.EnabledPlugins.Count ? Plugin.Config.EnabledPlugins.Count : targetIndex, unpinnedItems[activeDragIndex]);
                            hasChanges = true;
                        }
                    }
                    else
                    {
                        if (activeDragType == "DND_MAIN" && activeDragIndex != -1)
                        {
                            string movedItem = Plugin.Config.EnabledPlugins[activeDragIndex];
                            if (movedItem.StartsWith("CMD|") || movedItem.StartsWith("MACRO|"))
                            {
                                Plugin.Config.EnabledPlugins.RemoveAt(activeDragIndex);
                                hasChanges = true;
                            }
                        }
                    }

                    activeDragIndex = -1;
                }
                ImGui.EndDragDropTarget();
            }
        }
    }
}
