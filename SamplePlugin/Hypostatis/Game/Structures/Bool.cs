using System;
using System.Runtime.InteropServices;

namespace Hypostasis.Game.Structures;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public readonly struct Bool : IComparable<Bool>, IEquatable<Bool>
{
    [MarshalAs(UnmanagedType.U1)]
    private readonly bool b;
    public Bool(byte b2) => b = b2 != 0;
    public Bool(bool b2) => b = b2;
    public static bool operator ==(Bool l, Bool r) => l.b == r.b;
    public static bool operator !=(Bool l, Bool r) => l.b != r.b;
    public static implicit operator bool(Bool b) => b.b;
    public static implicit operator Bool(bool b) => new(b);
    public static implicit operator byte(Bool b) => (byte)(b.b ? 1 : 0);
    public static implicit operator Bool(byte b) => new(b);
    public bool Equals(Bool b2) => b.Equals(b2.b);
    public override bool Equals(object o) => o is Bool b2 && Equals(b2);
    public int CompareTo(Bool b2) => b.CompareTo(b2.b);
    public override int GetHashCode() => b.GetHashCode();
    public override string ToString() => b.ToString();
}