using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using JREMonitors.Core.Constants;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Widgets;

namespace JREMonitors.Core.Layouts
{
    /// <summary>
    ///     水平排布容器，将子组件按行依次排列并布局。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         仅布局直接子项中实现 <see cref="ILayoutable" /> 的 Widget；每个子项依据其 <see cref="PreferredWidth" />
    ///         的绝对值与 Flex 权重分配宽度，Flex 子项可拉伸占满剩余空间（<see cref="ExplicitAvailableWidth" /> 给出时
    ///         按剩余空间均分，否则按 <see cref="FallbackFlexUnitWidth" /> 兜底），并支持像素取整下的 Flex 余量分摊
    ///         （<c>positionSnapToPixels</c>）。
    ///     </para>
    ///     <para>
    ///         行内对齐：整体在目标区域内的偏移由 <c>rowHorizontalAlignment</c>/<c>rowVerticalAlignment</c>
    ///         （0~1：起点~终点）决定；各行高取子项最大高度，子项纵向位置由 <c>widgetVerticalAlignment</c> 对齐；
    ///         子项间以 <c>widgetSpacing</c> 分隔，并计入各自的 <see cref="MarginWidth" />/<see cref="MarginHeight" />。
    ///     </para>
    ///     <para>
    ///         <see cref="PreferredWidth" />/<see cref="PreferredHeight" /> 为响应式计算属性：宽度汇总参与排布子项的
    ///         固定宽、Flex 权重与间距，高度取子项最大高度；尺寸由外部通过 <see cref="SetLayoutSize" />/
    ///         <see cref="UpdateFromBounds" />（或 <see cref="FromBounds" /> 便捷构造）下发，并以各参数版本签名缓存 Arrange 结果。
    ///     </para>
    /// </remarks>
    public class Row : Group, ILayoutable
    {
        private readonly bool _positionSnapToPixels;
        private readonly float _rowHorizontalAlignment;
        private readonly float _rowVerticalAlignment;
        private readonly bool _spreadHeightFlex;
        private readonly bool _spreadWidthFlex;
        private readonly float _widgetSpacing;
        private readonly float _widgetVerticalAlignment;
        private (long eaw, long fwu, long fhu) _lastArrangeSignature;

        public Row(
            RenderContext context,
            float x = 0,
            float y = 0,
            float fallbackFlexUnitWidth = 0,
            float fallbackFlexUnitHeight = 0,
            float widgetSpacing = 0,
            ICollection<Widget> widgets = null,
            float rowHorizontalAlignment = 0,
            float rowVerticalAlignment = 0,
            float widgetVerticalAlignment = 0,
            float explicitAvailableWidth = 0,
            bool positionSnapToPixels = false,
            float marginWidth = 0,
            float marginHeight = 0,
            bool includeInTotalMajorDimensionSizeWhenVisible = true,
            bool includeInTotalMajorDimensionSizeWhenHidden = false,
            bool skipArrangeWhenHidden = true,
            bool spreadWidthFlex = true,
            bool spreadHeightFlex = true
        ) : base(context, x, y, widgets?.ToArray())
        {
            ExplicitAvailableWidth = CreatePropertySlot(DirtyType.Layout, explicitAvailableWidth);
            FallbackFlexUnitWidth = CreatePropertySlot(DirtyType.Layout, fallbackFlexUnitWidth);
            FallbackFlexUnitHeight = CreatePropertySlot(DirtyType.Layout, fallbackFlexUnitHeight);
            _widgetSpacing = widgetSpacing;
            _rowHorizontalAlignment = rowHorizontalAlignment;
            _rowVerticalAlignment = rowVerticalAlignment;
            _widgetVerticalAlignment = widgetVerticalAlignment;
            _positionSnapToPixels = positionSnapToPixels;
            _spreadWidthFlex = spreadWidthFlex;
            _spreadHeightFlex = spreadHeightFlex;
            MarginWidth = CreatePropertySlot(DirtyType.Layout, marginWidth);
            MarginHeight = CreatePropertySlot(DirtyType.Layout, marginHeight);
            IncludeInTotalMajorDimensionSizeWhenVisible =
                CreatePropertySlot(DirtyType.Layout, includeInTotalMajorDimensionSizeWhenVisible);
            IncludeInTotalMajorDimensionSizeWhenHidden =
                CreatePropertySlot(DirtyType.Layout, includeInTotalMajorDimensionSizeWhenHidden);
            SkipArrangeWhenHidden = CreatePropertySlot(DirtyType.Layout, skipArrangeWhenHidden);
            PreferredWidth = CreatePropertySlot(DirtyType.Layout, source: CreateComputed(() =>
            {
                float totalFixedWidth = 0;
                float totalFlexWeight = 0;
                var contributingCount = 0;
                for (var i = 0; i < Children.Count; i++)
                {
                    var widget = Children[i];
                    if (!(widget is ILayoutable layoutable)) continue;
                    if (!ShouldIncludeInMajorDimensionSize(widget, layoutable)) continue;

                    contributingCount++;
                    totalFixedWidth += layoutable.PreferredWidth.AbsoluteValue + layoutable.MarginWidth;
                    totalFlexWeight += layoutable.PreferredWidth.FlexValue;
                }

                if (contributingCount == 0) return new LayoutLength(0f, 0f);
                totalFixedWidth += _widgetSpacing * (contributingCount - 1);
                if (_spreadWidthFlex)
                    return new LayoutLength(totalFixedWidth, totalFlexWeight);
                return new LayoutLength(totalFixedWidth + totalFlexWeight * FallbackFlexUnitWidth.Value, 0f);
            }));
            PreferredHeight = CreatePropertySlot(DirtyType.Layout, source: CreateComputed(() =>
            {
                float maxAbsHeight = 0;
                float maxFlexWeight = 0;

                for (var i = 0; i < Children.Count; i++)
                {
                    var widget = Children[i];
                    if (!(widget is ILayoutable layoutable)) continue;
                    if (!ShouldArrange(widget, layoutable)) continue;

                    var totalH = layoutable.PreferredHeight.AbsoluteValue + layoutable.MarginHeight;
                    if (totalH > maxAbsHeight) maxAbsHeight = totalH;

                    if (layoutable.PreferredHeight.FlexValue > maxFlexWeight)
                        maxFlexWeight = layoutable.PreferredHeight.FlexValue;
                }

                return new LayoutLength(maxAbsHeight, _spreadHeightFlex ? maxFlexWeight : 0f);
            }));
            TotalWidth = CreateComputed(() =>
            {
                if (ExplicitAvailableWidth.Value > 0) return ExplicitAvailableWidth.Value;
                var pref = PreferredWidth.Value;
                return pref.AbsoluteValue + pref.FlexValue * FallbackFlexUnitWidth.Value;
            });
        }

