using Dalamud.Configuration;
using Hypostasis.Game.Structures;
using System;
using System.Diagnostics;
using System.Numerics;


namespace SamplePlugin;


///THIS SHOULD BE TREATED AS PLAYER CONFIGURATION SETTINGS - NOT A PLACE FOR ALL MY RANDOM VARIABLES

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool IsConfigWindowMovable { get; set; } = true;
    public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;

    public float MinFovValue = 1;
    public bool TestVar1 { get; set; } = true;

    public  float OffsetX = 0;
    public  float OffsetY = 0;
    public  float OffsetZ = 0;

    public  int BoneToBind = 1;

    //FOV
    public float Setting_FOV = 0.75f;

    //Control Params
    public  bool Setting_ModEnabled = true;
    public  bool Setting_FirstPersonOnly = true;
    public  bool Setting_ShowBody = true;
    public  bool Setting_RotateWithplayer = false;

    public  bool RotationBindBoolZ = false;
    public  bool RotationBindBoolX = false;



    // The below exist just to make saving less cumbersome
    public void Save()
    {
        //Service.Log.Information($"SAVE CALLED");
        Service.PluginInterface.SavePluginConfig(this);
    }
}
