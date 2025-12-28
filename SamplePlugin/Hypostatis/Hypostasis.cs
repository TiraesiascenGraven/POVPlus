global using Dalamud;
global using Hypostasis;
global using Hypostasis.Dalamud;
global using Hypostasis.Game;
using Dalamud.Plugin;
using Hypostasis.Debug;

namespace Hypostasis;

public static class Hypostasis
{
    public enum PluginState
    {
        Loading,
        Loaded,
        Unloading,
        Unloaded,
        Failed
    }

    public static string PluginName { get; private set; } = string.Empty;
    public static PluginState State { get; set; }
    public static bool IsDebug { get; }
#if DEBUG
        = true;
#endif

    public static void Initialize(IDalamudPlugin plugin, IDalamudPluginInterface pluginInterface)
    {
        PluginName = pluginInterface.InternalName;
        DalamudApi.Initialize(pluginInterface);
        Common.Initialize();
        DebugIPC.Initialize(plugin);
    }

    public static void Dispose(bool failed)
    {
        if (!failed)
            DebugIPC.Dispose();
        PluginModuleManager.Dispose();
        DalamudApi.Dispose();
        Common.Dispose();
        AsmPatch.DisposeAll();
    }
}