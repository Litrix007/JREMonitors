using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Constants;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Utils.Render;
using Vortice.Direct2D1;

namespace JREMonitors.Core.Boosters
{
    public enum BakerPrescaleMode
    {
        Vector,
        AutoCubic,
        SharpCubic,
        None
    }

    public class Baker : IDisposable
    {
        private readonly Dictionary<Vector2, CachedBitmap> _bitmaps = new Dictionary<Vector2, CachedBitmap>();

        // 不能持有RenderContext引用，否则在全局Cache中会导致上层对象无法被回收
        private readonly ID2D1DeviceContext _dc;
        private readonly BakerPrescaleMode _mode;

        public Baker(RenderContext context, BakerPrescaleMode? mode = null) : this(context.DeviceContext, mode)
        {
        }

        public Baker(ID2D1DeviceContext dc, BakerPrescaleMode? mode = null)
        {
            _dc = dc;
            _mode = mode ?? BakerPrescaleMode.Vector;
        }

        public void Dispose()
        {
            Refresh(false);
        }

        private BakerPrescaleMode GetEffectiveMode(float scaleX, float scaleY)
        {
            if (_mode != BakerPrescaleMode.AutoCubic) return _mode;
            return Math.Max(scaleX, scaleY) >= 1.5f ? BakerPrescaleMode.SharpCubic : BakerPrescaleMode.None;
        }

        private void GetScale(RectangleF bounds, RectangleF? destBounds, RectangleF? sourceBounds,
            out float scaleX, out float scaleY, out int prescaleX, out int prescaleY,
            out BakerPrescaleMode effectiveMode)
        {
            _dc.GetWorldScale(out var worldScaleX, out var worldScaleY);
            var srcWidth = sourceBounds?.Width ?? bounds.Width;
            var srcHeight = sourceBounds?.Height ?? bounds.Height;
            var destWidth = destBounds?.Width ?? srcWidth;
            var destHeight = destBounds?.Height ?? srcHeight;
            var destScaleX = srcWidth > 0 ? destWidth / srcWidth : 1f;
            var destScaleY = srcHeight > 0 ? destHeight / srcHeight : 1f;
            scaleX = worldScaleX * destScaleX;
            scaleY = worldScaleY * destScaleY;
            effectiveMode = GetEffectiveMode(scaleX, scaleY);
            if (effectiveMode != BakerPrescaleMode.None)
            {
                prescaleX = (int)Math.Ceiling(scaleX);
                prescaleY = (int)Math.Ceiling(scaleY);
                if (prescaleX < 1) prescaleX = 1;
                if (prescaleY < 1) prescaleY = 1;
            }
            else
            {
                prescaleX = 1;
                prescaleY = 1;
            }
        }

        private static int RoundUpToPowerOfTwo(int value)
        {
            if (value <= 0) return 1;
            var val = value - 1;
            val |= val >> 1;
            val |= val >> 2;
            val |= val >> 4;
            val |= val >> 8;
            val |= val >> 16;
            return val + 1;
        }

