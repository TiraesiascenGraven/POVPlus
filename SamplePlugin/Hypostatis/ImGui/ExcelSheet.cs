using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dalamud.Interface.Utility;
using Lumina.Excel;

namespace Dalamud.Bindings.ImGui;

public static partial class ImGuiEx
{
    public record ExcelSheetOptions<T> where T : struct, IExcelRow<T>
    {
        public Func<T, string> FormatRow { get; init; } = row => row.ToString();
        public Func<T, string, bool> SearchPredicate { get; init; } = null;
        public Func<T, bool, bool> DrawSelectable { get; init; } = null;
        public IEnumerable<T> FilteredSheet { get; init; } = null;
        public Vector2? Size { get; init; } = null;
    }

    public record ExcelSheetComboOptions<T> : ExcelSheetOptions<T> where T : struct, IExcelRow<T>
    {
        public Func<T, string> GetPreview { get; init; } = null;
        public ImGuiComboFlags ComboFlags { get; init; } = ImGuiComboFlags.None;
    }

    public record ExcelSheetPopupOptions<T> : ExcelSheetOptions<T> where T : struct, IExcelRow<T>
    {
        public ImGuiPopupFlags PopupFlags { get; init; } = ImGuiPopupFlags.None;
        public bool CloseOnSelection { get; init; } = false;
        public Func<T, bool> IsRowSelected { get; init; } = _ => false;
    }

    private static string sheetSearchText;
    private static uint[] filteredSearchIDs;
    private static string prevSearchID;
    private static Type prevSearchType;

    private static void ExcelSheetSearchInput<T>(string id, IEnumerable<T> filteredSheet, Func<T, string, bool> searchPredicate) where T : struct, IExcelRow<T>
    {
        if (ImGui.IsWindowAppearing() && ImGui.IsWindowFocused() && !ImGui.IsAnyItemActive())
        {
            if (id != prevSearchID)
            {
                if (typeof(T) != prevSearchType)
                {
                    sheetSearchText = string.Empty;
                    prevSearchType = typeof(T);
                }

                filteredSearchIDs = null;
                prevSearchID = id;
            }

            ImGui.SetKeyboardFocusHere(0);
        }

        if (ImGui.InputTextWithHint("##ExcelSheetSearch", "Search", ref sheetSearchText, 128, ImGuiInputTextFlags.AutoSelectAll))
            filteredSearchIDs = null;

        filteredSearchIDs ??= filteredSheet.Where(s => searchPredicate(s, sheetSearchText)).Select(r => r.RowId).ToArray();
    }

    public static bool ExcelSheetCombo<T>(string id, ref uint selectedRow, ExcelSheetComboOptions<T> options = null) where T : struct, IExcelRow<T>
    {
        options ??= new ExcelSheetComboOptions<T>();
        var sheet = DalamudApi.DataManager.GetExcelSheet<T>();

        var getPreview = options.GetPreview ?? options.FormatRow;
        if (!ImGui.BeginCombo(id, sheet.GetRowOrDefault(selectedRow) is { } r ? getPreview(r) : selectedRow.ToString(), options.ComboFlags | ImGuiComboFlags.HeightLargest)) return false;

        ExcelSheetSearchInput(id, options.FilteredSheet ?? sheet, options.SearchPredicate ?? ((row, s) => options.FormatRow(row).Contains(s, StringComparison.CurrentCultureIgnoreCase)));

        ImGui.BeginChild("ExcelSheetSearchList", options.Size ?? new Vector2(0, 200 * ImGuiHelpers.GlobalScale), true);

        var ret = false;
        var drawSelectable = options.DrawSelectable ?? ((row, selected) => ImGui.Selectable(options.FormatRow(row), selected));
        using (var clipper = new ListClipper(filteredSearchIDs.Length))
        {
            foreach (var i in clipper.Rows)
            {
                if (sheet.GetRowOrDefault(filteredSearchIDs[i]) is not { } row) continue;

                using var _ = IDBlock.Begin(i);
                if (!drawSelectable(row, selectedRow == row.RowId)) continue;
                selectedRow = row.RowId;
                ret = true;
                break;
            }
        }

        // ImGui issue #273849, children keep popups from closing automatically
        if (ret)
            ImGui.CloseCurrentPopup();

        ImGui.EndChild();
        ImGui.EndCombo();
        return ret;
    }

    public static bool ExcelSheetPopup<T>(string id, out uint selectedRow, ExcelSheetPopupOptions<T> options = null) where T : struct, IExcelRow<T>
    {
        options ??= new ExcelSheetPopupOptions<T>();
        var sheet = DalamudApi.DataManager.GetExcelSheet<T>();
        selectedRow = 0;

        ImGui.SetNextWindowSize(options.Size ?? new Vector2(0, 250 * ImGuiHelpers.GlobalScale));
        if (!ImGui.BeginPopupContextItem(id, options.PopupFlags)) return false;

        ExcelSheetSearchInput(id, options.FilteredSheet ?? sheet, options.SearchPredicate ?? ((row, s) => options.FormatRow(row).Contains(s, StringComparison.CurrentCultureIgnoreCase)));

        ImGui.BeginChild("ExcelSheetSearchList", Vector2.Zero, true);

        var ret = false;
        var drawSelectable = options.DrawSelectable ?? ((row, selected) => ImGui.Selectable(options.FormatRow(row), selected));
        using (var clipper = new ListClipper(filteredSearchIDs.Length))
        {
            foreach (var i in clipper.Rows)
            {
                if (sheet.GetRowOrDefault(filteredSearchIDs[i]) is not { } row) continue;

                using var _ = IDBlock.Begin(i);
                if (!drawSelectable(row, options.IsRowSelected(row))) continue;
                selectedRow = row.RowId;
                ret = true;
            }
        }

        // ImGui issue #273849, children keep popups from closing automatically
        if (ret && options.CloseOnSelection)
            ImGui.CloseCurrentPopup();

        ImGui.EndChild();
        ImGui.EndPopup();
        return ret;
    }

