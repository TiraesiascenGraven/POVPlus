using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Dalamud.Bindings.ImGui;

public static partial class ImGuiEx
{
    public unsafe class ListClipper : IEnumerable<(int, int)>, IDisposable
    {
        private ImGuiListClipperPtr clipper;
        private readonly int rows;
        private readonly int columns;
        private readonly bool twoDimensional;
        private readonly int itemRemainder;

        public int FirstRow { get; private set; } = -1;
        public int LastRow => CurrentRow;
        public int CurrentRow { get; private set; }
        public bool IsStepped => CurrentRow == DisplayStart;
        public int DisplayStart => clipper.DisplayStart;
        public int DisplayEnd => clipper.DisplayEnd;

        public IEnumerable<int> Rows
        {
            get
            {
                while (clipper.Step()) // Supposedly this calls End()
                {
                    if (clipper.ItemsHeight > 0 && FirstRow < 0)
                        FirstRow = (int)(ImGui.GetScrollY() / clipper.ItemsHeight);
                    for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                    {
                        CurrentRow = i;
                        yield return twoDimensional ? i : i * columns;
                    }
                }
            }
        }

        public IEnumerable<int> Columns
        {
            get
            {
                var cols = (itemRemainder == 0 || rows != DisplayEnd || CurrentRow != DisplayEnd - 1) ? columns : itemRemainder;
                for (int j = 0; j < cols; j++)
                    yield return j;
            }
        }

        public ListClipper(int items, int cols = 1, bool twoD = false, float itemHeight = 0)
        {
            twoDimensional = twoD;
            columns = cols;
            rows = twoDimensional ? items : (int)MathF.Ceiling((float)items / columns);
            itemRemainder = !twoDimensional ? items % columns : 0;
            clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper());
            clipper.Begin(rows, itemHeight);
        }

        public IEnumerator<(int, int)> GetEnumerator() => (from i in Rows from j in Columns select (i, j)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Dispose()
        {
            clipper.Destroy(); // This also calls End() but I'm calling it anyway just in case
            GC.SuppressFinalize(this);
        }
    }
}