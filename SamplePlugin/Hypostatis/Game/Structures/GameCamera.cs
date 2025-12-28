using System;
using System.Numerics;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.System.Framework;

namespace Hypostasis.Game.Structures;

[StructLayout(LayoutKind.Explicit), GameStructure("40 53 48 83 EC 20 48 8D 05 ?? ?? ?? ?? 48 8B D9 48 89 01 48 83 C1 10 E8 ?? ?? ?? ?? 0F B6 83 ?? ?? ?? ?? 33 C9 24 FD")]
public unsafe partial struct GameCamera : IHypostasisStructure
{
    [FieldOffset(0x0)] public nint* vtbl;
    [FieldOffset(0x60)] public float x;
    [FieldOffset(0x64)] public float y;
    [FieldOffset(0x68)] public float z;
    [FieldOffset(0x90)] public float lookAtX; // Position that the camera is focused on (Actual position when zoom is 0)
    [FieldOffset(0x94)] public float lookAtY;
    [FieldOffset(0x98)] public float lookAtZ;
    [FieldOffset(0x124)] public float currentZoom; // 6
    [FieldOffset(0x128)] public float minZoom; // 1.5
    [FieldOffset(0x12C)] public float maxZoom; // 20
    [FieldOffset(0x130)] public float currentFoV; // 0.78
    [FieldOffset(0x134)] public float minFoV; // 0.69
    [FieldOffset(0x138)] public float maxFoV; // 0.78
    [FieldOffset(0x13C)] public float addedFoV; // 0
    [FieldOffset(0x140)] public float currentHRotation; // -pi -> pi, default is pi
    [FieldOffset(0x144)] public float currentVRotation; // -0.349066
    [FieldOffset(0x148)] public float hRotationDelta;
    [FieldOffset(0x158)] public float minVRotation; // -1.483530, should be -+pi/2 for straight down/up but camera breaks so use -+1.569
    [FieldOffset(0x15C)] public float maxVRotation; // 0.785398 (pi/4)
    [FieldOffset(0x170)] public float tilt;
    [FieldOffset(0x180)] public int mode; // Camera mode? (0 = 1st person, 1 = 3rd person, 2+ = weird controller mode? cant look up/down)
    [FieldOffset(0x184)] public int controlType; // 0 first person, 1 legacy, 2 standard, 4 talking to npc in first person (with option enabled), 5 talking to npc (with option enabled), 3/6 ???
    [FieldOffset(0x18C)] public float interpolatedZoom;
    [FieldOffset(0x1A0)] public float transition; // Seems to be related to the 1st <-> 3rd camera transition
    [FieldOffset(0x1C0)] public float viewX;
    [FieldOffset(0x1C4)] public float viewY;
    [FieldOffset(0x1C8)] public float viewZ;
    [FieldOffset(0x1F4)] public byte isFlipped; // 1 while holding the keybind
    [FieldOffset(0x22C)] public float interpolatedY;
    [FieldOffset(0x234)] public float lookAtHeightOffset; // No idea what to call this (0x230 is the interpolated value)
    [FieldOffset(0x238)] public byte resetLookatHeightOffset; // No idea what to call this
    [FieldOffset(0x240)] public float interpolatedLookAtHeightOffset;
    [FieldOffset(0x2C0)] public byte lockPosition;
    [FieldOffset(0x2D4)] public float lookAtY2;

    public bool IsHRotationOffset => mode == isFlipped;
    public float GameObjectHRotation => !IsHRotationOffset ? (currentHRotation > 0 ? currentHRotation - MathF.PI : currentHRotation + MathF.PI) : currentHRotation;

    public class GameCameraVTable(nint* v) : VirtualTable(v)
    {
        public delegate void SetCameraLookAtDelegate(GameCamera* camera, Vector3* lookAtPosition, Vector3* cameraPosition, Vector3* a4);
        public readonly VirtualFunction<SetCameraLookAtDelegate> setCameraLookAt = new(v, 14, "40 53 48 83 EC 30 44 8B 89 ?? ?? ?? ?? 48 8B DA");

        public delegate void GetCameraPositionDelegate(GameCamera* camera, GameObject* target, Vector3* position, Bool swapPerson);
        public readonly VirtualFunction<GetCameraPositionDelegate> getCameraPosition = new(v, 15);

        public delegate GameObject* GetCameraTargetDelegate(GameCamera* camera);
        public readonly VirtualFunction<GetCameraTargetDelegate> getCameraTarget = new(v, 17);

        public delegate Bool CanChangePerspectiveDelegate();
        public readonly VirtualFunction<CanChangePerspectiveDelegate> canChangePerspective = new(v, 22);

        public delegate float GetZoomDeltaDelegate();
        public readonly VirtualFunction<GetZoomDeltaDelegate> getZoomDelta = new(v, 28, "F3 0F 10 05 ?? ?? ?? ?? C3"); // This sig is meant to match multiple things
    }

    private static GameCameraVTable vtable;
    public GameCameraVTable VTable => vtable ??= new(vtbl);

    public void SetCameraLookAt(Vector3* lookAtPosition, Vector3* cameraPosition, Vector3* a4)
    {
        fixed (GameCamera* ptr = &this)
            VTable.setCameraLookAt.Invoke(ptr, lookAtPosition, cameraPosition, a4);
    }

    public void GetCameraPosition(GameObject* target, Vector3* position, bool swapPerson)
    {
        fixed (GameCamera* ptr = &this)
            VTable.getCameraPosition.Invoke(ptr, target, position, swapPerson);
    }

    public GameObject* GetCameraTarget()
    {
        fixed (GameCamera* ptr = &this)
            return VTable.getCameraTarget.Invoke(ptr);
    }

