using System;
using System.Numerics;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

namespace Dalamud.Bindings.ImGui;

public static partial class ImGuiEx
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetItemTooltip(string s, ImGuiHoveredFlags flags = ImGuiHoveredFlags.None)
    {
        if (ImGui.IsItemHovered(flags))
            ImGui.SetTooltip(s);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsItemDoubleClicked(ImGuiMouseButton button = ImGuiMouseButton.Left, ImGuiHoveredFlags flags = ImGuiHoveredFlags.None) =>
        ImGui.IsMouseDoubleClicked(button) && ImGui.IsItemHovered(flags);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsItemReleased(ImGuiMouseButton button = ImGuiMouseButton.Left, ImGuiHoveredFlags flags = ImGuiHoveredFlags.None) =>
        ImGui.IsMouseReleased(button) && ImGui.IsItemHovered(flags);

    // Why is this not a basic feature of ImGui...
    private static readonly Stack<float> fontScaleStack = new();
    private static float curScale = 1;
    public static void PushFontScale(float scale)
    {
        ImGui.SetWindowFontScale(scale);
        fontScaleStack.Push(curScale);
        curScale = scale;
    }

    public static void PopFontScale()
    {
        curScale = fontScaleStack.Pop();
        ImGui.SetWindowFontScale(curScale);
    }

    public static void PushFontSize(float size) => PushFontScale(size / ImGui.GetFont().FontSize);

    public static void PopFontSize() => PopFontScale();

    public static float GetFontScale() => curScale;

    public static float GetFontSize() => curScale * ImGui.GetFont().FontSize;

    private static readonly Stack<float> indentStack = new();
    public static void PushIndent(float indent = 0f)
    {
        ImGui.Indent(indent);
        indentStack.Push(indent);
    }

    public static void PopIndent() => ImGui.Unindent(indentStack.Pop());

    public static void ClampWindowPosToViewport()
    {
        var viewport = ImGui.GetWindowViewport();
        if (ImGui.IsWindowAppearing() || viewport.ID != ImGuiHelpers.MainViewport.ID) return;

        var pos = viewport.Pos;
        ClampWindowPos(pos, pos + viewport.Size);
    }

    public static void ClampWindowPos(Vector2 max) => ClampWindowPos(Vector2.Zero, max);

    public static void ClampWindowPos(Vector2 min, Vector2 max)
    {
        var pos = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();
        var x = Math.Min(Math.Max(pos.X, min.X), max.X - size.X);
        var y = Math.Min(Math.Max(pos.Y, min.Y), max.Y - size.Y);
        ImGui.SetWindowPos(new Vector2(x, y));
    }

    public static bool IsWindowInMainViewport() => ImGui.GetWindowViewport().ID == ImGuiHelpers.MainViewport.ID;

    public static bool ShouldDrawInViewport() => IsWindowInMainViewport() || Util.IsWindowFocused;

    public static void ShouldDrawInViewport(out bool b) => b = ShouldDrawInViewport();

    // Helper function for displaying / hiding windows outside of the main viewport when the game isn't focused, returns the bool to allow using it in if statements to reduce code
    public static bool SetBoolOnGameFocus(ref bool b)
    {
        if (!b)
            b = Util.IsWindowFocused;
        return b;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetClipboardTextOrDefault(string def = "")
    {
        try { return ImGui.GetClipboardText(); }
        catch { return def; }
    }

    // ?????????
    public static void PushClipRectFullScreen() => ImGui.GetWindowDrawList().PushClipRectFullScreen();

    public static void TextCopyable(string text)
    {
        ImGui.TextUnformatted(text);

        if (!ImGui.IsItemHovered()) return;
        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        if (ImGui.IsItemClicked())
            ImGui.SetClipboardText(text);
    }

    public static void TextCopyable(Vector4 color, string text)
    {
        ImGui.TextColored(color, text);

        if (!ImGui.IsItemHovered()) return;
        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        if (ImGui.IsItemClicked())
            ImGui.SetClipboardText(text);
    }

    public static void TextMarquee(string text, float speed = 0.1f)
    {
        var textWidth = ImGui.CalcTextSize(text).X;
        var scrollWidth = ImGui.GetContentRegionMax().X + textWidth;
        var indent = (float)DalamudApi.PluginInterface.LoadTimeDelta.TotalSeconds * (scrollWidth * speed) % scrollWidth - textWidth;
        ImGui.Indent(indent);
        ImGui.TextUnformatted(text);
        ImGui.Unindent(indent);
    }

    public static void TextMarquee(Vector4 color, string text, float speed = 0.1f)
    {
        var textWidth = ImGui.CalcTextSize(text).X;
        var scrollWidth = ImGui.GetContentRegionMax().X + textWidth;
        var indent = (float)DalamudApi.PluginInterface.LoadTimeDelta.TotalSeconds * (scrollWidth * speed) % scrollWidth - textWidth;
        ImGui.Indent(indent);
        ImGui.TextColored(color, text);
        ImGui.Unindent(indent);
    }

    public static bool FontButton(string label, ImFontPtr font)
    {
        ImGui.PushFont(font);
        var ret = ImGui.Button(label);
        ImGui.PopFont();
        return ret;
    }

    public static bool FontButton(string label, ImFontPtr font, Vector2 size)
    {
        ImGui.PushFont(font);
        var ret = ImGui.Button(label, size);
        ImGui.PopFont();
        return ret;
    }

    public static bool DeleteConfirmationButton(Vector2 size = default)
    {
        using var _ = FontBlock.Begin(UiBuilder.IconFont);
        ImGui.Button(FontAwesomeIcon.Times.ToIconString(), size);
        if (IsItemReleased(ImGuiMouseButton.Right)) return true;

        using var __ = StyleVarBlock.Begin(ImGuiStyleVar.PopupBorderSize, 1);
        if (!ImGui.BeginPopupContextItem(ImU8String.Empty, ImGuiPopupFlags.MouseButtonLeft)) return false;
        var ret = ImGui.Selectable(FontAwesomeIcon.TrashAlt.ToIconString());
        ImGui.EndPopup();
        return ret;
    }

    // No way to block the title bar
    public static void BlockWindowDrag()
    {
        var io = ImGui.GetIO();
        var prev = io.ConfigWindowsMoveFromTitleBarOnly;
        io.ConfigWindowsMoveFromTitleBarOnly = true;
        DalamudApi.Framework.RunOnTick(() => io.ConfigWindowsMoveFromTitleBarOnly = prev);
    }

    private static void AddTextCentered(Vector2 pos, string text, uint color)
    {
        var textSize = ImGui.CalcTextSize(text);
        ImGui.GetWindowDrawList().AddText(pos - textSize / 2, color, text);
    }

    public static void Prefix(string prefix = "◇")
    {
        var dummySize = new Vector2(ImGui.GetFrameHeight());
        ImGui.Dummy(dummySize);
        AddTextCentered(ImGui.GetItemRectMin() + dummySize / 2, prefix, ImGui.GetColorU32(ImGuiCol.Text));
        ImGui.SameLine();
    }

    public static void Prefix(bool isLast) => Prefix(isLast ? "└" : "├");

    public static bool RadioBox(string label, ref int v, string[] optionsArray, bool vertical)
    {
        if (!BeginGroupBox(label, 0)) return false;

        var ret = false;
        var numOptions = optionsArray.Length;
        var maxWidth = 0f;

        ImGui.PushID(label);
        for (int i = 0; i < numOptions; i++)
        {
            var option = optionsArray[i];
            var selected = v == i;
            ret |= ImGui.RadioButton(vertical ? option : $"##{i}", ref v, i) && !selected;

            var width = ImGui.GetItemRectSize().X;
            maxWidth = Math.Max(width, maxWidth);
            if (i == numOptions - 1)
                maxWidth -= width;

            if (vertical) continue;

            SetItemTooltip(option);
            if (i != numOptions - 1)
                ImGui.SameLine();
        }
        ImGui.PopID();

        if (vertical)
        {
            ImGui.SameLine();
            ImGui.Dummy(new Vector2(maxWidth, 0));
        }
        else if (v >= 0 && v < numOptions)
        {
            ImGui.SameLine();
            var text = optionsArray[v];
            ImGui.TextUnformatted(text);
            ImGui.SameLine();
            ImGui.Dummy(new Vector2(optionsArray.Select(s => ImGui.CalcTextSize(s).X).Max() - ImGui.CalcTextSize(text).X, 0));
        }

        EndGroupBox();
        return ret;
    }

    public static bool RadioBox(string label, ref int v, string options, bool vertical) => RadioBox(label, ref v, options.Split('\0'), vertical);

    public static bool RadioBox<T>(string label, ref T e, bool vertical) where T : struct, Enum
    {
        var names = Enum.GetNames<T>();
        var i = Array.IndexOf(names, Enum.GetName(e));
        var ret = RadioBox(label, ref i, names.Select(name => typeof(T).GetField(name)?.GetCustomAttribute<DisplayAttribute>()?.Name ?? name).ToArray(), vertical);
        if (ret)
            e = Enum.Parse<T>(names[i]);
        return ret;
    }

    public static bool RadioBox<T>(string label, ref T e, T[] optionsArray, bool vertical) where T : struct, Enum
    {
        var i = Array.IndexOf(optionsArray, e);
        var ret = RadioBox(label, ref i, optionsArray.Select(Util.GetDisplayName).ToArray(), vertical);
        if (ret)
            e = optionsArray[i];
        return ret;
    }

    public static bool EnumCombo<T>(string label, ref T e, ImGuiComboFlags flags = ImGuiComboFlags.None) where T : struct, Enum
    {
        if (!ImGui.BeginCombo(label, e.GetDisplayName(), flags)) return false;

        var names = Enum.GetNames<T>();
        var selected = Enum.GetName(e);
        foreach (var name in names)
        {
            if (!ImGui.Selectable($"{typeof(T).GetField(name)?.GetCustomAttribute<DisplayAttribute>()?.Name ?? name}##{name}", name == selected)) continue;
            e = Enum.Parse<T>(name);
            ImGui.EndCombo();
            return true;
        }
        ImGui.EndCombo();

        return false;
    }

    public static bool CheckboxTristate(string label, ref bool? v)
    {
        bool ret;

        var unset = !v.HasValue;
        if (unset)
        {
            var _ = false;
            ret = ImGui.Checkbox(label, ref _);
            if (ret)
                v = true;

            var size = ImGui.GetFrameHeight();
            var padSize = Math.Max(MathF.Floor(size / 4), 1);
            var padding = new Vector2(padSize);
            var min = ImGui.GetItemRectMin();
            var max = min + new Vector2(size);
            ImGui.GetWindowDrawList().AddRect(min + padding, max - padding, ImGui.GetColorU32(ImGuiCol.CheckMark), ImGui.GetStyle().FrameRounding, ImDrawFlags.None, 3 * ImGuiHelpers.GlobalScale);
        }
        else
        {
            var value = v.Value;
            var isFalse = !value;

            if (isFalse)
                ImGui.PushStyleColor(ImGuiCol.CheckMark, Vector4.Zero);

            ret = ImGui.Checkbox(label, ref value);
            if (ret)
                v = value ? null : false;

            if (isFalse)
                ImGui.PopStyleColor();
        }

        return ret;
    }

    public static void FloatingDrawable(Action<ImDrawListPtr, float, Vector2> draw, uint timerMS = 1000)
    {
        var viewport = ImGui.GetWindowViewport() is { IsNull: false } v ? v : ImGui.GetMainViewport();
        var pos = ImGui.GetMousePos();
        var timer = Stopwatch.StartNew();

        void f()
        {
            var percentElapsed = Math.Min(timer.ElapsedMilliseconds / (float)timerMS, 1);

            // Moving a window to the main viewport and then back off while one of these is drawing can sometimes cause a crash if done quickly enough, this flag seems to be set on those viewports though
            if (percentElapsed < 1 && !viewport.Flags.HasFlag(ImGuiViewportFlags.NoTaskBarIcon))
                draw(ImGui.GetForegroundDrawList(viewport), percentElapsed, pos);
            else
                DalamudApi.PluginInterface.UiBuilder.Draw -= f;
        }

        DalamudApi.PluginInterface.UiBuilder.Draw += f;
    }

    public static void FloatingText(string text, uint color = 0xFFFFFFFF, uint timerMS = 1000)
    {
        var textSize = ImGui.CalcTextSize(text);
        var startingAlpha = color >> 24;

        FloatingDrawable((drawList, percentElapsed, pos) =>
        {
            var alphaReduction = percentElapsed > 0.75f ? (uint)(startingAlpha * (percentElapsed - 0.75f) * 4) << 24 : 0;
            pos = new Vector2(pos.X - textSize.X / 2, pos.Y - textSize.Y - 20 * percentElapsed * ImGuiHelpers.GlobalScale);
            drawList.AddText(pos + Vector2.One * ImGuiHelpers.GlobalScale, (startingAlpha << 24) - alphaReduction, text);
            drawList.AddText(pos, color - alphaReduction, text);
        }, timerMS);
    }
}