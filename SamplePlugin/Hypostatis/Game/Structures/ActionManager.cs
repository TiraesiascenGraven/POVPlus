using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace Hypostasis.Game.Structures;

[StructLayout(LayoutKind.Explicit), GameStructure("45 33 C0 B8 ?? ?? ?? ?? 48 89 41 38")]
public unsafe partial struct ActionManager : IHypostasisStructure
{
    [FieldOffset(0x0)] public FFXIVClientStructs.FFXIV.Client.Game.ActionManager CS;
    [FieldOffset(0x8)] public float animationLock;
    [FieldOffset(0x28)] public bool isCasting;
    [FieldOffset(0x28)] public uint castActionType;
    [FieldOffset(0x2C)] public uint castActionID;
    [FieldOffset(0x30)] public float elapsedCastTime;
    [FieldOffset(0x34)] public float castTime;
    [FieldOffset(0x38)] public ulong castTargetObjectID;
    [FieldOffset(0x60)] public float remainingComboTime;
    [FieldOffset(0x68)] public bool isQueued;
    [FieldOffset(0x6C)] public uint queuedActionType;
    [FieldOffset(0x70)] public uint queuedActionID;
    [FieldOffset(0x78)] public ulong queuedTargetObjectID;
    [FieldOffset(0xA0)] public ulong queuedGroundTargetObjectID;
    [FieldOffset(0xC0)] public byte activateGroundTarget;
    [FieldOffset(0x120)] public ushort currentSequence;
    //[FieldOffset(0x122)] public ushort unknownSequence; // ???
    [FieldOffset(0x5F8)] public bool isGCDRecastActive;
    [FieldOffset(0x5FC)] public uint currentGCDAction;
    [FieldOffset(0x600)] public float elapsedGCDRecastTime;
    [FieldOffset(0x604)] public float gcdRecastTime;

    public static uint GCDRecast => Math.Min(GetAdjustedRecastTime(1, 9, true), GetAdjustedRecastTime(1, 14, true));

    public delegate uint GetSpellIDForActionDelegate(uint actionType, uint actionID);
    public static readonly GameFunction<GetSpellIDForActionDelegate> getSpellIDForAction = new("E8 ?? ?? ?? ?? 83 FD 02 75 2D");
    public static uint GetSpellIDForAction(uint actionType, uint actionID) => getSpellIDForAction.Invoke(actionType, actionID);

    public delegate Bool CanUseActionOnGameObjectDelegate(uint actionID, GameObject* o);
    public static readonly GameFunction<CanUseActionOnGameObjectDelegate> canUseActionOnGameObject = new("48 89 5C 24 08 57 48 83 EC 20 48 8B DA 8B F9 E8 ?? ?? ?? ?? 4C 8B C3");
    public static bool CanUseActionOnGameObject(uint actionID, GameObject* o) =>
        canUseActionOnGameObject.Invoke(actionID, o) || DalamudApi.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>().GetRowOrDefault(actionID) is { TargetArea: true };

    public delegate uint GetAdjustedRecastTimeDelegate(uint actionType, uint actionID, Bool useStats);
    public static readonly GameFunction<GetAdjustedRecastTimeDelegate> getAdjustedRecastTime = new("E8 ?? ?? ?? ?? 85 C0 7E 1C");
    public static uint GetAdjustedRecastTime(uint actionType, uint actionID, bool useStats) => getAdjustedRecastTime.Invoke(actionType, actionID, useStats);

    public delegate Bool CanQueueActionDelegate(ActionManager* actionManager, uint actionType, uint actionID);
    public static readonly GameFunction<CanQueueActionDelegate> canQueueAction = new ("E8 ?? ?? ?? ?? 3C 01 0F 85 ?? ?? ?? ?? 88 45 68");
    public bool CanQueueAction(uint actionType, uint actionID)
    {
        fixed (ActionManager* ptr = &this)
            return canQueueAction.Invoke(ptr, actionType, actionID);
    }

    public bool Validate() => true;
}