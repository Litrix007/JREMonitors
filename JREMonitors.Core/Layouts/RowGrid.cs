using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using JREMonitors.Core.Constants;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Widgets;

namespace JREMonitors.Core.Layouts
{
    public class RowGrid : Widget
    {
        public RowGrid(
            RenderContext context,
            RectangleF bounds,
            float rowSpacing,
            float colSpacing,
            IList<ICollection<Widget>> rows,
            float horizontalAlignment = 0,
            float verticalAlignment = 0,
            bool positionSnapToPixels = false
        ) : base(context)
        {
            if (rows == null || rows.Count == 0) return;
            var rowCount = rows.Count;
            var maxCols = rows.Max(r => r.Count);
            var rowHeight = (bounds.Height - rowSpacing * (rowCount - 1)) / rowCount;
            var rowHeightExtra = 0;
            if (positionSnapToPixels && rowHeight > 0)
            {
                var integerRowHeight = (float)Math.Floor(rowHeight);
                rowHeightExtra = Math.Abs(integerRowHeight - rowHeight) < Epsilons.FloatEpsilon
                    ? 0
                    : (int)Math.Round(rowHeight * rowCount - integerRowHeight * rowCount);
                rowHeight = integerRowHeight;
            }

            var rowWidgets = new Row[rowCount];
            for (var i = 0; i < rowCount; i++)
            {
                var row = rows[i].ToList();
                while (row.Count < maxCols) row.Add(null);
                var rowX = bounds.X;
                var rowY = bounds.Y + i * (rowHeight + rowSpacing) - verticalAlignment * bounds.Height;
                var rowH = rowHeight + (i < rowHeightExtra ? 1 : 0);

                if (positionSnapToPixels)
                {
                    rowX = (float)Math.Round(rowX, MidpointRounding.AwayFromZero);
                    rowY = (float)Math.Round(rowY, MidpointRounding.AwayFromZero);
                }

                var rowBounds = new RectangleF(rowX, rowY, bounds.Width, rowH);
                rowWidgets[i] = Row.FromBounds(
                    context,
                    rowBounds,
                    colSpacing,
                    row,
                    horizontalAlignment,
                    positionSnapToPixels: positionSnapToPixels
                );
            }

            foreach (var row in rowWidgets)
            {
                AddChild(row);
                Rows.Add(row);
            }
        }

        public RowGrid(
            RenderContext context,
            RectangleF bounds,
            float spacing,
            IList<ICollection<Widget>> rows,
            float horizontalAlignment = 0,
            float verticalAlignment = 0,
            bool positionSnapToPixels = false
        ) : this(context, bounds, spacing, spacing, rows, horizontalAlignment, verticalAlignment, positionSnapToPixels)
        {
        }

        public List<Row> Rows { get; } = new List<Row>();

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
    }
}