        private CachedBitmap EnsureBitmapAllocated(Vector2 key, int prescaleX, int prescaleY, int width, int height,
            Size requiredSize, BakerPrescaleMode effectiveMode, bool markInvalid)
        {
            if (_bitmaps.TryGetValue(key, out var cached))
            {
                if (requiredSize.Width > cached.PixelSize.Width || requiredSize.Height > cached.PixelSize.Height)
                {
                    cached.Bitmap?.Dispose();
                    cached.Bitmap1X?.Dispose();
                    var powerOfTwoWidth = RoundUpToPowerOfTwo(width);
                    var powerOfTwoHeight = RoundUpToPowerOfTwo(height);
                    var powerOfTwoRequiredSize = new Size(powerOfTwoWidth * prescaleX, powerOfTwoHeight * prescaleY);
                    var bitmap =
                        _dc.CreateBitmap(powerOfTwoRequiredSize, RenderHelper.BitMapProperties8Bit);
                    ID2D1Bitmap bitmap1X = null;
                    if (effectiveMode == BakerPrescaleMode.SharpCubic)
                        bitmap1X = _dc.CreateBitmap(new Size(powerOfTwoWidth, powerOfTwoHeight),
                            RenderHelper.BitMapProperties8Bit);
                    cached.Bitmap = bitmap;
                    cached.Bitmap1X = bitmap1X;
                    cached.PixelSize = powerOfTwoRequiredSize;
                    cached.IsValid = false;
                }
            }
            else
            {
                var powerOfTwoWidth = RoundUpToPowerOfTwo(width);
                var powerOfTwoHeight = RoundUpToPowerOfTwo(height);
                var powerOfTwoRequiredSize = new Size(powerOfTwoWidth * prescaleX, powerOfTwoHeight * prescaleY);
                var bitmap =
                    _dc.CreateBitmap(powerOfTwoRequiredSize, RenderHelper.BitMapProperties8Bit);
                ID2D1Bitmap bitmap1X = null;
                if (effectiveMode == BakerPrescaleMode.SharpCubic)
                    bitmap1X = _dc.CreateBitmap(new Size(powerOfTwoWidth, powerOfTwoHeight),
                        RenderHelper.BitMapProperties8Bit);

                cached = new CachedBitmap(bitmap, bitmap1X, powerOfTwoRequiredSize)
                {
                    IsValid = !markInvalid
                };
                _bitmaps[key] = cached;
            }

            return cached;
        }

        public void Alloc(RectangleF bounds, RectangleF? destBounds = null, RectangleF? sourceBounds = null)
        {
            GetScale(bounds, destBounds, sourceBounds, out var scaleX, out var scaleY, out var prescaleX,
                out var prescaleY, out var effectiveMode);
            var key = new Vector2((float)Math.Round(scaleX, 3), (float)Math.Round(scaleY, 3));
            var width = (int)Math.Ceiling(bounds.Width);
            var height = (int)Math.Ceiling(bounds.Height);
            var requiredSize = new Size(width * prescaleX, height * prescaleY);

            EnsureBitmapAllocated(key, prescaleX, prescaleY, width, height, requiredSize, effectiveMode, true);
        }

        public BitmapEntry Bake(RectangleF bounds, Action action, RectangleF? destBounds = null,
            RectangleF? sourceBounds = null)
        {
            GetScale(bounds, destBounds, sourceBounds, out var scaleX, out var scaleY, out var prescaleX,
                out var prescaleY, out var effectiveMode);
            var key = new Vector2((float)Math.Round(scaleX, 3), (float)Math.Round(scaleY, 3));
            var width = (int)Math.Ceiling(bounds.Width);
            var height = (int)Math.Ceiling(bounds.Height);
            var requiredSize = new Size(width * prescaleX, height * prescaleY);

            if (_bitmaps.TryGetValue(key, out var cached) && cached.IsValid && cached.ActiveSize == requiredSize)
                return new BitmapEntry(cached.Bitmap, prescaleX, prescaleY, requiredSize.Width, requiredSize.Height);
            cached = EnsureBitmapAllocated(key, prescaleX, prescaleY, width, height, requiredSize, effectiveMode,
                false);
            var bitmap = cached.Bitmap;
            var bitmap1X = cached.Bitmap1X;
            var oldTransform = _dc.Transform;

            using (var oldTarget = _dc.Target)
            {
                var transitionTransform = Matrix3x2.CreateTranslation(-bounds.X, -bounds.Y);
                var scaleTransform = Matrix3x2.CreateScale(prescaleX, prescaleY);

                if (effectiveMode == BakerPrescaleMode.SharpCubic && bitmap1X != null)
                {
                    _dc.Target = bitmap1X;
                    _dc.Transform = transitionTransform;
                    _dc.Clear(null);
                    action();
                }

                _dc.Target = bitmap;
                _dc.Clear(null);
                if (effectiveMode == BakerPrescaleMode.SharpCubic && bitmap1X != null)
                {
                    _dc.Transform = scaleTransform;
                    _dc.DrawBitmap(bitmap1X, 1, InterpolationMode.NearestNeighbor);
                }
                else
                {
                    _dc.Transform = transitionTransform * scaleTransform;
                    action();
                }

                _dc.Transform = oldTransform;
                _dc.Target = oldTarget;
            }

            cached.ActiveSize = requiredSize;
            cached.IsValid = true;
            return new BitmapEntry(bitmap, prescaleX, prescaleY, requiredSize.Width, requiredSize.Height);
        }

