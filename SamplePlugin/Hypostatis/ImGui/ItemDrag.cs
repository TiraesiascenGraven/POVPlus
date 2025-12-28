using System;
using System.Numerics;
using Dalamud.Interface.Utility;

namespace Dalamud.Bindings.ImGui;

public static partial class ImGuiEx
{
    private static object dragID = null;
    private static bool isDraggingItem = false;
    private static bool initialPosition = false;
    private static Vector2 lastGridPosition = Vector2.Zero;
    private static Vector2 gridCenter = Vector2.Zero;

    public static bool IsItemDragged(object id, ImGuiMouseButton button, float gridSize, bool drawGrid, out Vector2 pos)
    {
        if (!GetDragLock(id, ImGuiMouseButton.Left))
        {
            pos = Vector2.Zero;
            return false;
        }

        pos = GetMouseGridPosition(gridSize) * gridSize;

        if (drawGrid)
            DrawGrid(gridSize, gridSize / 10 + 1, Vector2.Zero);

        if (initialPosition)
        {
            lastGridPosition = pos;
            initialPosition = false;
            return false;
        }

        var ret = pos != lastGridPosition;
        if (ret)
            lastGridPosition = pos;
        return ret;
    }

    public static bool IsItemDraggedDelta(object id, ImGuiMouseButton button, float gridSize, bool drawGrid, out Vector2 delta)
    {
        if (!GetDragLock(id, ImGuiMouseButton.Left))
        {
            delta = Vector2.Zero;
            return false;
        }

        var gridOffset = new Vector2(MathF.Round(gridCenter.X % gridSize), MathF.Round(gridCenter.Y % gridSize));
        var gridPosition = GetMouseGridPosition(gridSize, gridOffset);
        delta = gridPosition - lastGridPosition;

        if (drawGrid)
            DrawGrid(gridSize, gridSize / 10 + 1, gridOffset);

        if (initialPosition)
        {
            lastGridPosition = gridPosition;
            initialPosition = false;
            return false;
        }

        var ret = gridPosition != lastGridPosition;
        if (ret)
            lastGridPosition = gridPosition;
        return ret;
    }

    private static bool GetDragLock(object id, ImGuiMouseButton button)
    {
        if (isDraggingItem && !ImGui.IsAnyMouseDown())
        {
            dragID = null;
            isDraggingItem = false;
        }

        if (!isDraggingItem && ImGui.IsItemHovered())
            ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);

        var imguiID = id switch
        {
            string s => ImGui.GetID(s),
            _ when Util.IsNumeric(id) => ImGui.GetID(id.ToString()),
            _ => id
        };

        if ((!isDraggingItem && !ImGui.IsItemClicked(button)) || (dragID != null && (dragID is uint u1 && imguiID is uint u2 ? u1 != u2 : dragID != imguiID))) return false;

        if (!isDraggingItem)
        {
            BlockWindowDrag();
            dragID = imguiID;
            isDraggingItem = true;
            gridCenter = ImGui.GetMousePos();
            initialPosition = true;
        }

        if (ImGui.IsMouseDragging(button, 0)) return true;
        dragID = null;
        isDraggingItem = false;
        return false;
    }

    private static Vector2 GetMouseGridPosition(float gridSize, Vector2 offset = default)
    {
        var mousePos = ImGui.GetMousePos() - offset;
        return new Vector2(MathF.Round(mousePos.X / gridSize), MathF.Round(mousePos.Y / gridSize));
    }

    private static void DrawGrid(float size, float circleRadius = 0, Vector2 offset = default)
    {
        var drawList = ImGui.GetForegroundDrawList();

        var screenSize = ImGuiHelpers.MainViewport.Size;
        for (float x = offset.X == 0 ? size : offset.X; x < screenSize.X; x += size)
            drawList.AddLine(new Vector2(x, 0), screenSize with { X = x }, 0xFFFFFFFF);
        for (float y = offset.Y == 0 ? size : offset.Y; y < screenSize.Y; y += size)
            drawList.AddLine(new Vector2(0, y), screenSize with { Y = y }, 0xFFFFFFFF);

        if (circleRadius == 0) return;
        drawList.AddCircleFilled(GetMouseGridPosition(size, offset) * size + offset, circleRadius, 0xFFFFFFFF);
    }
}