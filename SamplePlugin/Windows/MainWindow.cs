using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Lumina.Excel.Sheets;
using Serilog;
using static Dalamud.Interface.Utility.Raii.ImRaii;
using static FFXIVClientStructs.FFXIV.Client.UI.RaptureAtkHistory.Delegates;
using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Lumina.Excel.Sheets.Experimental;
using SamplePlugin;
using System.Runtime.InteropServices;



namespace SamplePlugin.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly string goatImagePath;
    private readonly Plugin plugin;

    public static float arrowOffset = 0.75f;


    // We give this window a hidden ID using ##.
    // The user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(Plugin plugin, string goatImagePath)
        : base("POV+ Alpha 0.1.1", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.goatImagePath = goatImagePath;
        this.plugin = plugin;
    }

    private static bool ResetSliderFloat(string id, ref float val, float min, float max, float reset, string format)
    {
        var save = false;


        ///This is the Reset button
        ImGui.PushFont(UiBuilder.IconFont);
        if (ImGui.Button($"{FontAwesomeIcon.UndoAlt.ToIconString()}##{id}"))
        {
            val = reset;
            save = true;
            //Service.Log.Information($"===UI===");
        }
        ImGui.PopFont();

        ///This is the Slider
        ImGui.SameLine();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 160 * ImGuiHelpers.GlobalScale);
        save |= ImGui.SliderFloat(id, ref val, min, max, format);

        return save;
        //    if (CurrentPreset == PresetManager.CurrentPreset)
        //        CurrentPreset.Apply();
    }

    private static void ResetSliderInt(string id, ref float val, float min, float max, float reset, string format)
    {
        var save = true;


        ///This is the Reset button
        ImGui.PushFont(UiBuilder.IconFont);
        if (ImGui.Button($"{FontAwesomeIcon.CalendarMinus.ToIconString()}##{id}"))
        {
            val --;
            save = true;
            Service.Log.Information($"===UI===");
        }
        ImGui.PopFont();

        ///This is the Slider
        ImGui.SameLine();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 160 * ImGuiHelpers.GlobalScale);
        val = (int)val;
        save |= ImGui.SliderFloat(id, ref val, min, max, format);

        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        if (ImGui.Button($"{FontAwesomeIcon.CalendarPlus.ToIconString()}##{id}"))
        {
            val ++;
            save = true;
            Service.Log.Information($"===UI===");
        }
        ImGui.PopFont();

        if (!save) return;
        //Service.Log.Information($"==={val}===");
        //    if (CurrentPreset == PresetManager.CurrentPreset)
        //        CurrentPreset.Apply();

    }

    private static void ControlledSliderInt(string id, ref int val, int min, int max, int reset)
    {
        var save = true;


        if (ImGui.Button("-"))
        {
            if (val != min)
            {
                val -= 1;
            }
        }

        ImGui.SameLine();

        if (ImGui.Button("+"))
        {
            if (val != max)
            {
                val += 1;
            }
        }

        ImGui.SameLine();

        ImGui.SliderInt(id, ref val, min,max );

        

    }



    public void Dispose() { }

    //TEST VARIABLES
    public bool MyTest { get; set; } = false;

    public override void Draw()
    {


        ///Left Box
        ImGui.BeginChild("LeftPanel", new Vector2(250 * ImGuiHelpers.GlobalScale, 0), true);

        ImGui.Text("Profiles coming soon (maybe)");

        ImGui.EndChild();


        //Right Box
        ImGui.SameLine();
        ImGui.BeginChild("RightPanel", Vector2.Zero, true);

        if (ImGui.Checkbox("Mod Enabled?", ref Plugin.P.Configuration.Setting_ModEnabled))
            plugin.Configuration.Save();

        ImGui.Spacing();

        ImGui.TextUnformatted($" While using this mod I reccomend doing the following \n Character Configuration>General>1st Person Camera Auto-adjustment - Set this to Never" +
            $"\n This is not essential but it prevents the overwriting of the camera X and Z rotation that the First Person Auto Adjustment overwrites");
        ImGui.Dummy(new Vector2(0, 20));

        if (ImGui.Checkbox("Plugin Disabled when in Third Person", ref Plugin.P.Configuration.Setting_FirstPersonOnly))
            plugin.Configuration.Save();

        ImGui.Spacing();


        ImGui.TextUnformatted($"Camera Offset:");



        ImGui.Spacing();
        if (ResetSliderFloat("FOV", ref Plugin.P.Configuration.Setting_FOV, 0, 5, 0.75f, "%.2f"))
            plugin.Configuration.Save();


        ImGui.Spacing();
        if (ResetSliderFloat("X Offset (Forwards/Back)", ref Plugin.P.Configuration.OffsetX, -1, 1, 0f, "%1f"))
            plugin.Configuration.Save();


        ImGui.Spacing();
        if (ResetSliderFloat("Y Offset (Up Down)", ref Plugin.P.Configuration.OffsetY, -1, 1, 0f, "%1f"))
            plugin.Configuration.Save();

        ImGui.Spacing();
        if (ResetSliderFloat("Z Offset (Left Right)", ref Plugin.P.Configuration.OffsetZ, -1, 1, 0f, "%1f"))
            plugin.Configuration.Save();


        ImGui.Spacing();
        ImGui.Dummy(new Vector2(0, 20));

        ImGui.TextUnformatted($"EXPERIMENTAL BELOW - Causes rotation issues when moving while holding right mouse button");

        ImGui.Spacing();

        if (ImGui.Checkbox("Bind Camera Rotation X (Left Right) to the Head Bone", ref Plugin.P.Configuration.RotationBindBoolX))
            plugin.Configuration.Save();

        ImGui.Spacing();

        if (ImGui.Checkbox("Bind Camera Rotation Z (Up Down) to the Head Bone - (this also allows you to rotate the camera 360 degrees up and down , just to account for backflips flips etc)", ref Plugin.P.Configuration.RotationBindBoolZ))
            plugin.Configuration.Save(); ;

        ImGui.Spacing();
        ImGui.Dummy(new Vector2(0, 20));

        ImGui.TextUnformatted($"VERY BROKEN BELOW - Fine if you move with just the keyboard");


        if (ImGui.Checkbox("Camera Rotates when player rotates (emulates 1st person camera auto adjustment when moving - but different) VERY GLITCHY WHEN MOVING/ROTATING WITH RIGHT MOUSE CLICK", ref Plugin.P.Configuration.Setting_RotateWithplayer))
            plugin.Configuration.Save();


        ImGui.EndChild();




        //Disable all my godless testing BS
        if (1 == 2)
        {

            ImGui.TextUnformatted($"\nCamera Bone:\n1-POV / 26 - Head");

            ImGui.Spacing();


            ImGui.SliderInt("", ref Plugin.P.Configuration.BoneToBind, 0, 300);


            ImGui.Dummy(new Vector2(0, 20));
            ImGui.Separator();
            ImGui.TextUnformatted($"Camera Rotation Bindings\nJ_te_r - hand right - 65 \nJ_te_l - hand left - 64 \nJ_kao - head - 46 \n");

            //Bind Rotation X
            ImGui.TextUnformatted($"\n Camera X Rotation Bone Binding : ");
            ImGui.Spacing();
            ImGui.Checkbox("Bind Camera Rotation X (Left Right) to Bone", ref Plugin.P.Configuration.RotationBindBoolX);
            ImGui.Spacing();
            var XBoneName = GlobalVars.RotationBoneValueXName;
            ImGui.TextUnformatted($"Bone: {XBoneName}");
            ImGui.Spacing();
            ControlledSliderInt("Bone Ref X", ref GlobalVars.RotationBoneValueX, 0, GlobalVars.PlayerBoneCount, 1);
            ImGui.Spacing();
            ImGui.DragFloat("Bone X Rotation Offset", ref GlobalVars.RotationBindOffsetX, 0.01f, -3.6f, 3.6f, "%.3f");
            ImGui.Spacing();
            ImGui.Spacing();

            //Bind Rotation Z
            ImGui.TextUnformatted($"\n Camera Z Rotation Bone Binding : ");
            ImGui.Spacing();
            ImGui.Checkbox("Bind Camera Rotation Z (Up Down) to Bone", ref Plugin.P.Configuration.RotationBindBoolZ);
            ImGui.Spacing();
            var ZBoneName = GlobalVars.RotationBoneValueZName;
            ImGui.TextUnformatted($"Bone: {ZBoneName}");
            ImGui.SliderInt("Bone Ref Z", ref GlobalVars.RotationBoneValueZ, 0, 300);
            ImGui.Spacing();
            ImGui.DragFloat("Bone Z Rotation Offset", ref GlobalVars.RotationBindOffsetZ, 0.01f, -3.6f, 3.6f, "%.3f");
            ImGui.Spacing();
            ImGui.Spacing();
            //camera->minFoV = 1;
            //camera->maxFoV = 2;


            // Button Test

            ImGui.Spacing();

            // Normally a BeginChild() would have to be followed by an unconditional EndChild(),
            // ImRaii takes care of this after the scope ends.
            // This works for all ImGui functions that require specific handling, examples are BeginTable() or Indent().
        }
    }
}
