using Dalamud.Configuration;
using Hypostasis.Game.Structures;
using System;
using System.Diagnostics;
using System.Numerics;


namespace SamplePlugin
{
    internal static class GlobalVars
    {
        //public int Version { get; set; } = 0;

        //public bool IsConfigWindowMovable { get; set; } = true;
        //public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;

        //public float MinFovValue = 1;
        //public bool TestVar1 { get; set; } = true;

        //public static float OffsetX = 0;
        //public static float OffsetY = 0;
        //public static float OffsetZ = 0;

        //public static int BoneToBind = 1;

        public static float InitOnlyOncePls = 0;

        //Core Stuff I Think?
        public static GameCamera PlayersCamera;

        //Control Params
        //public static bool Setting_ModEnabled = true;
        //public static bool Setting_FirstPersonOnly = true;
        //public static bool Setting_ShowBody = true;
        //public static bool Setting_RotateWithplayer = false;


        //Camera Offset Params
        public static float RotationBindBoneX = 1;
        public static int RotationBoneValueX = 46;
        public static float RotationBindOffsetX = -0.96f;
        public static string RotationBoneValueXName = "ERROR";


        public static float RotationBindBoneZ = 1;
        public static int RotationBoneValueZ = 46;
        public static float RotationBindOffsetZ = -0.670f;
        public static string RotationBoneValueZName = "ERROR";

        public static int PlayerBoneCount = 1;

        //Bone Offset params
        public static float CameraXRelative = 0;
        public static float CameraXRelativePrev = 0;

        public static float CameraZRelative = 0;
        public static float CameraZRelativePrev = 0;

        public static Quaternion CameraQuartCurrent;
        public static Quaternion CameraQuartPrev;
        public static Quaternion CameraQuartNormalize;

        //Character Rotation Params
        public static float PlayerXRotationCurrent;
        public static float PlayerXRotationPrev;
        public static float PlayerXRotationNormalize;

        //Bone Rotation Params
        public static float BoneYawRotateCurrent = 0;
        public static float BoneYawRotateBefore = 0;

        public static float BonePitchRotateCurrent = 0;
        public static float BonePitchRotateBefore = 0;

        public static float TotalRotationOffsetAddition = 0;

        //Final Yaw Calculation
        public static float TotalYawCurrent;
        public static float TotalYawPrev;

        //Movement Detection
        public static Vector3 LastPlayerPosition;
        public static Vector3 CurrentPlayerPosition;
        public static bool RightClickMoving;
        public static bool StationaryRotateRight;

        public static float TemporalYawPrevious = 10;


        public static float TOTALYAW = 0;

        public static float PreviousRotationBeforeChanges = 0;

        //All Camera Values BEFORE this plugin is messed with
        public static float PreviousMaxVRotation;
        public static float PreviousMinVRotation;
        public static float PreviousCurrentFoV;
        public static float PreviousTilt;
        public static float PreviousMinFOV;

        //Conditional Body Hiding
        public static bool HideOwnBody = true;


    }
}
