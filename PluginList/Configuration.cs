using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using PluginList.Ui;

namespace PluginList
{
    // --- NEW: Sort Mode Enum ---
    public enum SortMode { Alphabetical, Manual }

    public class CustomCommand
    {
        public string Name { get; set; } = "";
        public string Command { get; set; } = "";
    }

    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        public bool IsEnabled { get; set; } = true;
        public DockEdge CurrentEdge { get; set; } = DockEdge.Right;
        public float EdgeOffset { get; set; } = 0f;

        // --- NEW: Save the user's sort preference ---
        public SortMode ShortcutSortMode { get; set; } = SortMode.Alphabetical;

        public List<string> EnabledPlugins { get; set; } = new();
        public List<CustomCommand> CustomCommands { get; set; } = new();

        public System.Collections.Generic.List<int> SavedMacros { get; set; } = new();
        public System.Collections.Generic.Dictionary<string, System.Numerics.Vector4> ItemColors { get; set; } = new();

        [NonSerialized] private IDalamudPluginInterface pluginInterface = null!;

        public void Initialize(IDalamudPluginInterface pInterface)
        {
            pluginInterface = pInterface;
        }

        public void Save()
        {
            pluginInterface.SavePluginConfig(this);
        }
    }
}
