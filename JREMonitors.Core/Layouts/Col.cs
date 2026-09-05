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
    ///     垂直排布容器，将子组件按列依次排列并布局。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         仅布局直接子项中实现 <see cref="ILayoutable" /> 的 Widget；每个子项依据其 <see cref="PreferredHeight" />
    ///         的绝对值与 Flex 权重分配高度，Flex 子项可拉伸占满剩余空间（<see cref="ExplicitAvailableHeight" /> 给出时
    ///         按剩余空间均分，否则按 <see cref="FallbackFlexUnitHeight" /> 兜底），并支持像素取整下的 Flex 余量分摊
    ///         （<c>positionSnapToPixels</c>）。
    ///     </para>
    ///     <para>
    ///         列内对齐：整体在目标区域内的偏移由 <c>colVerticalAlignment</c>/<c>colHorizontalAlignment</c>
    ///         （0~1：起点~终点）决定；各列宽取子项最大宽度，子项横向位置由 <c>widgetHorizontalAlignment</c> 对齐；
    ///         子项间以 <c>widgetSpacing</c> 分隔，并计入各自的 <see cref="MarginWidth" />/<see cref="MarginHeight" />。
    ///     </para>
    ///     <para>
    ///         <see cref="PreferredHeight" />/<see cref="PreferredWidth" /> 为响应式计算属性：高度汇总参与排布子项的
    ///         固定高、Flex 权重与间距，宽度取子项最大宽度；尺寸由外部通过 <see cref="SetLayoutSize" />/
    ///         <see cref="UpdateFromBounds" />（或 <see cref="FromBounds" /> 便捷构造）下发，并以各参数版本签名缓存 Arrange 结果。
    ///     </para>
    /// </remarks>
    public class Col : Widget, ILayoutable
    {
        private readonly float _colHorizontalAlignment;
        private readonly float _colVerticalAlignment;
        private readonly bool _positionSnapToPixels;
        private readonly bool _spreadHeightFlex;
        private readonly bool _spreadWidthFlex;
        private readonly float _widgetHorizontalAlignment;
        private readonly float _widgetSpacing;
        private (long eah, long fwu, long fhu) _lastArrangeSignature;

        public Col(
            RenderContext context,
            float x = 0,
            float y = 0,
            float fallbackFlexUnitWidth = 0,
            float fallbackFlexUnitHeight = 0,
            float widgetSpacing = 0,
            ICollection<Widget> widgets = null,
            float colVerticalAlignment = 0,
            float colHorizontalAlignment = 0,
            float widgetHorizontalAlignment = 0,
            float explicitAvailableHeight = 0,
            bool positionSnapToPixels = false,
            float marginWidth = 0,
            float marginHeight = 0,
            bool includeInTotalMajorDimensionSizeWhenVisible = true,
            bool includeInTotalMajorDimensionSizeWhenHidden = false,
            bool skipArrangeWhenHidden = true,
            bool spreadHeightFlex = true,
            bool spreadWidthFlex = true
        ) : base(context, x, y)
        {
            ExplicitAvailableHeight = CreatePropertySlot(DirtyType.Layout, explicitAvailableHeight);
            FallbackFlexUnitWidth = CreatePropertySlot(DirtyType.Layout, fallbackFlexUnitWidth);
            FallbackFlexUnitHeight = CreatePropertySlot(DirtyType.Layout, fallbackFlexUnitHeight);
            _widgetSpacing = widgetSpacing;
            _colVerticalAlignment = colVerticalAlignment;
            _colHorizontalAlignment = colHorizontalAlignment;
            _widgetHorizontalAlignment = widgetHorizontalAlignment;
            _positionSnapToPixels = positionSnapToPixels;
            _spreadHeightFlex = spreadHeightFlex;
            _spreadWidthFlex = spreadWidthFlex;
            MarginWidth = CreatePropertySlot(DirtyType.Layout, marginWidth);
            MarginHeight = CreatePropertySlot(DirtyType.Layout, marginHeight);
            IncludeInTotalMajorDimensionSizeWhenVisible =
                CreatePropertySlot(DirtyType.Layout, includeInTotalMajorDimensionSizeWhenVisible);
            IncludeInTotalMajorDimensionSizeWhenHidden =
                CreatePropertySlot(DirtyType.Layout, includeInTotalMajorDimensionSizeWhenHidden);
            SkipArrangeWhenHidden = CreatePropertySlot(DirtyType.Layout, skipArrangeWhenHidden);
            PreferredHeight = CreatePropertySlot(DirtyType.Layout, source: CreateComputed(() =>
            {
                float totalFixedHeight = 0;
                float totalFlexWeight = 0;
                var contributingCount = 0;
                for (var i = 0; i < Children.Count; i++)
                {
                    var widget = Children[i];
                    if (!(widget is ILayoutable layoutable)) continue;
                    if (!ShouldIncludeInMajorDimensionSize(widget, layoutable)) continue;

                    contributingCount++;
                    totalFixedHeight += layoutable.PreferredHeight.AbsoluteValue + layoutable.MarginHeight;
                    totalFlexWeight += layoutable.PreferredHeight.FlexValue;
                }

                if (contributingCount == 0) return new LayoutLength(0f, 0f);
                totalFixedHeight += _widgetSpacing * (contributingCount - 1);
                if (_spreadHeightFlex)
                    return new LayoutLength(totalFixedHeight, totalFlexWeight);
                return new LayoutLength(totalFixedHeight + totalFlexWeight * FallbackFlexUnitHeight.Value, 0f);
            }));
            PreferredWidth = CreatePropertySlot(DirtyType.Layout, source: CreateComputed(() =>
            {
                float maxAbsWidth = 0;
                float maxFlexWeight = 0;

                for (var i = 0; i < Children.Count; i++)
                {
                    var widget = Children[i];
                    if (!(widget is ILayoutable layoutable)) continue;
                    if (!ShouldArrange(widget, layoutable)) continue;

                    var totalW = layoutable.PreferredWidth.AbsoluteValue + layoutable.MarginWidth;
                    if (totalW > maxAbsWidth) maxAbsWidth = totalW;

                    if (layoutable.PreferredWidth.FlexValue > maxFlexWeight)
                        maxFlexWeight = layoutable.PreferredWidth.FlexValue;
                }

                return new LayoutLength(maxAbsWidth, _spreadWidthFlex ? maxFlexWeight : 0f);
            }));
            TotalHeight = CreateComputed(() =>
            {
                if (ExplicitAvailableHeight.Value > 0) return ExplicitAvailableHeight.Value;
                var pref = PreferredHeight.Value;
                return pref.AbsoluteValue + pref.FlexValue * FallbackFlexUnitHeight.Value;
            });
            if (widgets == null) return;
            foreach (var widget in widgets)
                AddChild(widget);
        }

        public PropertySlot<float> ExplicitAvailableHeight { get; }
        public PropertySlot<float> FallbackFlexUnitWidth { get; }
        public PropertySlot<float> FallbackFlexUnitHeight { get; }
        public Computed<float> TotalHeight { get; }
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

        bool ILayoutable.IncludeInTotalMajorDimensionSizeWhenHidden =>
            IncludeInTotalMajorDimensionSizeWhenHidden.Value;

        bool ILayoutable.SkipArrangeWhenHidden => SkipArrangeWhenHidden.Value;

        public void SetLayoutSize(float width, float height)
        {
            FallbackFlexUnitWidth.Value = width;
            ExplicitAvailableHeight.Value = height;
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

        public static Col FromBounds(
            RenderContext context,
            RectangleF bounds,
            float widgetSpacing = 0,
            ICollection<Widget> widgets = null,
            float colVerticalAlign = 0,
            float colHorizontalAlign = 0,
            float widgetHorizontalAlign = 0,
            bool positionSnapToPixels = false
        )
        {
            return new Col(context, bounds.X, bounds.Y, bounds.Width, 0, widgetSpacing,
                widgets, colVerticalAlign, colHorizontalAlign, widgetHorizontalAlign, bounds.Height,
                positionSnapToPixels);
        }

        protected override Widget CreateFallbackWidget()
        {
            return new PlaceHolder(Context);
        }

        public void UpdateFromBounds(RectangleF bounds)
        {
            X.Value = bounds.X;
            Y.Value = bounds.Y;
            ExplicitAvailableHeight.Value = bounds.Height;
            FallbackFlexUnitWidth.Value = bounds.Width;
        }

        private (long eah, long fwu, long fhu) CaptureArrangeSignature()
        {
            return (ExplicitAvailableHeight.Version,
                FallbackFlexUnitWidth.Version,
                FallbackFlexUnitHeight.Version);
        }

        private void Arrange()
        {
            var layoutables = Layoutables;

            var maxWidth = FallbackFlexUnitWidth.Value;
            for (var i = 0; i < layoutables.Count; i++)
            {
                var layoutable = layoutables[i];
                var w = layoutable.PreferredWidth.AbsoluteValue +
                        layoutable.PreferredWidth.FlexValue * FallbackFlexUnitWidth.Value;
                var totalW = w + layoutable.MarginWidth;
                if (totalW > maxWidth) maxWidth = totalW;
            }

            var totalHeight = TotalHeight.Value;
            var contentStartX = -maxWidth * _colHorizontalAlignment;
            var contentStartY = -totalHeight * _colVerticalAlignment;
            if (_positionSnapToPixels)
            {
                contentStartX = (float)Math.Round(X.Value + contentStartX, MidpointRounding.AwayFromZero) -
                                X.Value;
                contentStartY = (float)Math.Round(Y.Value + contentStartY, MidpointRounding.AwayFromZero) -
                                Y.Value;
            }

            float totalFixedHeight = 0;
            float totalFlexWeight = 0;
            float totalMargin = 0;
            var contributingCount = 0;
            for (var i = 0; i < layoutables.Count; i++)
            {
                var layoutable = layoutables[i];
                var widget = (Widget)layoutable;
                if (!ShouldIncludeInMajorDimensionSize(widget, layoutable)) continue;

                contributingCount++;
                totalMargin += layoutable.MarginHeight;
                totalFixedHeight += layoutable.PreferredHeight.AbsoluteValue;
                totalFlexWeight += layoutable.PreferredHeight.FlexValue;
            }

            var flexUnitHeight = FallbackFlexUnitHeight.Value;
            var flexExtraPixels = 0;

            if (ExplicitAvailableHeight.Value > 0 && totalFlexWeight > 0)
            {
                var remainingSpace = ExplicitAvailableHeight.Value - totalFixedHeight - totalMargin -
                                     _widgetSpacing * Math.Max(0, contributingCount - 1);
                flexUnitHeight = remainingSpace > 0 ? remainingSpace / totalFlexWeight : 0;
            }

            if (_positionSnapToPixels && flexUnitHeight > 0)
            {
                var integerUnit = (float)Math.Floor(flexUnitHeight);
                flexExtraPixels = Math.Abs(integerUnit - flexUnitHeight) < Epsilons.FloatEpsilon
                    ? 0
                    : (int)Math.Round(flexUnitHeight * totalFlexWeight - integerUnit * totalFlexWeight);
                flexUnitHeight = integerUnit;
            }

            var currentY = contentStartY;
            var flexCellIndex = 0;
            for (var i = 0; i < Children.Count; i++)
            {
                var widget = Children[i];
                if (!(widget is ILayoutable layoutable) || !ShouldArrange(widget, layoutable)) continue;

                var isContributing = ShouldIncludeInMajorDimensionSize(widget, layoutable);

                var flexCellExtra = 0f;
                if (isContributing && layoutable.PreferredHeight.FlexValue > 0)
                {
                    if (flexCellIndex < flexExtraPixels) flexCellExtra = 1f;
                    flexCellIndex++;
                }

                var finalWidth = layoutable.PreferredWidth.AbsoluteValue +
                                 layoutable.PreferredWidth.FlexValue * FallbackFlexUnitWidth.Value;
                if (_positionSnapToPixels)
                    finalWidth = (float)Math.Round(finalWidth, MidpointRounding.AwayFromZero);
                var finalHeight = layoutable.PreferredHeight.AbsoluteValue +
                                  layoutable.PreferredHeight.FlexValue *
                                  (isContributing ? flexUnitHeight : FallbackFlexUnitHeight.Value) + flexCellExtra;

                var currentX = contentStartX +
                               (maxWidth - finalWidth - layoutable.MarginWidth) * _widgetHorizontalAlignment;

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

                currentY += finalHeight + layoutable.MarginHeight + _widgetSpacing;
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