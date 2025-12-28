using System;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Gui.Toast;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace Hypostasis.Dalamud;

public class DalamudApi
{
    //[PluginService]
    public static IDalamudPluginInterface PluginInterface { get; private set; }

    [PluginService]
    public static IAddonEventManager AddonEventManager { get; private set; }

    [PluginService]
    public static IAddonLifecycle AddonLifecycle { get; private set; }

    [PluginService]
    public static IAetheryteList AetheryteList { get; private set; }

    [PluginService]
    public static IBuddyList BuddyList { get; private set; }

    [PluginService]
    public static IChatGui ChatGui { get; private set; }

    [PluginService]
    public static IClientState ClientState { get; private set; }

    [PluginService]
    public static ICommandManager CommandManager { get; private set; }

    [PluginService]
    public static ICondition Condition { get; private set; }

    [PluginService]
    public static IDataManager DataManager { get; private set; }

    [PluginService]
    public static IDtrBar DtrBar { get; private set; }

    [PluginService]
    public static IDutyState DutyState { get; private set; }

    [PluginService]
    public static IFateTable FateTable { get; private set; }

    [PluginService]
    public static IFlyTextGui FlyTextGui { get; private set; }

    [PluginService]
    public static IFramework Framework { get; private set; }

    [PluginService]
    public static IGameConfig GameConfig { get; private set; }

    [PluginService]
    public static IGameGui GameGui { get; private set; }

    [PluginService]
    public static IGameInteropProvider GameInteropProvider { get; private set; }

    [PluginService]
    public static IGameLifecycle GameLifecycle { get; private set; }

    [PluginService]
    public static IGamepadState GamepadState { get; private set; }

    [PluginService]
    public static IJobGauges JobGauges { get; private set; }

    [PluginService]
    public static IKeyState KeyState { get; private set; }

    [PluginService]
    public static INotificationManager NotificationManager { get; private set; }

    [PluginService]
    public static IObjectTable ObjectTable { get; private set; }

    [PluginService]
    public static IPartyFinderGui PartyFinderGui { get; private set; }

    [PluginService]
    public static IPartyList PartyList { get; private set; }

    [PluginService]
    public static IPluginLog PluginLog { get; private set; }

    [PluginService]
    private static ISigScanner sigScanner
    {
        set => SigScanner = new(value);
    }

    public static SigScannerWrapper SigScanner { get; private set; }

    [PluginService]
    public static ITargetManager TargetManager { get; private set; }

    [PluginService]
    public static ITextureProvider TextureProvider { get; private set; }

    [PluginService]
    public static ITitleScreenMenu TitleScreenMenu { get; private set; }

    [PluginService]
    public static IToastGui ToastGui { get; private set; }

    private static readonly string printName = Hypostasis.PluginName;
    private static readonly string printHeader = $"[{printName}] ";

    public DalamudApi(IDalamudPluginInterface pluginInterface)
    {
        PluginInterface = pluginInterface;
        if (!pluginInterface.Inject(this))
            throw new ApplicationException("Failed loading DalamudApi!");
    }

    public static void PrintEcho(string message) => ChatGui.Print($"{printHeader}{message}");

    public static void PrintError(string message) => ChatGui.PrintError($"{printHeader}{message}");

    public static void ShowNotification(string message, NotificationType type = NotificationType.None, uint msDelay = 3_000u) => NotificationManager.AddNotification(new Notification { Type = type, Title = printName, Content = message, InitialDuration = TimeSpan.FromMilliseconds(msDelay) });

    public static void ShowToast(string message, ToastOptions options = null) => ToastGui.ShowNormal($"{printHeader}{message}", options);

    public static void ShowQuestToast(string message, QuestToastOptions options = null) => ToastGui.ShowQuest($"{printHeader}{message}", options);

    public static void ShowErrorToast(string message) => ToastGui.ShowError($"{printHeader}{message}");

    public static void LogVerbose(string message, Exception exception = null) => PluginLog.Verbose(exception, message);

    public static void LogDebug(string message, Exception exception = null) => PluginLog.Debug(exception, message);

    public static void LogInfo(string message, Exception exception = null) => PluginLog.Information(exception, message);

    public static void LogWarning(string message, Exception exception = null) => PluginLog.Warning(exception, message);

    public static void LogError(string message, Exception exception = null) => PluginLog.Error(exception, message);

    public static void LogFatal(string message, Exception exception = null) => PluginLog.Fatal(exception, message);

    public static void Initialize(IDalamudPluginInterface pluginInterface) => _ = new DalamudApi(pluginInterface);

    public static void Dispose() => SigScanner?.Dispose();
}