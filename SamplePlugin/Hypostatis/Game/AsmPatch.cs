using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Hypostasis.Game;

[HypostasisDebuggable]
public sealed class AsmPatch : IDisposable
{
    public nint Address { get; }
    public string Signature { get; } = string.Empty;
    public byte[] NewBytes { get; }
    public byte[] OldBytes { get; }
    public bool IsEnabled { get; private set; }
    public bool IsValid => Address != nint.Zero;
    public string ReadBytes => !IsValid ? string.Empty : OldBytes.Aggregate(string.Empty, (current, b) => current + b.ToString("X2") + " ");
    private static readonly List<AsmPatch> asmPatches = [];

    public AsmPatch(nint address, IReadOnlyCollection<byte?> bytes, bool startEnabled = false)
    {
        if (address == nint.Zero) return;

        var trimmedBytes = bytes.SkipWhile(b => !b.HasValue).ToArray();
        var skip = bytes.Count - trimmedBytes.Length;
        address += skip;
        Address = address;
        SafeMemory.ReadBytes(address, trimmedBytes.Length, out var oldBytes);
        OldBytes = oldBytes;
        NewBytes = Enumerable.Range(0, trimmedBytes.Length).Select(i => trimmedBytes[i] ?? oldBytes[i]).ToArray();
        asmPatches.Add(this);

        if (startEnabled)
            Enable();
    }

    public AsmPatch(nint address, string bytesString, bool startEnabled = false) : this(address, ParseByteString(bytesString), startEnabled) { }

    public AsmPatch(string sig, IReadOnlyCollection<byte?> bytes, bool startEnabled = false) : this(Scan(sig), bytes, startEnabled) => Signature = sig;

    public AsmPatch(string sig, string bytesString, bool startEnabled = false) : this(sig, ParseByteString(bytesString), startEnabled) { }

    private static nint Scan(string sig)
    {
        try
        {
            return DalamudApi.SigScanner.DalamudSigScanner.ScanModule(sig);
        }
        catch (Exception e)
        {
            DalamudApi.LogWarning($"Failed to find signature {sig}", e);
            return nint.Zero;
        }
    }

    private static byte?[] ParseByteString(string bytesString)
    {
        bytesString = bytesString.Replace(" ", string.Empty);

        var bytes = new byte?[bytesString.Length / 2];
        for (int i = 0; i < bytesString.Length; i += 2)
        {
            var s = bytesString.Substring(i, 2);
            bytes[i / 2] = s is not ("??" or "**") ? byte.Parse(s, NumberStyles.AllowHexSpecifier) : null;
        }
        return bytes;
    }

    public void Enable()
    {
        if (IsEnabled || !IsValid) return;
        SafeMemory.WriteBytes(Address, NewBytes);
        IsEnabled = true;
    }

    public void Disable()
    {
        if (!IsEnabled || !IsValid) return;
        SafeMemory.WriteBytes(Address, OldBytes);
        IsEnabled = false;
    }

    public void Toggle()
    {
        if (!IsEnabled)
            Enable();
        else
            Disable();
    }

    public void Toggle(bool enable)
    {
        if (enable)
            Enable();
        else
            Disable();
    }

    public void Dispose()
    {
        if (IsEnabled)
            Disable();
    }

    public static void DisposeAll()
    {
        foreach (var patch in asmPatches)
            patch?.Dispose();
    }
}