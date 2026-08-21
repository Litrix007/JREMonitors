using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Constants;
using Vortice;
using Vortice.Direct2D1;
using Vortice.Direct3D11;
using Vortice.DirectWrite;
using Vortice.DXGI;
using Vortice.Mathematics;
using Vortice.WIC;
using AlphaMode = Vortice.DCommon.AlphaMode;
using PixelFormat = Vortice.DCommon.PixelFormat;

namespace JREMonitors.Core.Utils.Render
{
    public static class RenderHelper
    {
        public static BitmapProperties1 BitMapProperties8Bit = new BitmapProperties1(
            new PixelFormat(Format.B8G8R8A8_UNorm,
                AlphaMode.Premultiplied),
            96, 96,
            BitmapOptions.Target);

        public static BitmapProperties1 BitMapProperties16Bit = new BitmapProperties1(
            new PixelFormat(Format.R16G16B16A16_UNorm,
                AlphaMode.Premultiplied),
            96, 96,
            BitmapOptions.Target);

        public static Texture2DDescription CreateRenderTargetTextureDescription(int width, int height)
        {
            return new Texture2DDescription
            {
                Width = width,
                Height = height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.B8G8R8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource
            };
        }


        public static Texture2DDescription CreateStagingTextureDescription(int width, int height)
        {
            return new Texture2DDescription
            {
                Width = width,
                Height = height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.B8G8R8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging,
                BindFlags = BindFlags.None,
                CPUAccessFlags = CpuAccessFlags.Read,
                MiscFlags = ResourceOptionFlags.None
            };
        }

        public static ID2D1Bitmap1 LoadBitmapFromResource(
            this ID2D1DeviceContext dc,
            IWICImagingFactory wicFactory,
            Type type,
            string resourceName)
        {
            using (var resourceStream = ResourceHelper.GetResourceStream(type, resourceName))
            using (var wicStream = wicFactory.CreateStream())
            {
                wicStream.Initialize(resourceStream);
                using (var decoder = wicFactory.CreateDecoderFromStream(wicStream))
                using (var frame = decoder.GetFrame(0))
                using (var converter = wicFactory.CreateFormatConverter())
                {
                    converter.Initialize(frame, Vortice.WIC.PixelFormat.Format32bppPBGRA, BitmapDitherType.None, null,
                        0.0, BitmapPaletteType.Custom);
                    return dc.CreateBitmapFromWicBitmap(converter);
                }
            }
        }

        public static void DrawAliasedPolyLine(this ID2D1DeviceContext dc, ID2D1SolidColorBrush brush,
            ID2D1StrokeStyle stroke, params float[] vertices)
        {
            if (vertices == null || vertices.Length < 4 || vertices.Length % 2 != 0) return;
            for (var i = 0; i < vertices.Length - 2; i += 2)
                dc.DrawLine(
                    new Vector2(vertices[i] + 0.5f, vertices[i + 1] + 0.5f),
                    new Vector2(vertices[i + 2] + 0.5f, vertices[i + 3] + 0.5f),
                    brush,
                    1,
                    stroke
                );
        }