        public InterpolationMode GetInterpolationMode(InterpolationMode? overrideInterpolationMode = null)
        {
            if (overrideInterpolationMode.HasValue) return overrideInterpolationMode.Value;
            GetScale(RectangleF.Empty, null, null, out var scaleX, out var scaleY, out _, out _, out _);
            var interpolationMode =
                Math.Abs(scaleX - (int)scaleX) < Epsilons.FloatEpsilon &&
                Math.Abs(scaleY - (int)scaleY) < Epsilons.FloatEpsilon
                    ? InterpolationMode.NearestNeighbor
                    : InterpolationMode.Cubic;
            return interpolationMode;
        }

        public void Draw(BitmapEntry entry, RectangleF bounds, RectangleF? destBounds = null,
            RectangleF? sourceBounds = null,
            InterpolationMode? overrideInterpolationMode = null)
        {
            if (!destBounds.HasValue) destBounds = sourceBounds ?? bounds;
            var interpolationMode = GetInterpolationMode(overrideInterpolationMode);
            if (interpolationMode == InterpolationMode.HighQualityCubic) _dc.Flush(out _, out _);
            var actualSourceBounds =
                sourceBounds.HasValue
                    ? new RectangleF(
                        (sourceBounds.Value.X - bounds.X) * entry.PrescaleX,
                        (sourceBounds.Value.Y - bounds.Y) * entry.PrescaleY,
                        sourceBounds.Value.Width * entry.PrescaleX,
                        sourceBounds.Value.Height * entry.PrescaleY)
                    : new RectangleF(0, 0, entry.ActivePixelWidth, entry.ActivePixelHeight);

            _dc.DrawBitmap(entry.Bitmap, destBounds, 1, interpolationMode, actualSourceBounds, null);
        }

        public void BakeAndDraw(RectangleF bounds, Action action,
            RectangleF? destBounds = null,
            RectangleF? sourceBounds = null,
            InterpolationMode? overrideInterpolationMode = null
        )
        {
            var entry = Bake(bounds, action, destBounds, sourceBounds);
            Draw(entry, bounds, destBounds, sourceBounds, overrideInterpolationMode);
        }

        public void Refresh()
        {
            Refresh(true);
        }

        public void Refresh(bool cacheBitmap)
        {
            if (cacheBitmap)
            {
                foreach (var cached in _bitmaps.Values) cached.IsValid = false;
            }
            else
            {
                foreach (var cached in _bitmaps.Values)
                {
                    cached.Bitmap?.Dispose();
                    cached.Bitmap1X?.Dispose();
                }

                _bitmaps.Clear();
            }
        }

        public struct BitmapEntry
        {
            public readonly ID2D1Bitmap Bitmap;
            public readonly int PrescaleX;
            public readonly int PrescaleY;
            public readonly int ActivePixelWidth;
            public readonly int ActivePixelHeight;

            public BitmapEntry(ID2D1Bitmap bitmap, int prescaleX, int prescaleY, int activePixelWidth,
                int activePixelHeight)
            {
                Bitmap = bitmap;
                PrescaleX = prescaleX;
                PrescaleY = prescaleY;
                ActivePixelWidth = activePixelWidth;
                ActivePixelHeight = activePixelHeight;
            }
        }

        private class CachedBitmap
        {
            public CachedBitmap(ID2D1Bitmap bitmap, ID2D1Bitmap bitmap1X, Size pixelSize)
            {
                Bitmap = bitmap;
                Bitmap1X = bitmap1X;
                PixelSize = pixelSize;
                ActiveSize = pixelSize;
                IsValid = true;
            }

            public ID2D1Bitmap Bitmap { get; set; }
            public ID2D1Bitmap Bitmap1X { get; set; }
            public Size PixelSize { get; set; }
            public Size ActiveSize { get; set; }
            public bool IsValid { get; set; }
        }
    }
}