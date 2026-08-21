using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Text;
using JREMonitors.Core.Constants;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Managers;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Services.Render;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Utils.Render;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;

namespace JREMonitors.Core.Layouts.Text
{
    public class TextLayout : IContentMeasurableBoundsDrawer, IContentHashable
    {
        private readonly ContentArrangement _arrangement;
        private readonly bool _cacheMetrics;
        private readonly bool _ceilContentBounds;
        private readonly RenderContext _context;
        private readonly DisposableStack _disposableStack = new DisposableStack();
        private readonly float? _fixedLineSpacing;
        private readonly ContentFlowDirection _flowDirection;
        private readonly IDWriteTextFormat _format;
        private readonly TextFormatKey _formatKey;
        private readonly bool _isGdiCompatible;
        private readonly ContentOrientation _orientation;
        private readonly float _sizeLimit;
        private readonly Computed<TextLayoutSnapshot> _snapshot;
        private readonly bool _useBounds;

        private MeasureResult[] _instanceMeasureResults;
        private int _lastInstanceDocHash;

        public TextLayout(
            RenderContext context,
            IDWriteTextFormat format,
            RichTextDocument initialDocument = null,
            ContentOrientation? orientation = null,
            float sizeLimit = 0,
            bool useBounds = false,
            ContentFlowDirection? flowDirection = null,
            ContentArrangement? arrangement = null,
            float scaleX = 1,
            float scaleY = 1,
            IEnumerable<float> finalOffsetsMainAxis = null,
            float finalOffsetCrossAxis = 0,
            float? fixedLineSpacing = null,
            bool useHorizontalOverhangMetrics = false,
            bool useVerticalOverhangMetrics = true,
            bool ceilContentBounds = false,
            bool isGdiCompatible = false,
            bool cacheMetrics = true
        )
        {
            _context = context;
            _format = format;
            _formatKey = new TextFormatKey(format);
            _orientation = orientation ?? ContentOrientation.Horizontal;
            _sizeLimit = sizeLimit;
            _useBounds = useBounds;
            _flowDirection = flowDirection ?? ContentFlowDirection.Forward;
            _arrangement = arrangement ?? ContentArrangement.Center;
            _fixedLineSpacing = fixedLineSpacing;
            UseHorizontalOverhangMetrics = new PropertySlot<bool>(useHorizontalOverhangMetrics);
            _disposableStack.AddResource(UseHorizontalOverhangMetrics);
            UseVerticalOverhangMetrics = new PropertySlot<bool>(useVerticalOverhangMetrics);
            _disposableStack.AddResource(UseVerticalOverhangMetrics);
            _ceilContentBounds = ceilContentBounds;
            _isGdiCompatible = isGdiCompatible;
            _cacheMetrics = cacheMetrics;
            Document = new PropertySlot<RichTextDocument>(initialDocument ?? RichTextParser.Raw(string.Empty));
            _disposableStack.AddResource(Document);
            ScaleX = new PropertySlot<float>(scaleX);
            _disposableStack.AddResource(ScaleX);
            ScaleY = new PropertySlot<float>(scaleY);
            _disposableStack.AddResource(ScaleY);
            FinalOffsetCrossAxis = new PropertySlot<float>(finalOffsetCrossAxis);
            _disposableStack.AddResource(FinalOffsetCrossAxis);
            FinalOffsetsMainAxis = new ReactiveList<float>(finalOffsetsMainAxis);
            _disposableStack.AddResource(FinalOffsetsMainAxis);
            _snapshot = new Computed<TextLayoutSnapshot>(() => new TextLayoutSnapshot(this));
            _disposableStack.AddResource(_snapshot);
        }

