using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using SamplePlugin.Windows;
//using Lumina.Data.Parsing;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using static FFXIVClientStructs.FFXIV.Client.Game.Control.EmoteController;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Common.Math;
using System;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game.Character;


namespace SamplePlugin
{
    public class Plugin : IDalamudPlugin
    {


        private const string CommandName = "/pov";

        public Configuration Configuration { get; init; }

        internal static Plugin P = null!;

        public readonly WindowSystem WindowSystem = new("SamplePlugin");
        private ConfigWindow ConfigWindow { get; init; }
        private MainWindow MainWindow { get; init; }

        private Hook<CameraBase.Delegates.ShouldDrawGameObject>? ExecuteEmoteHook2 { get; init; }




        //public PosingConfiguration Posing { get; set; } = new PosingConfiguration();d







        public unsafe Plugin(IDalamudPluginInterface pluginInterface)
        {
            P = this;
            pluginInterface.Create<Service>();
            pluginInterface.Create<DalamudApi>();
            Service.Framework.RunOnFrameworkThread(Initialize);
            Configuration = Service.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

            // You might normally want to embed resources and load them from the manifest stream
            var goatImagePath = Path.Combine(Service.PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");



            ConfigWindow = new ConfigWindow(this);
            MainWindow = new MainWindow(this, goatImagePath);

            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(MainWindow);

            Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens the Settings"
            });

            // Tell the UI system that we want our windows to be drawn throught he window system
            Service.PluginInterface.UiBuilder.Draw += WindowSystem.Draw;

            // This adds a button to the plugin installer entry of this plugin which allows
            // toggling the display status of the configuration ui
            Service.PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

            // Adds another button doing the same but for the main ui of the plugin
            Service.PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

            //Service.Log.Information($"===A cool log messageee from {Service.PluginInterface.Manifest.Name}===");


            Service.Log.Information($"HOOK : ShouldDrawGameObjectDetour");
            ExecuteEmoteHook2 = Service.GameInteropProvider.HookFromAddress<CameraBase.Delegates.ShouldDrawGameObject>(CameraBase.MemberFunctionPointers.ShouldDrawGameObject, ShouldDrawGameObjectDetour
            );
            ExecuteEmoteHook2.Enable();

            Service.Framework.RunOnFrameworkThread(Initialize);
        }


        public unsafe bool ShouldDrawGameObjectDetour(CameraBase* thisPtr, GameObject* gameObject, Vector3* sceneCameraPos, Vector3* lookAtVector)
        {

            //Player Data?
            //var localPlayer = Service.ClientState.LocalPlayer;

            //var ObjectDebug = gameObject->GetName();


            //if (GlobalVars.HideOwnBody)
            //{
            //    return true;
            //}

            return true;
            

        }


        public void EnableDrawGameObject()
        {
            ExecuteEmoteHook2?.Enable();
        }

        public void DisableDrawGameObject()
        {
            ExecuteEmoteHook2?.Disable();
        }


        public void Dispose()
        {
            // Unregister all actions to not leak anythign during disposal of plugin
            Service.PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
            Service.PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
            Service.PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;

            WindowSystem.RemoveAllWindows();

            ConfigWindow.Dispose();
            MainWindow.Dispose();

            Service.CommandManager.RemoveHandler(CommandName);


            // Do not forget to dispose the hook
            ExecuteEmoteHook2?.Disable();
            ExecuteEmoteHook2?.Dispose();

            HideBody.Dispose();

            Service.Log.Information($"Turning mod off? ");

            //Add turn off logic here



        }

        private void OnCommand(string command, string args)
        {
            // In response to the slash command, toggle the display status of our main ui
            MainWindow.Toggle();
        }

        public void ToggleConfigUi() => ConfigWindow.Toggle();
        public void ToggleMainUi() => MainWindow.Toggle();








        private unsafe void Initialize()
        {
            //Idk why this fires twice and I cba fighting it rn
            if (GlobalVars.InitOnlyOncePls == 0)
            {
                GlobalVars.InitOnlyOncePls = 1;
                //Service.Log.Information($"===INITIALIZED IN PLUGIN===");
                HideBody.Initialize();
            }
        }
    }
}


