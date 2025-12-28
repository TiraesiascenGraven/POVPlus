using System.Runtime.InteropServices;

namespace Hypostasis.Game.Structures;

[StructLayout(LayoutKind.Explicit), GameStructure("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 33 F6 48 C7 41 ?? ?? ?? ?? ?? 48 8D 05")]
public unsafe partial struct CameraManager : IHypostasisStructure
{
    [FieldOffset(0x0)] public FFXIVClientStructs.FFXIV.Client.Game.Control.CameraManager CS;
    [FieldOffset(0x0)] public GameCamera* worldCamera;
    [FieldOffset(0x8)] public GameCamera* idleCamera;
    [FieldOffset(0x10)] public GameCamera* menuCamera;
    [FieldOffset(0x18)] public GameCamera* spectatorCamera;

    public bool Validate() => true;
}