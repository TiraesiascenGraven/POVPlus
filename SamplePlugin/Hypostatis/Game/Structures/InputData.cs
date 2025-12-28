using System.Runtime.InteropServices;

namespace Hypostasis.Game.Structures;

[StructLayout(LayoutKind.Explicit, Size = 0xA20), GameStructure("48 89 5C 24 08 48 89 74 24 10 57 48 83 EC 20 48 8D 05 ?? ?? ?? ?? 48 63 DA")]
public unsafe partial struct InputData : IHypostasisStructure
{
    //[FieldOffset(0x0)] public void CS;
    [FieldOffset(0x9B4)] public int inputIDCount;

    public delegate Bool InputIDDelegate(InputData* inputData, uint id);
    public static readonly GameFunction<InputIDDelegate> isInputIDHeld = new("E9 ?? ?? ?? ?? B9 4F 01 00 00");
    public bool IsInputIDHeld(uint id)
    {
        fixed (InputData* ptr = &this)
            return isInputIDHeld.Invoke(ptr, id);
    }

    public static readonly GameFunction<InputIDDelegate> isInputIDPressed = new("E9 ?? ?? ?? ?? 83 7F 44 02");
    public bool IsInputIDPressed(uint id)
    {
        fixed (InputData* ptr = &this)
            return isInputIDPressed.Invoke(ptr, id);
    }

    public static readonly GameFunction<InputIDDelegate> isInputIDLongPressed = new("E8 ?? ?? ?? ?? 84 C0 74 37 EB 06");
    public bool IsInputIDLongPressed(uint id)
    {
        fixed (InputData* ptr = &this)
            return isInputIDLongPressed.Invoke(ptr, id);
    }

    public static readonly GameFunction<InputIDDelegate> isInputIDReleased = new("E8 ?? ?? ?? ?? 88 43 0F");
    public bool IsInputIDReleased(uint id)
    {
        fixed (InputData* ptr = &this)
            return isInputIDReleased.Invoke(ptr, id);
    }

    public delegate int GetAxisInputDelegate(InputData* inputData, uint id);
    public static readonly GameFunction<GetAxisInputDelegate> getAxisInput = new("E8 ?? ?? ?? ?? 66 44 0F 6E C3");
    public int GetAxisInput(uint id)
    {
        fixed (InputData* ptr = &this)
            return getAxisInput.Invoke(ptr, id);
    }

    public float GetAxisInputFloat(uint id) => GetAxisInput(id) / 100f;

    public delegate sbyte GetMouseWheelStatusDelegate();
    public static readonly GameFunction<GetMouseWheelStatusDelegate> getMouseWheelStatus = new("E8 ?? ?? ?? ?? F7 D8 48 8B CB");
    public static sbyte GetMouseWheelStatus() => getMouseWheelStatus.Invoke();

    public delegate void* GetInputBindingDelegate(InputData* inputData, uint id);
    public static readonly GameFunction<GetInputBindingDelegate> getInputBinding = new("48 63 C2 48 6B C0 0B");
    public void* GetInputBinding(uint id)
    {
        fixed (InputData* ptr = &this)
            return getInputBinding.Invoke(ptr, id);
    }

    public bool Validate() => true;
}