using System;
using System.Collections.Generic;
using System.Text;
using JREMonitors.Core.Utils;
using Vortice.Mathematics;

namespace JREMonitors.Core.Layouts.Text
{
    /// <summary>
    ///     富文本解析器。
    /// </summary>
    public static class RichTextParser
    {
        public static RichTextDocument Parse(string markup)
        {
            if (string.IsNullOrEmpty(markup))
                return new RichTextDocument(Array.Empty<RichTextSpan>());
            var spans = new List<RichTextSpan>();
            var sb = new StringBuilder();
            var fontSizes = new Stack<float?>();
            var colors = new Stack<Color4?>();
            var spacings = new Stack<float?>();
            var bolds = new Stack<bool>();
            var italics = new Stack<bool>();

            var isRaw = false;

            for (var i = 0; i < markup.Length; i++)
            {
                var c = markup[i];

                if (isRaw)
                {
                    if (i + 5 < markup.Length && markup.Substring(i, 6) == "</raw>")
                    {
                        isRaw = false;
                        i += 5;
                    }
                    else
                    {
                        sb.Append(c);
                    }

                    continue;
                }

                if (c == '\\' && i + 1 < markup.Length)
                {
                    var next = markup[i + 1];
                    if (next == '<' || next == '>' || next == '\\')
                    {
                        sb.Append(next);
                        i++;
                        continue;
                    }
                }

                if (c == '<')
                {
                    var closeIdx = markup.IndexOf('>', i);
                    if (closeIdx != -1)
                    {
                        var tagContent = markup.Substring(i + 1, closeIdx - i - 1).Trim();
                        var isClosing = tagContent.StartsWith("/");
                        var tagCore = isClosing ? tagContent.Substring(1).Trim() : tagContent;
                        var parsed = true;

                        if (tagCore.Equals("raw", StringComparison.OrdinalIgnoreCase) && !isClosing)
                        {
                            FlushSpan();
                            isRaw = true;
                            i = closeIdx;
                            continue;
                        }

                        if (tagCore.StartsWith("size=", StringComparison.OrdinalIgnoreCase) && !isClosing)
                        {
                            FlushSpan();
                            if (float.TryParse(tagCore.Substring(5), out var size))
                                fontSizes.Push(size);
                        }
                        else if (tagCore.Equals("size", StringComparison.OrdinalIgnoreCase) && isClosing)
                        {
                            FlushSpan();
                            if (fontSizes.Count > 0)
                                fontSizes.Pop();
                        }
                        else if (tagCore.StartsWith("color=", StringComparison.OrdinalIgnoreCase) && !isClosing)
                        {
                            FlushSpan();
                            var colorStr = tagCore.Substring(6);
                            colors.Push(colorStr.ToColor4());
                        }
                        else if (tagCore.Equals("color", StringComparison.OrdinalIgnoreCase) && isClosing)
                        {
                            FlushSpan();
                            if (colors.Count > 0)
                                colors.Pop();
                        }
                        else if (tagCore.StartsWith("spacing=", StringComparison.OrdinalIgnoreCase) && !isClosing)
                        {
                            FlushSpan();
                            if (float.TryParse(tagCore.Substring(8), out var spc))
                                spacings.Push(spc);
                        }
                        else if (tagCore.Equals("spacing", StringComparison.OrdinalIgnoreCase) && isClosing)
                        {
                            FlushSpan();
                            if (spacings.Count > 0)
                                spacings.Pop();
                        }
                        else if (tagCore.Equals("b", StringComparison.OrdinalIgnoreCase))
                        {
                            FlushSpan();
                            if (!isClosing)
                                bolds.Push(true);
                            else if (bolds.Count > 0)
                                bolds.Pop();
                        }
                        else if (tagCore.Equals("i", StringComparison.OrdinalIgnoreCase))
                        {
                            FlushSpan();
                            if (!isClosing)
                                italics.Push(true);
                            else if (italics.Count > 0)
                                italics.Pop();
                        }
                        else
                        {
                            parsed = false;
                        }

                        if (parsed)
                        {
                            i = closeIdx;
                            continue;
                        }
                    }
                }

                sb.Append(c);
            }

            FlushSpan();
            return new RichTextDocument(spans);

            void FlushSpan()
            {
                if (sb.Length <= 0) return;
                spans.Add(new RichTextSpan(
                    sb.ToString(),
                    fontSizes.Count > 0 ? fontSizes.Peek() : null,
                    colors.Count > 0 ? colors.Peek() : null,
                    spacings.Count > 0 ? spacings.Peek() : null,
                    bolds.Count > 0 && bolds.Peek(),
                    italics.Count > 0 && italics.Peek()
                ));
                sb.Clear();
            }
        }

