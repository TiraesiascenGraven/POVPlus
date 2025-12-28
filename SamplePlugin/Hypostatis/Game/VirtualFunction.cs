using System;

namespace Hypostasis.Game;

public sealed unsafe class VirtualFunction<T> : GameFunction<T> where T : Delegate
{
    public nint* VTable { get; }
    public int VFuncIndex { get; }

    public VirtualFunction(nint* vtbl, int i, string sig = null)
    {
        VTable = vtbl;
        VFuncIndex = i;
        Signature = sig;
        SetupAddress(true);
    }

    protected override nint ScanAddress() => VTable != null
        ? string.IsNullOrEmpty(Signature)
            ? VTable[VFuncIndex]
            : DalamudApi.SigScanner.Scan(VTable[VFuncIndex], Signature.Length / 2, Signature)
        : nint.Zero;
}