        public static MeasureResult MeasureText(
            this IDWriteFactory dwFactory,
            string text,
            IDWriteTextFormat format,
            IEnumerable<TextSpacing> spacings = null,
            IEnumerable<TextWeight> weights = null,
            IEnumerable<FontSize> fontSizes = null,
            IEnumerable<TextEffect> effects = null,
            bool isVertical = false,
            bool isGdiCompatible = false
        )
        {
            IDWriteTextLayout textLayout = null;
            try
            {
                if (isGdiCompatible)
                    textLayout = dwFactory.CreateGdiCompatibleTextLayout(
                        text,
                        text.Length,
                        format,
                        float.MaxValue,
                        float.MaxValue,
                        1,
                        Matrix3x2.Identity,
                        false
                    );
                else
                    textLayout = dwFactory.CreateTextLayout(text, format, float.MaxValue, float.MaxValue);

                if (effects != null)
                    foreach (var effect in effects)
                        textLayout.SetDrawingEffect(effect.Effect, effect.Range);

                if (isVertical)
                {
                    textLayout.ReadingDirection = ReadingDirection.TopToBottom;
                    textLayout.FlowDirection = FlowDirection.RightToLeft;
                }

                var textLayout2 = textLayout.QueryInterface<IDWriteTextLayout2>();
                try
                {
                    if (isVertical)
                        textLayout2.VerticalGlyphOrientation = VerticalGlyphOrientation.Stacked;

                    if (spacings != null)
                        foreach (var spacing in spacings)
                        {
                            if (spacing.Length == 0) continue;
                            textLayout2.SetCharacterSpacing(spacing.LeadingSpacing, spacing.TrailingSpacing,
                                spacing.MinAdvanceWidth,
                                spacing.Range);
                        }

                    if (weights != null)
                        foreach (var weight in weights)
                        {
                            if (weight.Length == 0) continue;
                            textLayout2.SetFontWeight(weight.Weight, weight.Range);
                        }

                    if (fontSizes != null)
                        foreach (var fontSize in fontSizes)
                        {
                            if (fontSize.Length == 0) continue;
                            textLayout2.SetFontSize(fontSize.Size, fontSize.Range);
                        }

                    var metrics = textLayout2.Metrics;
                    var realHeight = metrics.HeightIncludingTrailingWhitespace;
                    var realWidth = metrics.WidthIncludingTrailingWhitespace;
                    textLayout2.MaxWidth = realWidth;
                    textLayout2.MaxHeight = realHeight;
                    var overhang = textLayout2.OverhangMetrics;
                    var fixedHeight = realHeight + overhang.Bottom + overhang.Top;
                    return new MeasureResult(textLayout2, realWidth, fixedHeight);
                }
                catch (Exception)
                {
                    textLayout2.Dispose();
                    throw;
                }
            }
            finally
            {
                textLayout?.Dispose();
            }
        }

        public static void DrawDynamicText(
            this ID2D1DeviceContext dc,
            MeasureResult measureResult,
            float x,
            float y,
            ID2D1Brush brush,
            float horizontalAlignment = 0,
            float verticalAlignment = 0,
            float angleDegrees = 0,
            bool useHorizontalOverhangMetrics = false,
            bool useVerticalOverhangMetrics = true
        )
        {
            var pos = new Vector2(x, y) +
                      measureResult.GetCorrectOffset(useHorizontalOverhangMetrics, useVerticalOverhangMetrics,
                          horizontalAlignment, verticalAlignment);
            if (angleDegrees == 0)
            {
                dc.DrawTextLayout(pos, measureResult.Layout, brush);
            }
            else
            {
                var angleRadian = -angleDegrees * (MathHelper.Pi / 180.0f);
                var skewX = MathHelper.Tan(angleRadian);
                var skewTransform = Matrix3x2.CreateSkew(skewX, 0f, pos);
                var oldTransform = dc.Transform;
                dc.Transform = skewTransform * oldTransform;
                dc.DrawTextLayout(pos, measureResult.Layout, brush);
                dc.Transform = oldTransform;
            }
        }

        public static void DrawDynamicText(
            this ID2D1DeviceContext dc,
            IDWriteFactory dwFactory,
            string text,
            float x,
            float y,
            IDWriteTextFormat format,
            ID2D1Brush brush,
            float horizontalAlignment = 0,
            float verticalAlignment = 0,
            float angleDegrees = 0,
            IEnumerable<TextSpacing> spacings = null,
            IEnumerable<TextWeight> weights = null,
            IEnumerable<FontSize> fontSizes = null,
            IEnumerable<TextEffect> effects = null,
            bool useHorizontalOverhangMetrics = false,
            bool useVerticalOverhangMetrics = true,
            bool isVertical = false
        )
        {
            using (var measureResult =
                   dwFactory.MeasureText(text, format, spacings, weights, fontSizes, effects, isVertical))
            {
                dc.DrawDynamicText(measureResult, x, y, brush, horizontalAlignment, verticalAlignment, angleDegrees,
                    useHorizontalOverhangMetrics, useVerticalOverhangMetrics);
            }
        }