        public TextLayout(
            RenderContext context,
            IDWriteTextFormat format,
            IValueSignal<RichTextDocument> documentSource,
            ContentOrientation? orientation = null,
            float sizeLimit = 0,
            bool useBounds = false,
            ContentFlowDirection? flowDirection = null,
            ContentArrangement? arrangement = null,
            float scaleX = 1,
            float scaleY = 1,
            IEnumerable<float> finalOffsetsMainAxis = null,
            float finalOffsetCrossAxis = 0,
            float? fixedLineSpacing = null,
            bool useHorizontalOverhangMetrics = false,
            bool useVerticalOverhangMetrics = true,
            bool ceilContentBounds = false,
            bool isGdiCompatible = false,
            bool cacheMetrics = true
        ) : this(context, format, (RichTextDocument)null, orientation, sizeLimit, useBounds, flowDirection, arrangement,
            scaleX, scaleY, finalOffsetsMainAxis, finalOffsetCrossAxis, fixedLineSpacing, useHorizontalOverhangMetrics,
            useVerticalOverhangMetrics, ceilContentBounds, isGdiCompatible, cacheMetrics)
        {
            Document.Bind(documentSource);
        }

        public TextLayout(
            RenderContext context,
            IDWriteTextFormat format,
            string initialMarkupText,
            ContentOrientation? orientation = null,
            float sizeLimit = 0,
            bool useBounds = false,
            ContentFlowDirection? flowDirection = null,
            ContentArrangement? arrangement = null,
            float scaleX = 1,
            float scaleY = 1,
            IEnumerable<float> finalOffsetsMainAxis = null,
            float finalOffsetCrossAxis = 0,
            float? fixedLineSpacing = null,
            bool useHorizontalOverhangMetrics = false,
            bool useVerticalOverhangMetrics = true,
            bool ceilContentBounds = false,
            bool isGdiCompatible = false,
            bool cacheMetrics = true
        ) : this(context, format, RichTextParser.Parse(initialMarkupText), orientation, sizeLimit, useBounds,
            flowDirection, arrangement, scaleX, scaleY, finalOffsetsMainAxis, finalOffsetCrossAxis, fixedLineSpacing,
            useHorizontalOverhangMetrics, useVerticalOverhangMetrics, ceilContentBounds, isGdiCompatible, cacheMetrics)
        {
        }

        public PropertySlot<RichTextDocument> Document { get; }
        public PropertySlot<float> ScaleX { get; }
        public PropertySlot<float> ScaleY { get; }
        public PropertySlot<bool> UseHorizontalOverhangMetrics { get; }
        public PropertySlot<bool> UseVerticalOverhangMetrics { get; }
        public ReactiveList<float> FinalOffsetsMainAxis { get; }
        public PropertySlot<float> FinalOffsetCrossAxis { get; }

        public IContentSnapshot ToSnapshot()
        {
            return _snapshot.Value;
        }

        public void Draw(RectangleF targetBounds, Color4 color)
        {
            _context.CommonBrush.Color = color;
            var sx = ScaleX.Value;
            var sy = ScaleY.Value;
            var oldTransform = _context.DeviceContext.Transform;
            var center = new Vector2(targetBounds.X + targetBounds.Width / 2f,
                targetBounds.Y + targetBounds.Height / 2f);
            _context.DeviceContext.Transform = Matrix3x2.CreateScale(new Vector2(sx, sy), center) * oldTransform;
            TraverseLayout(targetBounds, (pos, measureResult) =>
            {
                _context.DeviceContext.DrawTextLayout(
                    pos,
                    measureResult.Layout,
                    _context.CommonBrush,
                    DrawTextOptions.None
                );
            });
            _context.DeviceContext.Transform = oldTransform;
        }

        public RectangleF GetContentBounds(RectangleF targetBounds)
        {
            return TraverseLayout(targetBounds, null);
        }

        public void Dispose()
        {
            ClearInstanceMeasureResults();
            _disposableStack.Dispose();
        }

        public void Track()
        {
            Document.Track();
            ScaleX.Track();
            ScaleY.Track();
            FinalOffsetCrossAxis.Track();
            FinalOffsetsMainAxis.Track();
            UseHorizontalOverhangMetrics.Track();
            UseVerticalOverhangMetrics.Track();
        }

        private void ClearInstanceMeasureResults()
        {
            if (_instanceMeasureResults != null)
            {
                for (var i = 0; i < _instanceMeasureResults.Length; i++) _instanceMeasureResults[i].Dispose();

                _instanceMeasureResults = null;
            }
        }

