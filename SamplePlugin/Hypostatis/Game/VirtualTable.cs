namespace Hypostasis.Game;

public abstract unsafe class VirtualTable(nint* v)
{
    protected readonly nint* vtbl = v;
    public nint this[int i] => vtbl[i];
}