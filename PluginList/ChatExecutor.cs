using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;
using FFXIVClientStructs.Interop;
using System;
using System.Collections.Generic;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureMacroModule;

namespace PluginList
{
    public unsafe class ChatExecutor
    {
        private static UIModule* uiModule;

        public delegate void ProcessChatBoxDelegate(UIModule* uiModule, nint message, nint unused, byte a4);

        [Signature("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B F2 48 8B F9 45 84 C9")]
        public static ProcessChatBoxDelegate? ProcessChatBox = null;

        // --- NEW: Queue System Variables ---
        private static Queue<string> commandQueue = new();
        private static float delayTimer = 0f;
        private const float DelayBetweenCommands = 0.1f; // 100 milliseconds
        // -----------------------------------

        public static void Initialize(IGameInteropProvider interopProvider)
        {
            uiModule = Framework.Instance()->GetUIModule();
            interopProvider.InitializeFromAttributes(new ChatExecutor());
        }

        public static void ExecuteCommand(string commandText)
        {
            if (string.IsNullOrWhiteSpace(commandText)) return;

            var lines = commandText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                string cmd = line.Trim();
                if (string.IsNullOrWhiteSpace(cmd)) continue;

                if (cmd.StartsWith("//m", StringComparison.OrdinalIgnoreCase))
                {
                    string macroStr = cmd.Substring(3).Trim();
                    if (int.TryParse(macroStr, out int macroIndex))
                    {
                        ExecuteGameMacro(macroIndex);
                        continue;
                    }
                }

                // Instead of firing instantly, we add it to the waiting line!
                commandQueue.Enqueue(cmd);
            }
        }

        // --- NEW: The Queue Processor ---
        // We will call this every frame to check if we need to fire a command
        public static void ProcessQueue(float deltaTime)
        {
            if (commandQueue.Count == 0 || ProcessChatBox == null) return;

            delayTimer -= deltaTime;

            if (delayTimer <= 0f)
            {
                string cmd = commandQueue.Dequeue();

                var utf8Cmd = Utf8String.FromString(cmd);
                try
                {
                    ProcessChatBox(uiModule, (nint)utf8Cmd, nint.Zero, 0);
                }
                finally
                {
                    utf8Cmd->Dtor(true);
                }

                // Reset the timer for the next command in the queue
                delayTimer = DelayBetweenCommands;
            }
        }

        private static void ExecuteGameMacro(int macroIndex)
        {
            // FFXIV only has 200 macros (0-99 Individual, 100-199 Shared)
            if (macroIndex < 0 || macroIndex >= 200) return;

            // Grab the native FFXIV modules
            var raptureShellModule = uiModule->GetRaptureShellModule();
            var raptureMacroModule = uiModule->GetRaptureMacroModule();

            if (raptureShellModule == null || raptureMacroModule == null) return;

            Macro* macroPtr = null;

            if (macroIndex < 100)
            {
                // GetPointer() safely grabs the memory address without needing C# pinning!
                macroPtr = raptureMacroModule->Individual.GetPointer(macroIndex);
            }
            else
            {
                macroPtr = raptureMacroModule->Shared.GetPointer(macroIndex - 100);
            }

            if (macroPtr != null)
            {
                // Trigger the macro exactly as if the user clicked it on their hotbar!
                raptureShellModule->ExecuteMacro(macroPtr);
            }
        }
        public static List<(int Index, string Name)> GetAvailableMacros()
        {
            var result = new List<(int Index, string Name)>();

            // Safety check to ensure the game is loaded
            if (uiModule == null) return result;

            var raptureMacroModule = uiModule->GetRaptureMacroModule();
            if (raptureMacroModule == null) return result;

            // 1. Fetch Individual Macros (Indices 0 - 99)
            for (int i = 0; i < 100; i++)
            {
                // Safely get the pointer without pinning!
                var macro = raptureMacroModule->Individual.GetPointer(i);
                if (macro == null) continue;

                string name = macro->Name.ToString();

                // A macro is considered "real" if it has a name OR if the user gave it an icon
                if (!string.IsNullOrWhiteSpace(name) || macro->IconId != 0)
                {
                    string displayName = string.IsNullOrWhiteSpace(name) ? $"Unnamed Macro #{i}" : name;
                    result.Add((i, $"[Indiv] {displayName}"));
                }
            }

            // 2. Fetch Shared Macros (Indices 100 - 199)
            for (int i = 0; i < 100; i++)
            {
                // Safely get the pointer without pinning!
                var macro = raptureMacroModule->Shared.GetPointer(i);
                if (macro == null) continue;

                string name = macro->Name.ToString();

                if (!string.IsNullOrWhiteSpace(name) || macro->IconId != 0)
                {
                    string displayName = string.IsNullOrWhiteSpace(name) ? $"Unnamed Macro #{i}" : name;
                    result.Add((i + 100, $"[Shared] {displayName}"));
                }
            }

            return result;
        }
        public static string GetMacroName(int index)
        {
            if (uiModule == null) return $"Macro #{index}";
            var raptureMacroModule = uiModule->GetRaptureMacroModule();
            if (raptureMacroModule == null) return $"Macro #{index}";

            Macro* macro = null;
            if (index < 100) macro = raptureMacroModule->Individual.GetPointer(index);
            else macro = raptureMacroModule->Shared.GetPointer(index - 100);

            if (macro == null) return $"Macro #{index}";

            string name = macro->Name.ToString();
            return string.IsNullOrWhiteSpace(name) ? $"Macro #{index}" : name;
        }
    }
}
