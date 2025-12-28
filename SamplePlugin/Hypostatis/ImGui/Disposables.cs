using System;
using System.Numerics;

namespace Dalamud.Bindings.ImGui;

public static partial class ImGuiEx
{
    public sealed class IDBlock : IDisposable
    {
        private static readonly IDBlock instance = new();
        private IDBlock() { }

        public static IDBlock Begin(int id)
        {
            ImGui.PushID(id);
            return instance;
        }

        public static IDBlock Begin(uint id) => Begin((int)id);

        public static IDBlock Begin(nint id)
        {
            ImGui.PushID(id);
            return instance;
        }

        public static IDBlock Begin(nuint id) => Begin((nint)id);

        public static IDBlock Begin(string id)
        {
            ImGui.PushID(id);
            return instance;
        }

        public static unsafe IDBlock Begin(void* ptr)
        {
            ImGui.PushID(ptr);
            return instance;
        }

        public void Dispose() => ImGui.PopID();
    }

    public sealed class StyleVarBlock : IDisposable
    {
        private static readonly StyleVarBlock instance = new();
        private StyleVarBlock() { }

        public static StyleVarBlock Begin(ImGuiStyleVar idx, float val, bool conditional = true)
        {
            if (!conditional) return null;
            ImGui.PushStyleVar(idx, val);
            return instance;
        }

        public static StyleVarBlock Begin(ImGuiStyleVar idx, Vector2 val, bool conditional = true)
        {
            if (!conditional) return null;
            ImGui.PushStyleVar(idx, val);
            return instance;
        }

        public void Dispose() => ImGui.PopStyleVar();
    }

    public sealed class StyleColorBlock : IDisposable
    {
        private static readonly StyleColorBlock instance = new();
        private StyleColorBlock() { }

        public static StyleColorBlock Begin(ImGuiCol idx, uint val, bool conditional = true)
        {
            if (!conditional) return null;
            ImGui.PushStyleColor(idx, val);
            return instance;
        }

        public static StyleColorBlock Begin(ImGuiCol idx, Vector4 val, bool conditional = true)
        {
            if (!conditional) return null;
            ImGui.PushStyleColor(idx, val);
            return instance;
        }

        public void Dispose() => ImGui.PopStyleColor();
    }

    public sealed class IndentBlock : IDisposable
    {
        private static readonly IndentBlock instance = new();
        private IndentBlock() { }

        public static IndentBlock Begin()
        {
            PushIndent();
            return instance;
        }

        public static IndentBlock Begin(float indent)
        {
            if (indent == 0) return null;
            PushIndent(indent);
            return instance;
        }

        public void Dispose() => PopIndent();
    }

    public sealed class FontBlock : IDisposable
    {
        private static readonly FontBlock instance = new();
        private FontBlock() { }

        public static FontBlock Begin(ImFontPtr font)
        {
            ImGui.PushFont(font);
            return instance;
        }

        public void Dispose() => ImGui.PopFont();
    }

    public sealed class GroupBlock : IDisposable
    {
        private static readonly GroupBlock instance = new();
        private GroupBlock() { }

        public static GroupBlock Begin()
        {
            ImGui.BeginGroup();
            return instance;
        }

        public void Dispose() => ImGui.EndGroup();
    }

    public sealed class ClipRectBlock : IDisposable
    {
        private static readonly ClipRectBlock instance = new();
        private ClipRectBlock() { }

        public static ClipRectBlock Begin(Vector2 min, Vector2 max, bool overlap = true)
        {
            ImGui.PushClipRect(min, max, overlap);
            return instance;
        }

        public void Dispose() => ImGui.PopClipRect();
    }

    public sealed class TooltipBlock : IDisposable
    {
        private static readonly TooltipBlock instance = new();
        private TooltipBlock() { }

        public static TooltipBlock Begin()
        {
            ImGui.BeginTooltip();
            return instance;
        }

        public void Dispose() => ImGui.EndTooltip();
    }

    public sealed class DisabledBlock : IDisposable
    {
        private static readonly DisabledBlock instance = new();
        private DisabledBlock() { }

        public static DisabledBlock Begin(bool conditional = true)
        {
            ImGui.BeginDisabled(conditional);
            return instance;
        }

        public void Dispose() => ImGui.EndDisabled();
    }

    public sealed class AllowKeyboardFocusBlock : IDisposable
    {
        private static readonly AllowKeyboardFocusBlock instance = new();
        private AllowKeyboardFocusBlock() { }

        public static AllowKeyboardFocusBlock Begin(bool allow = false)
        {
            ImGui.PushAllowKeyboardFocus(allow);
            return instance;
        }

        public void Dispose() => ImGui.PopAllowKeyboardFocus();
    }

    public sealed class ButtonRepeatBlock : IDisposable
    {
        private static readonly ButtonRepeatBlock instance = new();
        private ButtonRepeatBlock() { }

        public static ButtonRepeatBlock Begin(bool repeat = true)
        {
            ImGui.PushButtonRepeat(repeat);
            return instance;
        }

        public void Dispose() => ImGui.PopButtonRepeat();
    }

    public sealed class ItemWidthBlock : IDisposable
    {
        private static readonly ItemWidthBlock instance = new();
        private ItemWidthBlock() { }

        public static ItemWidthBlock Begin(float width)
        {
            ImGui.PushItemWidth(width);
            return instance;
        }

        public void Dispose() => ImGui.PopItemWidth();
    }

    public sealed class TextWrapPosBlock : IDisposable
    {
        private static readonly TextWrapPosBlock instance = new();
        private TextWrapPosBlock() { }

        public static TextWrapPosBlock Begin()
        {
            ImGui.PushTextWrapPos();
            return instance;
        }

        public static TextWrapPosBlock Begin(float posX)
        {
            ImGui.PushTextWrapPos(posX);
            return instance;
        }

        public void Dispose() => ImGui.PopTextWrapPos();
    }
}