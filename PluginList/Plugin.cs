using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using PluginList.Ui;

namespace PluginList
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "PluginList";

        // This makes your config globally accessible to fix the "does not contain a definition" error
        public static Configuration Config { get; private set; } = null!;

        public readonly WindowSystem WindowSystem = new("PluginList");
        // Change it to this:
        public static HoverDockWindow HoverDock { get; private set; } = null!;
        public static SettingsWindow Settings { get; private set; } = null!;


        [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;

        public Plugin(IDalamudPluginInterface pluginInterface, IGameInteropProvider interopProvider)
        {
            // Load the existing config or create a new one
            Config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Config.Initialize(PluginInterface);

            ChatExecutor.Initialize(interopProvider);
            // Initialize your windows
            HoverDock = new HoverDockWindow();
            Settings = new SettingsWindow();

            WindowSystem.AddWindow(HoverDock);
            WindowSystem.AddWindow(Settings); // Update this line too!

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI; // Links the Dalamud gear icon to your Settings
        }

        public void Dispose()
        {
            WindowSystem.RemoveAllWindows();
            PluginInterface.UiBuilder.Draw -= DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
        }

        private void DrawUI()
        {
            WindowSystem.Draw();
        }

        private void DrawConfigUI()
        {
            Settings.Toggle();
        }
    }
}