    public static bool ExcelSheetMultiselectPopup<T>(string id, ICollection<uint> selectedRows, ExcelSheetPopupOptions<T> options = null) where T : struct, IExcelRow<T>
    {
        options ??= new ExcelSheetPopupOptions<T>();
        options = options with { IsRowSelected = row => selectedRows.Contains(row.RowId) };
        if (!ExcelSheetPopup(id, out var selectedRow, options)) return false;
        if (!selectedRows.Remove(selectedRow))
            selectedRows.Add(selectedRow);
        return true;
    }

    private static string tableSearchText = string.Empty;
    private static uint[] filteredTableSearchSheetIDs;
    private static string prevTableSearchID;
    private static Type prevTableSearchType;
    private static bool tableCompatMode = false;

    public static void ExcelSheetTable<T>(string id) where T : struct, IExcelRow<T>
    {
        return; // Crashes
        var properties = typeof(T).GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
        var columns = properties.Length;

        if (columns == 0) return;

        ImGui.BeginGroup();

        if (prevTableSearchID != id || prevTableSearchType != typeof(T))
        {
            tableSearchText = string.Empty;
            prevTableSearchType = typeof(T);
            filteredTableSearchSheetIDs = null;
            prevTableSearchID = id;
        }

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        if (ImGui.InputTextWithHint("##ExcelTableSearch", !tableCompatMode ? "\uE052 Search" : "\uE052 Large Table Sorting Enabled", ref tableSearchText, 128, ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EnterReturnsTrue))
            filteredTableSearchSheetIDs = null;

        if (IsItemReleased(ImGuiMouseButton.Right))
            tableCompatMode ^= true;

        if (tableCompatMode)
            ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.GetColorU32(ImGuiCol.TabActive), ImGui.GetStyle().FrameRounding);

        if (!ImGui.BeginTable(id, tableCompatMode && columns >= 64 ? 64 : columns + 1, (columns < 64 || tableCompatMode ? ImGuiTableFlags.Sortable : ImGuiTableFlags.None) | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY))
        {
            ImGui.EndGroup();
            return;
        }

        static string GetObjectAsString(object o) => o switch
        {
            RowRef rowRef => $"{rowRef.GetType().GenericTypeArguments[0].Name}#{rowRef.RowId}",
            //Lumina.Text.SeString seString => seString.ToDalamudString().ToString(),
            _ => o?.ToString() ?? string.Empty
        };

        static IEnumerable<string> GetPropertiesAsStrings(IEnumerable<PropertyInfo> properties, object o)
        {
            foreach (var propertyInfo in properties)
                yield return GetObjectAsString(propertyInfo.GetValue(o));
        }

        static IComparable GetComparable(object o) => o is RowRef rowRef ? rowRef.RowId : o as IComparable ?? GetObjectAsString(o);

        ImGui.TableSetupScrollFreeze(1, 1);
        ImGui.TableSetupColumn("Row");
        for (int col = 0; col < properties.Length; col++)
        {
            if (tableCompatMode && col >= 63)
                break;

            var propertyInfo = properties[col];
            ImGui.TableSetupColumn(propertyInfo.Name);
        }

        ImGui.TableHeadersRow();

        var sheet = DalamudApi.DataManager.GetExcelSheet<T>();
        // Sorting causes crashes if above the current max column limit of 64, so this is required until Dalamud updates ImGui
        var sortSpecs = ImGui.TableGetSortSpecs();
        bool usingSort;
        unsafe
        {
            usingSort = !sortSpecs.IsNull;
        }

        if (filteredTableSearchSheetIDs == null || (usingSort && sortSpecs.SpecsDirty))
        {
            var rows = sheet.Where(row => row.RowId.ToString() == tableSearchText || GetPropertiesAsStrings(properties, row).Any(valueStr => valueStr.Contains(tableSearchText, StringComparison.CurrentCultureIgnoreCase)));

            if (usingSort)
            {
                if (sortSpecs.Specs.ColumnIndex > 0)
                {
                    rows = sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending
                        ? rows.OrderBy(row => GetComparable(properties[sortSpecs.Specs.ColumnIndex - 1].GetValue(row)))
                        : rows.OrderByDescending(row => GetComparable(properties[sortSpecs.Specs.ColumnIndex - 1].GetValue(row)));
                }
                else
                {
                    rows = sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending
                        ? rows.OrderBy(row => row.RowId)
                        : rows.OrderByDescending(row => row.RowId);
                }

                sortSpecs.SpecsDirty = false;
            }

            filteredTableSearchSheetIDs = rows.Select(r => r.RowId).ToArray();
        }
        using var clipper = new ListClipper(filteredTableSearchSheetIDs.Length);
        foreach (var i in clipper.Rows)
        {
            if (sheet.GetRowOrDefault(filteredTableSearchSheetIDs[i]) is not { } row) continue;

            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            ImGui.TextUnformatted(row.RowId.ToString());
            ImGui.TableNextColumn();

            var j = 0;
            foreach (var valueStr in GetPropertiesAsStrings(properties, row))
            {
                ImGui.TextUnformatted(valueStr);

                if (tableCompatMode && j == 62)
                    break;

                if (j++ != properties.Length - 1)
                    ImGui.TableNextColumn();
            }
        }

        ImGui.EndTable();
        ImGui.EndGroup();
    }
}