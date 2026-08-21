using System;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Layouts.Text;
using JREMonitors.Core.Utils;
using Vortice.DirectWrite;

namespace JREMonitors.Core.Services.Render
{
    public class TextMetricsCache : ResourceCache<TextMetricsCacheKey, TextMetricsHolder>
    {
        public static readonly PropertyKey Key = new PropertyKey(nameof(TextMetricsCache));

        public MeasureResult[] GetOrCreateMeasureResults(TextMetricsCacheKey key, Func<MeasureResult[]> factory)
        {
            return GetOrCreate(key, factory, f => new TextMetricsHolder(f())).Metrics;
        }
    }

    public class TextMetricsHolder : IDisposable
    {
        public TextMetricsHolder(MeasureResult[] metrics)
        {
            Metrics = metrics;
        }

        public MeasureResult[] Metrics { get; }

        public void Dispose()
        {
            if (Metrics == null) return;
            for (var i = 0; i < Metrics.Length; i++) Metrics[i].Dispose();
        }
    }

    public readonly struct TextFormatKey : IEquatable<TextFormatKey>
    {
        private readonly string _fontFamily;
        private readonly float _fontSize;
        private readonly FontWeight _fontWeight;
        private readonly FontStyle _fontStyle;
        private readonly FontStretch _fontStretch;

        public TextFormatKey(IDWriteTextFormat format)
        {
            if (format != null)
            {
                _fontFamily = format.FontFamilyName;
                _fontSize = format.FontSize;
                _fontWeight = format.FontWeight;
                _fontStyle = format.FontStyle;
                _fontStretch = format.FontStretch;
            }
            else
            {
                _fontFamily = string.Empty;
                _fontSize = 0f;
                _fontWeight = FontWeight.Normal;
                _fontStyle = FontStyle.Normal;
                _fontStretch = FontStretch.Normal;
            }
        }

        public bool Equals(TextFormatKey other)
        {
            return _fontFamily == other._fontFamily &&
                   _fontSize == other._fontSize &&
                   _fontWeight == other._fontWeight &&
                   _fontStyle == other._fontStyle &&
                   _fontStretch == other._fontStretch;
        }

        public override bool Equals(object obj)
        {
            return obj is TextFormatKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_fontFamily, _fontSize, (int)_fontWeight, (int)_fontStyle, (int)_fontStretch);
        }
    }

    public readonly struct TextMetricsCacheKey : IEquatable<TextMetricsCacheKey>
    {
        public readonly RichTextDocument Document;
        public readonly TextFormatKey FormatKey;
        public readonly ContentOrientation Orientation;
        public readonly bool IsGdiCompatible;
        public readonly float? FixedLineSpacing;

        public TextMetricsCacheKey(RichTextDocument document, TextFormatKey formatKey, ContentOrientation orientation,
            bool isGdiCompatible, float? fixedLineSpacing)
        {
            Document = document;
            FormatKey = formatKey;
            Orientation = orientation;
            IsGdiCompatible = isGdiCompatible;
            FixedLineSpacing = fixedLineSpacing;
        }

        public bool Equals(TextMetricsCacheKey other)
        {
            return (Document?.Equals(other.Document) ?? other.Document == null) &&
                   FormatKey.Equals(other.FormatKey) &&
                   Orientation == other.Orientation &&
                   IsGdiCompatible == other.IsGdiCompatible &&
                   Nullable.Equals(FixedLineSpacing, other.FixedLineSpacing);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Document != null ? Document.ContentHash : 0, FormatKey.GetHashCode(),
                (int)Orientation,
                IsGdiCompatible, FixedLineSpacing);
        }
    }

    public static class TextMetricsCacheRenderServiceExtensions
    {
        public static TextMetricsCache GetTextMetricsCache(this RenderContext context)
        {
            return context.GetService<TextMetricsCache>(TextMetricsCache.Key);
        }
    }
}