        public static RichTextDocument Raw(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new RichTextDocument(Array.Empty<RichTextSpan>());
            return new RichTextDocument(new[] { new RichTextSpan(text) });
        }
    }

    public class RichTextBuilder
    {
        private readonly List<RichTextSpan> _spans = new List<RichTextSpan>();

        public RichTextBuilder Append(string text, float? fontSize = null, Color4? color = null,
            float? textSpacing = null, bool isBold = false, bool isItalic = false)
        {
            if (!string.IsNullOrEmpty(text))
                _spans.Add(new RichTextSpan(text, fontSize, color, textSpacing, isBold, isItalic));

            return this;
        }

        public RichTextBuilder AppendMarkup(string markup)
        {
            if (!string.IsNullOrEmpty(markup))
            {
                var doc = RichTextParser.Parse(markup);
                for (var i = 0; i < doc.Spans.Count; i++) _spans.Add(doc.Spans[i]);
            }

            return this;
        }

        public RichTextDocument Build()
        {
            return new RichTextDocument(_spans);
        }
    }

    public readonly struct RichTextSpan : IEquatable<RichTextSpan>
    {
        public readonly string Text;
        public readonly float? FontSize;
        public readonly Color4? Color;
        public readonly float? TextSpacing;
        public readonly bool IsBold;
        public readonly bool IsItalic;

        public RichTextSpan(string text, float? fontSize = null, Color4? color = null, float? textSpacing = null,
            bool isBold = false, bool isItalic = false)
        {
            Text = text ?? string.Empty;
            FontSize = fontSize;
            Color = color;
            TextSpacing = textSpacing;
            IsBold = isBold;
            IsItalic = isItalic;
        }

        public int GetContentHash()
        {
            return HashCode.Combine(Text, FontSize, Color, TextSpacing, IsBold, IsItalic);
        }

        public bool Equals(RichTextSpan other)
        {
            return Text == other.Text &&
                   FontSize == other.FontSize &&
                   Color == other.Color &&
                   TextSpacing == other.TextSpacing &&
                   IsBold == other.IsBold &&
                   IsItalic == other.IsItalic;
        }

        public override bool Equals(object obj)
        {
            return obj is RichTextSpan other && Equals(other);
        }

        public override int GetHashCode()
        {
            return GetContentHash();
        }
    }

    public class RichTextDocument : IEquatable<RichTextDocument>
    {
        public RichTextDocument(IReadOnlyList<RichTextSpan> spans)
        {
            Spans = spans ?? Array.Empty<RichTextSpan>();
            var hc = new HashCode();
            for (var i = 0; i < Spans.Count; i++) hc.Add(Spans[i].GetContentHash());

            ContentHash = hc.ToHashCode();
        }

        public IReadOnlyList<RichTextSpan> Spans { get; }
        public int ContentHash { get; }

        public bool Equals(RichTextDocument other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (ContentHash != other.ContentHash) return false;
            if (Spans.Count != other.Spans.Count) return false;
            for (var i = 0; i < Spans.Count; i++)
                if (!Spans[i].Equals(other.Spans[i]))
                    return false;

            return true;
        }

        public override int GetHashCode()
        {
            return ContentHash;
        }
    }
}