    public Bool CanChangePerspective() => VTable.canChangePerspective.Invoke();

    public float GetZoomDelta() => VTable.getZoomDelta.Invoke();

    public delegate byte GetCameraAutoRotateModeDelegate(GameCamera* camera, Framework* framework);
    public static readonly GameFunction<GetCameraAutoRotateModeDelegate> getCameraAutoRotateMode = new("E8 ?? ?? ?? ?? 48 8B CB 85 C0 0F 84 ?? ?? ?? ?? 83 E8 01");
    public byte GetCameraAutoRotateMode()
    {
        fixed (GameCamera* ptr = &this)
            return getCameraAutoRotateMode.Invoke(ptr, Framework.Instance());
    }

    public delegate float GetCameraMaxMaintainDistanceDelegate(GameCamera* camera);
    public static readonly GameFunction<GetCameraMaxMaintainDistanceDelegate> getCameraMaxMaintainDistance = new("E8 ?? ?? ?? ?? F3 0F 5D 44 24 58");
    public float GetCameraMaxMaintainDistance()
    {
        fixed (GameCamera* ptr = &this)
            return getCameraMaxMaintainDistance.Invoke(ptr);
    }

    public delegate Bool UpdateLookAtHeightOffsetDelegate(GameCamera* camera, GameObject* o, Bool zero);
    public static readonly GameFunction<UpdateLookAtHeightOffsetDelegate> updateLookAtHeightOffset = new("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 40 48 8B 02 48 8B F1 48 8B CA"); // INLINED... unable to hook
    public bool UpdateLookAtHeightOffset(GameObject* o, bool zero)
    {
        fixed (GameCamera* ptr = &this)
            return updateLookAtHeightOffset.Invoke(ptr, o, zero);
    }

    public delegate Bool ShouldDisplayObjectDelegate(GameCamera* camera, GameObject* o, Vector3* cameraPosition, Vector3* cameraLookAt);
    public static readonly GameFunction<ShouldDisplayObjectDelegate> shouldDisplayObject = new("E8 ?? ?? ?? ?? 84 C0 75 18 48 8D 0D ?? ?? ?? ?? B3 01");
    public bool ShouldDisplayObject(GameObject* o)
    {
        fixed (GameCamera* ptr = &this)
            return shouldDisplayObject.Invoke(ptr, o, (Vector3*)&ptr->x, (Vector3*)&ptr->lookAtX);
    }

    public bool Validate() => true;
}

/*
public unsafe class VirtualTable
{
    public delegate*<nint, byte, void> vf0; // Dispose
    public delegate*<nint, void> vf1; // Init
    public delegate*<nint, void> vf2; // ??? (new in endwalker)
    public delegate*<nint, void> vf3; // Update
    public delegate*<nint, nint> vf4; // ??? crashes (calls scene camera vf1)
    public delegate*<nint, void> vf5; // reset camera angle
    public delegate*<nint, nint> vf6; // ??? gets something (might need a float array)
    public delegate*<nint, nint> vf7; // ??? get position / rotation? (might need a float array)
    public delegate*<nint, void> vf8; // duplicate of 4
    public delegate*<nint, void> vf9; // ??? (runs whenever the camera is swapped to)
    public delegate*<void> vf10; // empty function (for the world camera anyway) (runs whenever the camera is swapped from)
    public delegate*<void> vf11; // empty function
    public delegate*<nint, nint, bool> vf12; // ??? looks like it returns a bool? (runs whenever the camera gets too close to the character) (compares vf16 return to 2nd argument)
    public delegate*<nint, byte> vf13; // ??? looks like it does something with inputs (returns 0/1 depending on some input)
    public delegate*<void, nint, nint, nint, nint> vf14; // applies center height offset
    public delegate*<nint, nint, nint, byte, void> vf15; // set position (requires 4 arguments, might need a float array)
    public delegate*<nint, byte> vf16; // get control type? returns 1 for legacy, 2 for standard (this value is applied to 0x174)
    public delegate*<nint, nint> vf17; // get camera target
    public delegate*<nint, nint, float> vf18; // ??? crashes
    public delegate*<nint, nint, void> vf19; // ??? requires 2 arguments (might need a float array)
    public delegate*<nint, nint> vf20; // ??? looks like it does something with targeting
    public delegate*<nint, byte, int> vf21; // ??? requires 2 arguments
    public delegate*<nint, bool> vf22; // can change perspective (1st <-> 3rd) (SOMETHING CHANGED IN ENDWALKER)
    public delegate*<nint, void> vf23; // ??? causes a "camera position set" toast with no obvious effect (switch statement with vf15 return)
    public delegate*<nint, void> vf24; // loads the camera angle from 22 (switch statement with vf15 return)
    public delegate*<nint, void> vf25; // causes a "camera position restored to default" toast and causes an effect similar to 1, but doesnt change horizontal angle to default (switch statement with vf15 return)
    public delegate*<nint, float, float> vf26; // ??? places the camera really high above character
    public delegate*<float> vf27; // get max distance? doesnt seem to return anything except 20 ever though
    public delegate*<float> vf28; // get scroll amount (0.75)
    public delegate*<float> vf29; // get ??? (1)
    public delegate*<float> vf30; // get ??? (0.5 or 1) (uses actionmanager/g_layoutworld and uimodule vf87?)
    public delegate*<float> vf31; // duplicate of 28
    public delegate*<float> vf32; // duplicate of 28

    public VirtualTable(nint* address)
    {
        foreach (var f in GetType().GetFields())
        {
            var i = ushort.Parse(f.Name[2..]);
            var vfunc = *(address + i);
            f.SetValue(this, f.FieldType.Cast(vfunc));
        }
    }
}*/