using System;
using System.Collections.Generic;
using System.Linq;

namespace JREMonitors.Core.Layouts
{
    public enum AdjacencyDirection
    {
        Left,
        Right,
        Top,
        Bottom
    }

    public class AdjacencyConnection<TElement, TState> where TElement : class, IExpandableElement<TState>
    {
        public AdjacencyConnection(TElement a, TElement b, AdjacencyDirection direction, float spacing)
        {
            ElementA = a;
            ElementB = b;
            DirectionFromAToB = direction;
            Spacing = spacing;
        }

        public TElement ElementA { get; }
        public TElement ElementB { get; }
        public AdjacencyDirection DirectionFromAToB { get; }
        public float Spacing { get; }
    }

    public abstract class AdjacencyManagerBase<TElement, TState> where TElement : class, IExpandableElement<TState>
    {
        private readonly List<AdjacencyConnection<TElement, TState>> _connections =
            new List<AdjacencyConnection<TElement, TState>>();

        private readonly List<TElement> _elements = new List<TElement>();

        protected abstract bool IsActive(TState state);

        public void RegisterElement(TElement element)
        {
            if (element != null && !_elements.Contains(element))
                _elements.Add(element);
        }

        public void AddConnection(TElement elementA, TElement elementB, AdjacencyDirection direction, float spacing)
        {
            if (elementA == null || elementB == null) return;
            RegisterElement(elementA);
            RegisterElement(elementB);
            _connections.Add(new AdjacencyConnection<TElement, TState>(elementA, elementB, direction, spacing));
        }

        public void AddFromRow(Row row, float spacing)
        {
            if (row == null) return;
            var children = row.Children.ToList();
            for (var k = 0; k < children.Count - 1; k++)
                if (children[k] is TElement elemA && children[k + 1] is TElement elemB)
                    AddConnection(elemA, elemB, AdjacencyDirection.Right, spacing);
        }

        public void AddFromRowGrid(RowGrid grid, float rowSpacing, float colSpacing)
        {
            if (grid == null) return;
            var rows = grid.Rows;
            foreach (var row in rows) AddFromRow(row, colSpacing);
            for (var i = 0; i < rows.Count - 1; i++)
            {
                var rowCurrent = rows[i].Children.ToList();
                var rowNext = rows[i + 1].Children.ToList();
                var colCount = Math.Min(rowCurrent.Count, rowNext.Count);
                for (var k = 0; k < colCount; k++)
                    if (rowCurrent[k] is TElement elemA && rowNext[k] is TElement elemB)
                        AddConnection(elemA, elemB, AdjacencyDirection.Bottom, rowSpacing);
            }
        }

        public void AddFromRowGrid(RowGrid grid, float spacing)
        {
            AddFromRowGrid(grid, spacing, spacing);
        }

        public void UpdateLimits()
        {
            var limits = new Dictionary<TElement, (float Left, float Right, float Top, float Bottom)>();
            foreach (var elem in _elements)
            {
                var max = elem.MaxDynamicExpansion;
                limits[elem] = (max, max, max, max);
            }

            for (var i = 0; i < _connections.Count; i++)
            {
                var conn = _connections[i];
                var a = conn.ElementA;
                var b = conn.ElementB;
                var dir = conn.DirectionFromAToB;
                var s = conn.Spacing;

                var maxA = a.MaxDynamicExpansion;
                var maxB = b.MaxDynamicExpansion;

                var isActiveA = IsActive(a.ExpansionState);
                var isActiveB = IsActive(b.ExpansionState);

                var staticA = isActiveA
                    ? dir == AdjacencyDirection.Right ? a.StaticExtensionRight : a.StaticExtensionBottom
                    : 0f;
                var staticB = isActiveB
                    ? dir == AdjacencyDirection.Right ? b.StaticExtensionLeft : b.StaticExtensionTop
                    : 0f;

                var effectiveSpacing = s - staticA - staticB;

                var limitA = maxA;
                var limitB = maxB;

                if (isActiveA && isActiveB)
                {
                    if (effectiveSpacing < 0)
                    {
                        limitA = 0;
                        limitB = 0;
                    }
                    else if (maxA + maxB > effectiveSpacing)
                    {
                        limitA = Math.Min(maxA, effectiveSpacing / 2f);
                        limitB = Math.Min(maxB, effectiveSpacing / 2f);
                    }
                }
                else if (isActiveA)
                {
                    limitA = Math.Min(maxA, Math.Max(0, effectiveSpacing));
                    limitB = 0;
                }
                else if (isActiveB)
                {
                    limitA = 0;
                    limitB = Math.Min(maxB, Math.Max(0, effectiveSpacing));
                }
                else
                {
                    limitA = 0;
                    limitB = 0;
                }

                var currentA = limits[a];
                var currentB = limits[b];

                switch (dir)
                {
                    case AdjacencyDirection.Right:
                        limits[a] = (currentA.Left, Math.Min(currentA.Right, limitA), currentA.Top, currentA.Bottom);
                        limits[b] = (Math.Min(currentB.Left, limitB), currentB.Right, currentB.Top, currentB.Bottom);
                        break;
                    case AdjacencyDirection.Left:
                        limits[a] = (Math.Min(currentA.Left, limitA), currentA.Right, currentA.Top, currentA.Bottom);
                        limits[b] = (currentB.Left, Math.Min(currentB.Right, limitB), currentB.Top, currentB.Bottom);
                        break;
                    case AdjacencyDirection.Bottom:
                        limits[a] = (currentA.Left, currentA.Right, currentA.Top, Math.Min(currentA.Bottom, limitA));
                        limits[b] = (currentB.Left, currentB.Right, Math.Min(currentB.Top, limitB), currentB.Bottom);
                        break;
                    case AdjacencyDirection.Top:
                    default:
                        limits[a] = (currentA.Left, currentA.Right, Math.Min(currentA.Top, limitA), currentA.Bottom);
                        limits[b] = (currentB.Left, currentB.Right, currentB.Top, Math.Min(currentB.Bottom, limitB));
                        break;
                }
            }

            for (var i = 0; i < _elements.Count; i++)
            {
                var elem = _elements[i];
                var l = limits[elem];
                elem.SetDynamicExpansionLimits(l.Left, l.Right, l.Top, l.Bottom);
            }
        }
    }
}