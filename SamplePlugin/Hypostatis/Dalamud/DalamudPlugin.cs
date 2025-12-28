using System;
using System.Diagnostics;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Hypostasis.Debug;

namespace Hypostasis.Dalamud;

public abstract class DalamudPlugin : IDisposable
{
    private readonly PluginCommandManager pluginCommandManager;

    protected DalamudPlugin(IDalamudPluginInterface pluginInterface)
    {
#if DEBUG
        var stopwatch = Stopwatch.StartNew();
#endif

        try
        {
            if (this is not IDalamudPlugin plugin)
                throw new ApplicationException("A DalamudPlugin MUST implement IDalamudPlugin!");

            Hypostasis.Initialize(plugin, pluginInterface);
            SetupConfig();
            pluginCommandManager = new(this);
        }
        catch (Exception e)
        {
            DalamudApi.LogError($"Failed loading {nameof(Hypostasis)} for {Hypostasis.PluginName}", e);
            Dispose();
            Hypostasis.State = Hypostasis.PluginState.Failed;
            return;
        }

#if DEBUG
        var hypostasisMS = stopwatch.Elapsed.TotalMilliseconds;
        stopwatch.Restart();
#endif

        try
        {
            DalamudApi.SigScanner.InjectSignatures();
            Initialize();

            if (!PluginModuleManager.Initialize())
                DalamudApi.ShowNotification("One or more modules failed to load,\nplease check /xllog for more info", NotificationType.Warning, 10_000);

            var derivedType = GetType();

            if (derivedType.DeclaresMethod(nameof(Update)))
                DalamudApi.Framework.Update += Update;

            if (derivedType.DeclaresMethod(nameof(Draw)))
                DalamudApi.PluginInterface.UiBuilder.Draw += Draw;

            if (derivedType.DeclaresMethod(nameof(ToggleConfig)))
                DalamudApi.PluginInterface.UiBuilder.OpenConfigUi += ToggleConfig;

            Hypostasis.State = Hypostasis.PluginState.Loaded;

#if DEBUG
            DalamudApi.ShowNotification($"{nameof(Hypostasis)} initialized in {hypostasisMS} ms\nPlugin initialized in {stopwatch.Elapsed.TotalMilliseconds} ms", NotificationType.Info);
#endif
        }
        catch (Exception e)
        {
            // Excessive? Yes.
            var msg = $"Failed loading {Hypostasis.PluginName}";
            DalamudApi.LogError(msg, e);
            DalamudApi.ShowNotification($"\t\t\t{msg}\t\t\t\n\n", NotificationType.Error, 10_000);
            DalamudApi.ShowErrorToast(msg);
            DalamudApi.PrintError(msg);
            Dispose();
            Hypostasis.State = Hypostasis.PluginState.Failed;
        }

        DebugIPC.SetupDebugMembers();
    }

    protected virtual void Initialize() { }

    protected virtual void ToggleConfig() { }

    protected virtual void Update() { }

    private void Update(IFramework framework) => Update();

    protected virtual void Draw() { }

    protected virtual void SetupConfig() { }

    protected virtual void DisposeConfig() { }

    protected virtual void Dispose(bool disposing) { }

    public void Dispose()
    {
        var failed = Hypostasis.State == Hypostasis.PluginState.Loading;
        Hypostasis.State = Hypostasis.PluginState.Unloading;
        DisposeConfig();

        DalamudApi.Framework.Update -= Update;
        DalamudApi.PluginInterface.UiBuilder.Draw -= Draw;
        DalamudApi.PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfig;

        try
        {
            Dispose(true);
        }
        finally
        {
            pluginCommandManager?.Dispose();
            Hypostasis.Dispose(failed);

            Hypostasis.State = Hypostasis.PluginState.Unloaded;
            GC.SuppressFinalize(this);
        }
    }
}

public abstract class DalamudPlugin<C>(IDalamudPluginInterface pluginInterface) : DalamudPlugin(pluginInterface) where C : PluginConfiguration, new()
{
    public static C Config { get; private set; }
    protected sealed override void SetupConfig() => Config = PluginConfiguration.LoadConfig<C>();
    protected sealed override void DisposeConfig() => Config?.Save();
}

public abstract class DalamudPlugin<P, C> : DalamudPlugin<C> where P : DalamudPlugin where C : PluginConfiguration, new()
{
    public static P Plugin { get; private set; }
    protected DalamudPlugin(IDalamudPluginInterface pluginInterface) : base(pluginInterface) => Plugin = this as P;
}