        private MeasureResult[] GetMeasureResults()
        {
            var doc = Document.Value ?? RichTextParser.Raw(string.Empty);

            if (_cacheMetrics)
            {
                var key = new TextMetricsCacheKey(doc, _formatKey, _orientation, _isGdiCompatible,
                    _fixedLineSpacing);
                return _context.GetTextMetricsCache().GetOrCreateMeasureResults(key, () => MeasureDocument(doc));
            }

            if (_instanceMeasureResults == null || _lastInstanceDocHash != doc.ContentHash)
            {
                ClearInstanceMeasureResults();
                _instanceMeasureResults = MeasureDocument(doc);
                _lastInstanceDocHash = doc.ContentHash;
            }

            return _instanceMeasureResults;
        }

        private MeasureResult[] MeasureDocument(RichTextDocument doc)
        {
            var lines = BuildLines(doc);
            var results = new MeasureResult[lines.Count];
            var isVertical = _orientation == ContentOrientation.Vertical;

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];

                var spacings = new List<TextSpacing>();
                var weights = new List<TextWeight>();
                var fontSizes = new List<FontSize>();
                var effects = new List<TextEffect>();

                foreach (var range in line.SpanRanges)
                {
                    var span = range.Span;
                    if (range.Length == 0) continue;

                    if (span.FontSize.HasValue)
                        fontSizes.Add(new FontSize(range.Start, range.Length, span.FontSize.Value));

                    if (span.IsBold) weights.Add(new TextWeight(range.Start, range.Length, FontWeight.Bold));

                    if (span.TextSpacing.HasValue)
                        spacings.Add(new TextSpacing
                        {
                            Index = range.Start,
                            Length = range.Length,
                            TrailingSpacing = span.TextSpacing.Value
                        });

                    if (span.Color.HasValue)
                    {
                        var colorBrush = _context.GetBrushCache().GetOrCreateBrush(span.Color.Value);
                        effects.Add(new TextEffect(range.Start, range.Length, colorBrush));
                    }
                }

                results[i] = _context.FontManager.DwFactory.MeasureText(
                    line.FullText,
                    _format,
                    spacings.Count > 0 ? spacings : null,
                    weights.Count > 0 ? weights : null,
                    fontSizes.Count > 0 ? fontSizes : null,
                    effects.Count > 0 ? effects : null,
                    isVertical,
                    _isGdiCompatible
                );
            }

