namespace Hypostasis.Game;

public interface IGameFunction
{
    public string Signature { get; }
    public nint Address { get; }
    public bool IsValid { get; }
    public bool IsHooked { get; }
}