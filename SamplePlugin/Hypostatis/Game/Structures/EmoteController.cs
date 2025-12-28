using System.Runtime.InteropServices;

namespace Hypostasis.Game.Structures;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct EmoteController : IHypostasisStructure
{
    public delegate Bool CancelEmote(EmoteController* emoteController, nint unknown);
    public static readonly GameFunction<CancelEmote> cancelEmote = new("E8 ?? ?? ?? ?? 48 8B 7B 08 45 33 C0");

    public bool Validate() => true;
}