        public static void DrawInnerRoundedRectangle(
            this ID2D1DeviceContext dc,
            RectangleF rect,
            float radiusX,
            float radiusY,
            ID2D1Brush brush,
            float strokeWidth)
        {
            var halfStroke = strokeWidth / 2f;
            var pathRect = new RectangleF(
                rect.X + halfStroke,
                rect.Y + halfStroke,
                rect.Width - strokeWidth,
                rect.Height - strokeWidth
            );
            var pathRx = Math.Max(0f, radiusX - halfStroke);
            var pathRy = Math.Max(0f, radiusY - halfStroke);
            dc.DrawRoundedRectangle(
                new RoundedRectangle(pathRect, pathRx, pathRy),
                brush,
                strokeWidth);
        }

        public static void WithLayer(this ID2D1DeviceContext dc, ID2D1PathGeometry geometry,
            Action<RawRectF> action)
        {
            var bounds = geometry.GetBounds(Matrix3x2.Identity);
            var layerParams = new LayerParameters1
            {
                ContentBounds = bounds,
                GeometricMask = geometry,
                MaskTransform = Matrix3x2.Identity,
                MaskAntialiasMode = AntialiasMode.PerPrimitive,
                Opacity = 1.0f
            };
            dc.PushLayer(ref layerParams, null);
            action(bounds);
            dc.PopLayer();
        }

        public static void GetWorldScale(this ID2D1DeviceContext dc, out float scaleX, out float scaleY)
        {
            var transform = dc.Transform;
            scaleX = new Vector2(transform.M11, transform.M12).Length();
            scaleY = new Vector2(transform.M21, transform.M22).Length();
        }

        public static float GetMaxWorldScale(this ID2D1DeviceContext dc)
        {
            dc.GetWorldScale(out var scaleX, out var scaleY);
            return MathHelper.Max(scaleX, scaleY);
        }

        public static Matrix3x2 CreateRotationMatrixInDegrees(float angleInDegrees)
        {
            return Matrix3x2.CreateRotation(angleInDegrees * (MathHelper.Pi / 180f));
        }

        public static void CopyTexture(
            IntPtr source,
            IntPtr dest,
            int srcPitch,
            int destPitch,
            int width,
            int height,
            int bytesPerPixel = 4)
        {
            unsafe
            {
                var srcPtr = (byte*)source.ToPointer();
                var destPtr = (byte*)dest.ToPointer();
                var widthBytes = (long)width * bytesPerPixel;
                if (srcPitch == widthBytes && destPitch == widthBytes)
                {
                    var totalBytes = widthBytes * height;
                    Buffer.MemoryCopy(srcPtr, destPtr, totalBytes, totalBytes);
                }
                else
                {
                    for (var y = 0; y < height; y++)
                    {
                        Buffer.MemoryCopy(srcPtr, destPtr, widthBytes, widthBytes);
                        srcPtr += srcPitch;
                        destPtr += destPitch;
                    }
                }
            }
        }

        public static void WithAliasedIfNeeded(this ID2D1DeviceContext dc, Action action)
        {
            dc.GetWorldScale(out var scaleX, out var scaleY);
            var oldAntialiasMode = dc.AntialiasMode;
            if (oldAntialiasMode == AntialiasMode.Aliased || (Math.Abs(scaleX - (int)scaleX) < Epsilons.FloatEpsilon &&
                                                              Math.Abs(scaleY - (int)scaleY) < Epsilons.FloatEpsilon))
            {
                action();
                return;
            }

            dc.AntialiasMode = AntialiasMode.Aliased;
            action();
            dc.AntialiasMode = oldAntialiasMode;
        }
    }
}