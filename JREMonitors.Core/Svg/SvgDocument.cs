using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Xml.Linq;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Managers;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Utils;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.Core.Svg
{
    /// <summary>
    ///     Svg 文档绘制器。
    /// </summary>
    public class SvgDocument : IResourceSlot
    {
        private readonly RenderContext _context;
        private readonly DisposableStack _disposableStack = new DisposableStack();
        private readonly Dictionary<string, SvgNode> _elementsMap = new Dictionary<string, SvgNode>();

        public SvgDocument(RenderContext context, SvgDocumentProperties properties)
        {
            _context = context;
            Parse(properties.XmlContent);
            TargetWidth = new PropertySlot<float>(properties.TargetWidth ?? SvgWidth);
            _disposableStack.AddResource(TargetWidth);
            TargetHeight = new PropertySlot<float>(properties.TargetHeight ?? SvgHeight);
            _disposableStack.AddResource(TargetHeight);
            GlobalColor = properties.GlobalColor;
            GlobalColorBrush = context.DeviceContext.CreateSolidColorBrush(Colors.Transparent);
            _disposableStack.AddResource(GlobalColorBrush);
        }

        public PropertySlot<float> TargetWidth { get; }
        public PropertySlot<float> TargetHeight { get; }
        public Color4? GlobalColor { get; set; }
        public ID2D1SolidColorBrush GlobalColorBrush { get; }

        public float SvgWidth { get; private set; }
        public float SvgHeight { get; private set; }
        public RectangleF SvgViewBox { get; private set; }
        public SvgGroup Root { get; private set; }
        public SizeF TargetSize => new SizeF(TargetWidth, TargetHeight);

        public event Action OnInvalidated
        {
            add => Root.OnInvalidated += value;
            remove => Root.OnInvalidated -= value;
        }

        public bool Update(bool force)
        {
            var changed = Root.Update(force);
            return changed;
        }

        public void Dispose()
        {
            Root?.Dispose();
            _disposableStack.Dispose();
            _elementsMap.Clear();
        }

        public void Track()
        {
            TargetWidth.Track();
            TargetHeight.Track();
            TrackNode(Root);
        }

        private void TrackNode(SvgNode node)
        {
            while (true)
            {
                node.IsVisible.Track();
                if (!node.IsVisible.Value) return;
                node.Transform.Track();
                node.Fill.Track();
                node.Stroke.Track();
                node.StrokeWidth.Track();

                if (node is SvgGroup group)
                {
                    var children = group.Children;
                    var count = children.Count;
                    for (var i = 0; i < count; i++) TrackNode(children[i]);
                }
                else if (node is SvgUse use)
                {
                    if (_elementsMap.TryGetValue(use.TargetId, out var target))
                    {
                        node = target;
                        continue;
                    }
                }

                break;
            }
        }

        public void SetVisibility(string id, bool isVisible)
        {
            if (_elementsMap.TryGetValue(id, out var node)) node.IsVisible.Value = isVisible;
        }

        private void Parse(string xmlContent)
        {
            var doc = XDocument.Parse(xmlContent);
            var rootTag = doc.Root;
            if (rootTag == null) throw new FormatException("Invalid svg document.");
            SvgWidth = SvgParser.ParseFloat(rootTag.Attribute("width")?.Value) ?? 0f;
            SvgHeight = SvgParser.ParseFloat(rootTag.Attribute("height")?.Value) ?? 0f;
            var viewBoxStr = rootTag.Attribute("viewBox")?.Value;
            if (!string.IsNullOrEmpty(viewBoxStr))
            {
                var vbParts = viewBoxStr.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (vbParts.Length == 4)
                    SvgViewBox = new RectangleF(
                        SvgParser.ParseFloat(vbParts[0]).Value,
                        SvgParser.ParseFloat(vbParts[1]).Value,
                        SvgParser.ParseFloat(vbParts[2]).Value,
                        SvgParser.ParseFloat(vbParts[3]).Value);
            }
            else
            {
                SvgViewBox = new RectangleF(0f, 0f, SvgWidth, SvgHeight);
            }

            Root = new SvgGroup(_context) { Id = "root", ElementsMap = _elementsMap };
            ParseChildren(rootTag, Root);
        }

        private void ParseChildren(XElement parentElement, SvgGroup parentGroup)
        {
            foreach (var el in parentElement.Elements())
            {
                SvgNode node = null;
                var localName = el.Name.LocalName.ToLower();
                switch (localName)
                {
                    case "g":
                        var group = new SvgGroup(_context) { ElementsMap = _elementsMap };
                        ParseChildren(el, group);
                        node = group;
                        break;
                    case "path":
                        node = new SvgPath(_context, el.Attribute("d")?.Value);
                        break;
                    case "rect":
                    {
                        var x = SvgParser.ParseFloat(el.Attribute("x")?.Value) ?? 0f;
                        var y = SvgParser.ParseFloat(el.Attribute("y")?.Value) ?? 0f;
                        var width = SvgParser.ParseFloat(el.Attribute("width")?.Value) ?? 0f;
                        var height = SvgParser.ParseFloat(el.Attribute("height")?.Value) ?? 0f;
                        var rx = SvgParser.ParseFloat(el.Attribute("rx")?.Value) ?? 0f;
                        var ry = SvgParser.ParseFloat(el.Attribute("ry")?.Value) ?? 0f;
                        node = new SvgRect(_context, x, y, width, height, rx, ry);
                        break;
                    }
                    case "circle":
                        node = new SvgCircle(
                            _context,
                            SvgParser.ParseFloat(el.Attribute("cx")?.Value) ?? 0,
                            SvgParser.ParseFloat(el.Attribute("cy")?.Value) ?? 0,
                            SvgParser.ParseFloat(el.Attribute("r")?.Value) ?? 0);
                        break;
                    case "ellipse":
                    {
                        var cx = SvgParser.ParseFloat(el.Attribute("cx")?.Value) ?? 0f;
                        var cy = SvgParser.ParseFloat(el.Attribute("cy")?.Value) ?? 0f;
                        var rx = SvgParser.ParseFloat(el.Attribute("rx")?.Value) ?? 0f;
                        var ry = SvgParser.ParseFloat(el.Attribute("ry")?.Value) ?? 0f;
                        node = new SvgEllipse(_context, cx, cy, rx, ry);
                        break;
                    }
                    case "use":
                        var href = el.Attribute("href")?.Value ??
                                   el.Attribute("{http://www.w3.org/1999/xlink}href")?.Value;
                        if (!string.IsNullOrEmpty(href) && href.StartsWith("#"))
                            node = new SvgUse(_context, href.Substring(1));

                        break;
                }

                if (node != null)
                {
                    node.ElementsMap = _elementsMap;
                    node.Id = el.Attribute("id")?.Value;
                    node.IsVisible.Value = SvgParser.ParseDisplay(el);
                    node.Transform.Value = SvgParser.ParseTransform(el.Attribute("transform")?.Value);

                    var fillOpacity = SvgParser.ParseFloat(SvgParser.ExtractStyle(el, "fill-opacity"));
                    var fill = SvgParser.ParseColor(SvgParser.ExtractStyle(el, "fill") ??
                                                    el.Attribute("fill")?.Value);
                    if (fill.HasValue && fillOpacity.HasValue) fill = fill.Value.WithAlpha(fillOpacity.Value);
                    node.Fill.Value = fill;

                    node.StrokeWidth.Value = SvgParser.ParseFloat(SvgParser.ExtractStyle(el, "stroke-width") ??
                                                                  el.Attribute("stroke-width")?.Value) ?? 1f;
                    var strokeOpacity = SvgParser.ParseFloat(SvgParser.ExtractStyle(el, "stroke-opacity"));
                    var stroke = SvgParser.ParseColor(SvgParser.ExtractStyle(el, "stroke") ??
                                                      el.Attribute("stroke")?.Value);
                    if (stroke.HasValue && strokeOpacity.HasValue) stroke = stroke.Value.WithAlpha(strokeOpacity.Value);
                    node.Stroke.Value = stroke;

                    var lineCap = SvgParser.ExtractStyle(el, "stroke-linecap") ?? el.Attribute("stroke-linecap")?.Value;
                    var capStyle = SvgParser.ParseStrokeLineCap(lineCap);
                    if (capStyle.HasValue)
                    {
                        var props = new StrokeStyleProperties
                        {
                            StartCap = capStyle.Value,
                            EndCap = capStyle.Value,
                            DashCap = capStyle.Value,
                            LineJoin = LineJoin.Miter,
                            MiterLimit = 1f,
                            DashStyle = DashStyle.Solid,
                            DashOffset = 0f
                        };
                        node.StrokeStyle = _context.D2D1Factory.CreateStrokeStyle(props);
                    }

                    if (node.Fill.Value.HasValue) node.FillBrush.Color = node.Fill.Value.Value;
                    if (node.Stroke.Value.HasValue) node.StrokeBrush.Color = node.Stroke.Value.Value;

                    if (!string.IsNullOrEmpty(node.Id)) _elementsMap[node.Id] = node;

                    parentGroup.Children.Add(node);
                }
            }
        }

        public void Draw(ID2D1DeviceContext deviceContext)
        {
            var oldTransform = deviceContext.Transform;
            var scaleX = TargetWidth.Value / SvgViewBox.Width;
            var scaleY = TargetHeight.Value / SvgViewBox.Height;
            var transform = Matrix3x2.CreateTranslation(-SvgViewBox.X, -SvgViewBox.Y) *
                            Matrix3x2.CreateScale(scaleX, scaleY);
            deviceContext.Transform = transform * oldTransform;
            var hasGlobalColor = GlobalColor.HasValue;
            if (hasGlobalColor) GlobalColorBrush.Color = GlobalColor.Value;
            RenderNode(deviceContext, Root, hasGlobalColor, _elementsMap);
            deviceContext.Transform = oldTransform;
        }

        private void RenderNode(ID2D1DeviceContext dc, SvgNode node, bool hasGlobalFill,
            Dictionary<string, SvgNode> elementMap)
        {
            if (!node.IsVisible.Value) return;
            switch (node)
            {
                case SvgGroup group:
                {
                    var oldTransform = dc.Transform;
                    dc.Transform = node.Transform.Value * oldTransform;
                    var children = group.Children;
                    var count = children.Count;
                    for (var i = 0; i < count; i++) RenderNode(dc, children[i], hasGlobalFill, elementMap);
                    dc.Transform = oldTransform;
                    break;
                }
                case SvgUse use:
                {
                    if (elementMap.TryGetValue(use.TargetId, out var target))
                        if (target.IsVisible.Value)
                        {
                            var tg = use.GetBaseGeometry();
                            if (tg != null) DrawGeometryNode(dc, tg, target, hasGlobalFill);
                        }

                    break;
                }
                default:
                {
                    var oldTransform = dc.Transform;
                    dc.Transform = node.Transform.Value * oldTransform;
                    var geom = node.GetBaseGeometry();
                    if (geom != null) DrawGeometryNode(dc, geom, node, hasGlobalFill);
                    dc.Transform = oldTransform;
                    break;
                }
            }
        }

        private void DrawGeometryNode(ID2D1DeviceContext dc, ID2D1Geometry geometry, SvgNode styleNode,
            bool isGlobalColorBrush)
        {
            if (styleNode.Fill.Value.HasValue)
            {
                var fillBrush = isGlobalColorBrush ? GlobalColorBrush : styleNode.FillBrush;
                dc.FillGeometry(geometry, fillBrush);
            }

            if (styleNode.Stroke.Value.HasValue)
            {
                var strokeBrush = isGlobalColorBrush ? GlobalColorBrush : styleNode.StrokeBrush;
                dc.DrawGeometry(geometry, strokeBrush, styleNode.StrokeWidth.Value, styleNode.StrokeStyle);
            }
        }
    }
}