        public PropertySlot<float> ExplicitAvailableWidth { get; }
        public PropertySlot<float> FallbackFlexUnitHeight { get; }
        public PropertySlot<float> FallbackFlexUnitWidth { get; }
        public Computed<float> TotalWidth { get; }
        public PropertySlot<LayoutLength> PreferredWidth { get; }
        public PropertySlot<LayoutLength> PreferredHeight { get; }
        public PropertySlot<float> MarginWidth { get; }
        public PropertySlot<float> MarginHeight { get; }
        public PropertySlot<bool> IncludeInTotalMajorDimensionSizeWhenVisible { get; }
        public PropertySlot<bool> IncludeInTotalMajorDimensionSizeWhenHidden { get; }
        public PropertySlot<bool> SkipArrangeWhenHidden { get; }

        private List<ILayoutable> Layoutables => Children
            .Where(widget => widget is ILayoutable layoutable && ShouldArrange(widget, layoutable))
            .Cast<ILayoutable>()
            .ToList();

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
        LayoutLength ILayoutable.PreferredWidth => PreferredWidth.Value;
        LayoutLength ILayoutable.PreferredHeight => PreferredHeight.Value;
        float ILayoutable.MarginWidth => MarginWidth.Value;
        float ILayoutable.MarginHeight => MarginHeight.Value;

        bool ILayoutable.IncludeInTotalMajorDimensionSizeWhenVisible =>
            IncludeInTotalMajorDimensionSizeWhenVisible.Value;

        bool ILayoutable.IncludeInTotalMajorDimensionSizeWhenHidden => IncludeInTotalMajorDimensionSizeWhenHidden.Value;
        bool ILayoutable.SkipArrangeWhenHidden => SkipArrangeWhenHidden.Value;

        public void SetLayoutSize(float width, float height)
        {
            ExplicitAvailableWidth.Value = width;
            FallbackFlexUnitHeight.Value = height;
        }

        private static bool ShouldArrange(Widget widget, ILayoutable layoutable)
        {
            return widget.IsVisible || !layoutable.SkipArrangeWhenHidden;
        }

        private static bool ShouldIncludeInMajorDimensionSize(Widget widget, ILayoutable layoutable)
        {
            return widget.IsVisible
                ? layoutable.IncludeInTotalMajorDimensionSizeWhenVisible
                : layoutable.IncludeInTotalMajorDimensionSizeWhenHidden;
        }

        public static Row FromBounds(
            RenderContext context,
            RectangleF bounds,
            float widgetSpacing = 0,
            ICollection<Widget> widgets = null,
            float rowHorizontalAlign = 0,
            float rowVerticalAlign = 0,
            float widgetVerticalAlign = 0,
            bool positionSnapToPixels = false
        )
        {
            return new Row(context, bounds.X, bounds.Y, 0, bounds.Height, widgetSpacing,
                widgets, rowHorizontalAlign, rowVerticalAlign, widgetVerticalAlign, bounds.Width, positionSnapToPixels);
        }

        protected override Widget CreateFallbackWidget()
        {
            return new PlaceHolder(Context);
        }

        public void UpdateFromBounds(RectangleF bounds)
        {
            X.Value = bounds.X;
            Y.Value = bounds.Y;
            ExplicitAvailableWidth.Value = bounds.Width;
            FallbackFlexUnitHeight.Value = bounds.Height;
        }