            return results;
        }

        private static List<RichTextLine> BuildLines(RichTextDocument doc)
        {
            var lines = new List<RichTextLine>();
            var currentText = new StringBuilder();
            var currentRanges = new List<SpanRange>();
            var endsWithNewline = false;

            foreach (var span in doc.Spans)
            {
                var text = span.Text.Replace("\r\n", "\n").Replace('\r', '\n');
                var startIdx = 0;
                while (startIdx < text.Length)
                {
                    var nlIdx = text.IndexOf('\n', startIdx);
                    if (nlIdx == -1)
                    {
                        var sub = text.Substring(startIdx);
                        currentRanges.Add(new SpanRange(currentText.Length, sub.Length, span));
                        currentText.Append(sub);
                        endsWithNewline = false;
                        break;
                    }

                    var linePart = text.Substring(startIdx, nlIdx - startIdx);
                    currentRanges.Add(new SpanRange(currentText.Length, linePart.Length, span));
                    currentText.Append(linePart);
                    lines.Add(new RichTextLine(currentText.ToString(), currentRanges.ToArray()));
                    currentText.Clear();
                    currentRanges.Clear();
                    startIdx = nlIdx + 1;
                    endsWithNewline = true;
                }
            }

            if (currentText.Length > 0 || endsWithNewline || lines.Count == 0)
                lines.Add(new RichTextLine(currentText.ToString(), currentRanges.ToArray()));

            return lines;
        }

        private RectangleF TraverseLayout(RectangleF targetBounds, Action<Vector2, MeasureResult> onProcessLine)
        {
            var measureResults = GetMeasureResults();
            if (measureResults == null || measureResults.Length == 0) return RectangleF.Empty;
            var isVertical = _orientation == ContentOrientation.Vertical;
            var lineCount = measureResults.Length;

            var totalSize = 0f;
            for (var i = 0; i < lineCount; i++)
            {
                var m = measureResults[i];
                if (isVertical)
                    totalSize += m.Width;
                else
                    totalSize += UseVerticalOverhangMetrics.Value
                        ? m.Height
                        : m.Layout.Metrics.HeightIncludingTrailingWhitespace;
            }

            float lineGap, startPadding, dimensionSize;

            if (isVertical)
            {
                if (_fixedLineSpacing.HasValue)
                {
                    lineGap = _fixedLineSpacing.Value;
                    var totalBlockWidth = totalSize + lineGap * (lineCount - 1);
                    startPadding = (targetBounds.Width - totalBlockWidth) / 2f;
                }
                else
                {
                    lineGap = (targetBounds.Width - totalSize) / (lineCount + 1);
                    startPadding = lineGap;
                }

                var maxHeight = 0f;
                for (var i = 0; i < lineCount; i++)
                {
                    var h = UseVerticalOverhangMetrics.Value
                        ? measureResults[i].Height
                        : measureResults[i].Layout.Metrics.HeightIncludingTrailingWhitespace;
                    if (h > maxHeight) maxHeight = h;
                }

                dimensionSize = _useBounds ? targetBounds.Height : Math.Max(_sizeLimit, maxHeight);
            }
            else
            {
                if (_fixedLineSpacing.HasValue)
                {
                    lineGap = _fixedLineSpacing.Value;
                    var totalBlockHeight = totalSize + lineGap * (lineCount - 1);
                    startPadding = (targetBounds.Height - totalBlockHeight) / 2f;
                }
                else
                {
                    lineGap = (targetBounds.Height - totalSize) / (lineCount + 1);
                    startPadding = lineGap;
                }

                var maxWidth = 0f;
                for (var i = 0; i < lineCount; i++)
                {
                    var w = UseHorizontalOverhangMetrics.Value
                        ? measureResults[i].Width
                        : measureResults[i].Layout.Metrics.WidthIncludingTrailingWhitespace;
                    if (w > maxWidth) maxWidth = w;
                }

                dimensionSize = _useBounds ? targetBounds.Width : Math.Max(_sizeLimit, maxWidth);
            }

            var center = new Vector2(targetBounds.X + targetBounds.Width / 2f,
                targetBounds.Y + targetBounds.Height / 2f);
            var scaleMatrix = Matrix3x2.CreateScale(new Vector2(ScaleX.Value, ScaleY.Value), center);

            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;

            if (lineCount == 1)
            {
                var ratio = GetAlignmentRatio(0, 1);
                var m = measureResults[0];
                var metrics = m.Layout.Metrics;

                var logicalWidth = metrics.WidthIncludingTrailingWhitespace;
                var logicalHeight = metrics.HeightIncludingTrailingWhitespace;

                var refWidth = UseHorizontalOverhangMetrics.Value ? m.Width : logicalWidth;
                var refHeight = UseVerticalOverhangMetrics.Value ? m.Height : logicalHeight;

                var mainOffset = FinalOffsetsMainAxis.Count > 0 ? FinalOffsetsMainAxis[0] : 0f;
                var crossOffset = FinalOffsetCrossAxis.Value;

                var alignedX = targetBounds.X + (targetBounds.Width - refWidth) * (isVertical ? 0.5f : ratio);
                var alignedY = targetBounds.Y + (targetBounds.Height - refHeight) * (isVertical ? ratio : 0.5f);

                if (!isVertical)
                {
                    alignedX += mainOffset;
                    alignedY += crossOffset;
                }
                else
                {
                    alignedX += crossOffset;
                    alignedY += mainOffset;
                }

                var offset = !isVertical
                    ? m.GetCorrectOffset(UseHorizontalOverhangMetrics.Value, UseVerticalOverhangMetrics.Value)
                    : Vector2.Zero;
                var finalPos = new Vector2(alignedX, alignedY) + offset;

                onProcessLine?.Invoke(finalPos, m);

                var overhang = m.Layout.OverhangMetrics;
                minX = finalPos.X - (UseHorizontalOverhangMetrics.Value ? overhang.Left : 0f);
                maxX = finalPos.X + logicalWidth + (UseHorizontalOverhangMetrics.Value ? overhang.Right : 0f);
                minY = finalPos.Y - (UseVerticalOverhangMetrics.Value ? overhang.Top : 0f);
                maxY = finalPos.Y + logicalHeight + (UseVerticalOverhangMetrics.Value ? overhang.Bottom : 0f);
            }
            else
            {
                if (!isVertical)
                {
                    var y = _flowDirection == ContentFlowDirection.Forward
                        ? targetBounds.Y + startPadding
                        : targetBounds.Bottom - startPadding;
                    y += FinalOffsetCrossAxis.Value;

                    var factor = _flowDirection == ContentFlowDirection.Forward ? 1 : -1;

                    for (var i = 0; i < lineCount; i++)
                    {
                        var m = measureResults[i];
                        var ratio = GetAlignmentRatio(i, lineCount);
                        var offset = m.GetCorrectOffset(UseHorizontalOverhangMetrics.Value,
                            UseVerticalOverhangMetrics.Value);

                        var metrics = m.Layout.Metrics;
                        var logicalWidth = metrics.WidthIncludingTrailingWhitespace;
                        var logicalHeight = metrics.HeightIncludingTrailingWhitespace;
                        var refHeight = UseVerticalOverhangMetrics.Value ? m.Height : logicalHeight;

                        if (_flowDirection == ContentFlowDirection.Reverse) y -= refHeight;

                        var mainOffset = i < FinalOffsetsMainAxis.Count ? FinalOffsetsMainAxis[i] : 0f;
                        var x = targetBounds.X + (targetBounds.Width - dimensionSize) / 2f +
                                (dimensionSize - logicalWidth) * ratio + mainOffset;

                        var finalPos = new Vector2(x, y) + offset;

                        onProcessLine?.Invoke(finalPos, m);

                        var overhang = m.Layout.OverhangMetrics;
                        var lLeft = finalPos.X - (UseHorizontalOverhangMetrics.Value ? overhang.Left : 0f);
                        var lRight = finalPos.X + logicalWidth +
                                     (UseHorizontalOverhangMetrics.Value ? overhang.Right : 0f);
                        var lTop = finalPos.Y - (UseVerticalOverhangMetrics.Value ? overhang.Top : 0f);
                        var lBottom = finalPos.Y + logicalHeight +
                                      (UseVerticalOverhangMetrics.Value ? overhang.Bottom : 0f);

                        if (lLeft < minX) minX = lLeft;
                        if (lRight > maxX) maxX = lRight;
                        if (lTop < minY) minY = lTop;
                        if (lBottom > maxY) maxY = lBottom;

                        y += factor * lineGap;
                        if (_flowDirection == ContentFlowDirection.Forward) y += refHeight;
                    }
                }
                else
                {
                    var x = _flowDirection == ContentFlowDirection.Forward
                        ? targetBounds.X + startPadding
                        : targetBounds.Right - startPadding;
                    x += FinalOffsetCrossAxis.Value;

                    var factor = _flowDirection == ContentFlowDirection.Forward ? 1 : -1;

                    for (var i = 0; i < lineCount; i++)
                    {
                        var m = measureResults[i];
                        var ratio = GetAlignmentRatio(i, lineCount);

                        var metrics = m.Layout.Metrics;
                        var logicalWidth = metrics.WidthIncludingTrailingWhitespace;
                        var logicalHeight = metrics.HeightIncludingTrailingWhitespace;
                        var refHeight = UseVerticalOverhangMetrics.Value ? m.Height : logicalHeight;

                        if (_flowDirection == ContentFlowDirection.Reverse) x -= logicalWidth;

                        var mainOffset = i < FinalOffsetsMainAxis.Count ? FinalOffsetsMainAxis[i] : 0f;
                        var y = targetBounds.Y + (targetBounds.Height - dimensionSize) / 2f +
                                (dimensionSize - refHeight) * ratio + mainOffset;
                        var finalPos = new Vector2(x, y);
                        onProcessLine?.Invoke(finalPos, m);
                        var overhang = m.Layout.OverhangMetrics;
                        var lLeft = finalPos.X - (UseHorizontalOverhangMetrics.Value ? overhang.Left : 0f);
                        var lRight = finalPos.X + logicalWidth +
                                     (UseHorizontalOverhangMetrics.Value ? overhang.Right : 0f);
                        var lTop = finalPos.Y - (UseVerticalOverhangMetrics.Value ? overhang.Top : 0f);
                        var lBottom = finalPos.Y + logicalHeight +
                                      (UseVerticalOverhangMetrics.Value ? overhang.Bottom : 0f);

                        if (lLeft < minX) minX = lLeft;
                        if (lRight > maxX) maxX = lRight;
                        if (lTop < minY) minY = lTop;
                        if (lBottom > maxY) maxY = lBottom;

                        x += factor * lineGap;
                        if (_flowDirection == ContentFlowDirection.Forward) x += logicalWidth;
                    }
                }
            }

            if (_ceilContentBounds)
            {
                minX = (float)Math.Floor(minX);
                maxX = (float)Math.Ceiling(maxX);
                minY = (float)Math.Floor(minY);
                maxY = (float)Math.Ceiling(maxY);
            }

            if (minX > maxX || minY > maxY) return RectangleF.Empty;

            var topLeft = Vector2.Transform(new Vector2(minX, minY), scaleMatrix);
            var bottomRight = Vector2.Transform(new Vector2(maxX, maxY), scaleMatrix);

            return new RectangleF(
                Math.Min(topLeft.X, bottomRight.X),
                Math.Min(topLeft.Y, bottomRight.Y),
                Math.Abs(bottomRight.X - topLeft.X),
                Math.Abs(bottomRight.Y - topLeft.Y)
            );
        }

        private float GetAlignmentRatio(int lineIndex, int lineCount)
        {
            switch (_arrangement)
            {
                case ContentArrangement.Near: return 0f;
                case ContentArrangement.Far: return 1f;
                case ContentArrangement.Step: return lineCount > 1 ? (float)lineIndex / (lineCount - 1) : 0f;
                case ContentArrangement.Center:
                default: return 0.5f;
            }
        }

        private struct SpanRange
        {
            public readonly int Start;
            public readonly int Length;
            public readonly RichTextSpan Span;

            public SpanRange(int start, int length, RichTextSpan span)
            {
                Start = start;
                Length = length;
                Span = span;
            }
        }

        private class RichTextLine
        {
            public RichTextLine(string fullText, SpanRange[] spanRanges)
            {
                FullText = fullText;
                SpanRanges = spanRanges;
            }

            public string FullText { get; }
            public SpanRange[] SpanRanges { get; }
        }

        public sealed class TextLayoutSnapshot : IContentSnapshot, IEquatable<TextLayoutSnapshot>
        {
            private readonly ContentArrangement _arrangement;
            private readonly bool _ceilContentBounds;
            private readonly RichTextDocument _document;
            private readonly float _finalOffsetCrossAxis;
            private readonly float[] _finalOffsetsMainAxis;
            private readonly float? _fixedLineSpacing;
            private readonly ContentFlowDirection _flowDirection;
            private readonly TextFormatKey _formatKey;
            private readonly bool _isGdiCompatible;
            private readonly ContentOrientation _orientation;
            private readonly float _scaleX;
            private readonly float _scaleY;
            private readonly float _sizeLimit;
            private readonly bool _useBounds;
            private readonly bool _useHorizontalOverhangMetrics;
            private readonly bool _useVerticalOverhangMetrics;

            public TextLayoutSnapshot(TextLayout src)
            {
                _document = src.Document.Value;
                _formatKey = src._formatKey;
                _orientation = src._orientation;
                _flowDirection = src._flowDirection;
                _arrangement = src._arrangement;
                _fixedLineSpacing = src._fixedLineSpacing;
                _scaleX = src.ScaleX.Value;
                _scaleY = src.ScaleY.Value;
                _finalOffsetCrossAxis = src.FinalOffsetCrossAxis.Value;
                _sizeLimit = src._sizeLimit;
                _useHorizontalOverhangMetrics = src.UseHorizontalOverhangMetrics.Value;
                _useVerticalOverhangMetrics = src.UseVerticalOverhangMetrics.Value;
                _ceilContentBounds = src._ceilContentBounds;
                _isGdiCompatible = src._isGdiCompatible;
                _useBounds = src._useBounds;

                var list = src.FinalOffsetsMainAxis;
                if (list.Count > 0)
                {
                    _finalOffsetsMainAxis = new float[list.Count];
                    for (var i = 0; i < _finalOffsetsMainAxis.Length; i++)
                        _finalOffsetsMainAxis[i] = list[i];
                }

                ContentHash = ComputeHash();
            }

            public int ContentHash { get; }

            public bool Equals(IContentSnapshot other)
            {
                return other is TextLayoutSnapshot s && Equals(s);
            }

            public bool Equals(TextLayoutSnapshot o)
            {
                if (o == null) return false;
                if (Math.Abs(_scaleX - o._scaleX) > Epsilons.FloatEpsilon) return false;
                if (Math.Abs(_scaleY - o._scaleY) > Epsilons.FloatEpsilon) return false;
                if (Math.Abs(_finalOffsetCrossAxis - o._finalOffsetCrossAxis) > Epsilons.FloatEpsilon) return false;
                if (Math.Abs(_sizeLimit - o._sizeLimit) > Epsilons.FloatEpsilon) return false;
                if (!_formatKey.Equals(o._formatKey)) return false;
                if (_orientation != o._orientation) return false;
                if (_flowDirection != o._flowDirection) return false;
                if (_arrangement != o._arrangement) return false;
                if (!Nullable.Equals(_fixedLineSpacing, o._fixedLineSpacing)) return false;
                if (_useHorizontalOverhangMetrics != o._useHorizontalOverhangMetrics) return false;
                if (_useVerticalOverhangMetrics != o._useVerticalOverhangMetrics) return false;
                if (_ceilContentBounds != o._ceilContentBounds) return false;
                if (_isGdiCompatible != o._isGdiCompatible) return false;
                if (_useBounds != o._useBounds) return false;
                if (_document == null || o._document == null) return false;
                if (!_document.Equals(o._document)) return false;
                var aLen = _finalOffsetsMainAxis?.Length ?? 0;
                var bLen = o._finalOffsetsMainAxis?.Length ?? 0;
                if (aLen != bLen) return false;
                for (var i = 0; i < aLen; i++)
                    if (Math.Abs(_finalOffsetsMainAxis[i] - o._finalOffsetsMainAxis[i]) > Epsilons.FloatEpsilon)
                        return false;
                return true;
            }

            public override bool Equals(object obj)
            {
                return obj is TextLayoutSnapshot s && Equals(s);
            }

            public override int GetHashCode()
            {
                return ContentHash;
            }

            private int ComputeHash()
            {
                var hc = new HashCode();
                hc.Add(_document?.ContentHash ?? 0);
                hc.Add(_scaleX);
                hc.Add(_scaleY);
                hc.Add(_formatKey.GetHashCode());
                hc.Add(_finalOffsetCrossAxis);
                hc.Add((int)_orientation);
                hc.Add((int)_flowDirection);
                hc.Add((int)_arrangement);
                hc.Add(_fixedLineSpacing);
                hc.Add(_useHorizontalOverhangMetrics);
                hc.Add(_useVerticalOverhangMetrics);
                hc.Add(_ceilContentBounds);
                hc.Add(_isGdiCompatible);
                hc.Add(_sizeLimit);
                hc.Add(_useBounds);
                if (_finalOffsetsMainAxis != null)
                    for (var i = 0; i < _finalOffsetsMainAxis.Length; i++)
                        hc.Add(_finalOffsetsMainAxis[i]);
                return hc.ToHashCode();
            }
        }
    }
}