using System;
using System.Runtime.InteropServices;
using Dalamud.Hooking;

namespace Hypostasis.Game;

[HypostasisDebuggable]
public class GameFunction<T> : IGameFunction where T : Delegate
{
    public string Signature { get; protected set; }
    public nint Address => address ?? SetupAddress(false);
    public T Invoke => Address != nint.Zero ? del ?? SetupDelegate() : null;
    public Hook<T> Hook { get; private set; }
    public T Original => Hook?.Original ?? Invoke;
    public bool IsValid => Invoke != null;
    public bool IsHooked => Hook != null;

    private nint? address;
    private T del;

    public GameFunction() { }

    public GameFunction(string sig, bool required = false)
    {
        Signature = sig;
        if (required)
            SetupAddress(true);
    }

    protected nint SetupAddress(bool required)
    {
        try
        {
            address = ScanAddress();
        }
        catch (Exception e)
        {
            address = nint.Zero;
            DalamudApi.LogWarning($"Failed to find signature {Signature}", e);
            if (required)
                throw;
        }

        return address.Value;
    }

    protected virtual nint ScanAddress() => DalamudApi.SigScanner.DalamudSigScanner.ScanText(Signature);

    private T SetupDelegate()
    {
        if (address is not > 0) return null;
        return del = Marshal.GetDelegateForFunctionPointer<T>(address.Value);
    }

    public void CreateHook(T detour, bool enable = true, bool dispose = true)
    {
        if (Address == nint.Zero) return;

        if (IsHooked)
            throw new ApplicationException("Attempted to hook function more than once");
        Hook = DalamudApi.GameInteropProvider.HookFromAddress(Address, detour);
        DalamudApi.SigScanner.AddHook(Hook, enable, dispose);
        //DalamudApi.SigScanner.AddMember(GetType(), this, nameof(Hook));
    }
}