        private (long eaw, long fwu, long fhu) CaptureArrangeSignature()
        {
            return (ExplicitAvailableWidth.Version,
                FallbackFlexUnitWidth.Version,
                FallbackFlexUnitHeight.Version);
        }

        private void Arrange()
        {
            var layoutables = Layoutables;

            var maxHeight = FallbackFlexUnitHeight.Value;
            for (var i = 0; i < layoutables.Count; i++)
            {
                var layoutable = layoutables[i];
                var h = layoutable.PreferredHeight.AbsoluteValue +
                        layoutable.PreferredHeight.FlexValue * FallbackFlexUnitHeight.Value;
                var totalH = h + layoutable.MarginHeight;
                if (totalH > maxHeight) maxHeight = totalH;
            }

            var totalWidth = TotalWidth.Value;
            var contentStartX = -totalWidth * _rowHorizontalAlignment;
            var contentStartY = -maxHeight * _rowVerticalAlignment;
            if (_positionSnapToPixels)
            {
                contentStartX = (float)Math.Round(X.Value + contentStartX, MidpointRounding.AwayFromZero) -
                                X.Value;
                contentStartY = (float)Math.Round(Y.Value + contentStartY, MidpointRounding.AwayFromZero) -
                                Y.Value;
            }

            float totalFixedWidth = 0;
            float totalFlexWeight = 0;
            float totalMargin = 0;
            var contributingCount = 0;
            for (var i = 0; i < layoutables.Count; i++)
            {
                var layoutable = layoutables[i];
                var widget = (Widget)layoutable;
                if (!ShouldIncludeInMajorDimensionSize(widget, layoutable)) continue;

                contributingCount++;
                totalMargin += layoutable.MarginWidth;
                totalFixedWidth += layoutable.PreferredWidth.AbsoluteValue;
                totalFlexWeight += layoutable.PreferredWidth.FlexValue;
            }

            var flexUnitWidth = FallbackFlexUnitWidth.Value;
            var flexExtraPixels = 0;

            if (ExplicitAvailableWidth.Value > 0 && totalFlexWeight > 0)
            {
                var remainingSpace = ExplicitAvailableWidth.Value - totalFixedWidth - totalMargin -
                                     _widgetSpacing * Math.Max(0, contributingCount - 1);
                flexUnitWidth = remainingSpace > 0 ? remainingSpace / totalFlexWeight : 0;
            }

            if (_positionSnapToPixels && flexUnitWidth > 0)
            {
                var integerUnit = (float)Math.Floor(flexUnitWidth);
                flexExtraPixels = Math.Abs(integerUnit - flexUnitWidth) < Epsilons.FloatEpsilon
                    ? 0
                    : (int)Math.Round(flexUnitWidth * totalFlexWeight - integerUnit * totalFlexWeight);
                flexUnitWidth = integerUnit;
            }

            var currentX = contentStartX;
            var flexCellIndex = 0;
            for (var i = 0; i < Children.Count; i++)
            {
                var widget = Children[i];
                if (!(widget is ILayoutable layoutable) || !ShouldArrange(widget, layoutable)) continue;

                var isContributing = ShouldIncludeInMajorDimensionSize(widget, layoutable);

                var flexCellExtra = 0f;
                if (isContributing && layoutable.PreferredWidth.FlexValue > 0)
                {
                    if (flexCellIndex < flexExtraPixels) flexCellExtra = 1f;
                    flexCellIndex++;
                }

                var finalWidth = layoutable.PreferredWidth.AbsoluteValue +
                                 layoutable.PreferredWidth.FlexValue *
                                 (isContributing ? flexUnitWidth : FallbackFlexUnitWidth.Value) + flexCellExtra;
                var finalHeight = layoutable.PreferredHeight.AbsoluteValue +
                                  layoutable.PreferredHeight.FlexValue * FallbackFlexUnitHeight.Value;
                if (_positionSnapToPixels)
                    finalHeight = (float)Math.Round(finalHeight, MidpointRounding.AwayFromZero);

                var currentY = contentStartY +
                               (maxHeight - finalHeight - layoutable.MarginHeight) * _widgetVerticalAlignment;

                var wx = currentX + layoutable.MarginWidth / 2f;
                var wy = currentY + layoutable.MarginHeight / 2f;
                if (_positionSnapToPixels)
                {
                    wx = (float)Math.Round(X.Value + wx, MidpointRounding.AwayFromZero) - X.Value;
                    wy = (float)Math.Round(Y.Value + wy, MidpointRounding.AwayFromZero) - Y.Value;
                }

                widget.X.Value = wx;
                widget.Y.Value = wy;
                layoutable.SetLayoutSize(finalWidth, finalHeight);

                currentX += finalWidth + layoutable.MarginWidth + _widgetSpacing;
            }
        }

        protected override void OnArrangeLayout(bool ignoreHidden)
        {
            var current = CaptureArrangeSignature();
            if (IsChildrenLayoutDirty || current != _lastArrangeSignature)
            {
                Arrange();
                _lastArrangeSignature = current;
            }

            base.OnArrangeLayout(ignoreHidden);
        }
    }
}