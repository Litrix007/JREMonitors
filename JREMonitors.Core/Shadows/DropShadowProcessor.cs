using System;
using System.Collections.Generic;
using System.Numerics;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.Core.Shadows
{
    /// <summary>
    ///     外阴影处理器。
    /// </summary>
    public class DropShadowProcessor : IDisposable
    {
        private readonly ID2D1DeviceContext _dc;

        private readonly Dictionary<DropShadow, DropShadowChain> _dropChains =
            new Dictionary<DropShadow, DropShadowChain>();

        private readonly DropShadowChain _dynamicDropChain;
        private bool _disposed;

        public DropShadowProcessor(ID2D1DeviceContext dc)
        {
            _dc = dc;
            _dynamicDropChain = new DropShadowChain(dc);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _dynamicDropChain?.Dispose();
            foreach (var chain in _dropChains.Values) chain.Dispose();
            _dropChains.Clear();
            _disposed = true;
        }

        private DropShadowChain GetOrCreateDropChain(DropShadow dropShadow)
        {
            if (_dropChains.TryGetValue(dropShadow, out var chain)) return chain;
            chain = new DropShadowChain(_dc, dropShadow);
            _dropChains[dropShadow] = chain;
            return chain;
        }

        private static Matrix5x4 CreateShadowColorMatrix(Color4 shadowColor)
        {
            var a = shadowColor.A;
            return new Matrix5x4(
                0f, 0f, 0f, 0f,
                0f, 0f, 0f, 0f,
                0f, 0f, 0f, 0f,
                0f, 0f, 0f, a,
                shadowColor.R, shadowColor.G, shadowColor.B, 0f
            );
        }

        public void DrawDropShadows(IReadOnlyList<DropShadow> shadows, ID2D1CommandList commandList, bool cache = true)
        {
            foreach (var shadow in shadows)
            {
                if (shadow.Color.A <= 0) continue;
                DropShadowChain chain;
                if (cache)
                {
                    chain = GetOrCreateDropChain(shadow);
                }
                else
                {
                    chain = _dynamicDropChain;
                    chain.Apply(shadow);
                }

                chain.Offset.SetInput(0, commandList, true);
                _dc.DrawImage(chain.Opacity);
                chain.Offset.SetInput(0, null, true);
            }
        }

        public void DrawWithDropShadows(IReadOnlyList<DropShadow> shadows,
            Action drawingAction = null,
            Action noShadowAction = null, Action shadowAction = null, bool cache = true)
        {
            if (_disposed || (drawingAction == null && noShadowAction == null && shadowAction == null)) return;
            noShadowAction?.Invoke();
            if (shadows == null || shadows.Count == 0)
            {
                drawingAction?.Invoke();
                return;
            }

            using (var maskCommandList = _dc.CreateCommandList())
            {
                using (var oldTarget = _dc.Target)
                {
                    var oldTransform = _dc.Transform;
                    _dc.Target = maskCommandList;
                    _dc.Transform = Matrix3x2.Identity;
                    drawingAction?.Invoke();
                    shadowAction?.Invoke();
                    maskCommandList.Close();
                    _dc.Transform = oldTransform;
                    _dc.Target = oldTarget;
                }

                DrawDropShadows(shadows, maskCommandList, cache);
                drawingAction?.Invoke();
            }
        }

        private class DropShadowChain : IDisposable
        {
            public DropShadowChain(ID2D1DeviceContext dc)
            {
                Offset = new ID2D1Effect(dc.CreateEffect(EffectGuids.AffineTransform2D));
                BlurX = new ID2D1Effect(dc.CreateEffect(EffectGuids.DirectionalBlur));
                BlurX.SetValue((int)DirectionalBlurProperties.Angle, 0.0f);
                BlurX.SetValue((int)DirectionalBlurProperties.BorderMode, BorderMode.Soft);
                BlurX.SetValue((int)DirectionalBlurProperties.Optimization, DirectionalBlurOptimization.Speed);
                BlurY = new ID2D1Effect(dc.CreateEffect(EffectGuids.DirectionalBlur));
                BlurY.SetValue((int)DirectionalBlurProperties.Angle, 90.0f);
                BlurY.SetValue((int)DirectionalBlurProperties.BorderMode, BorderMode.Soft);
                BlurY.SetValue((int)DirectionalBlurProperties.Optimization, DirectionalBlurOptimization.Speed);
                Opacity = new ID2D1Effect(dc.CreateEffect(EffectGuids.ColorMatrix));
                BlurX.SetInputEffect(0, Offset);
                BlurY.SetInputEffect(0, BlurX);
                Opacity.SetInputEffect(0, BlurY);
            }

            public DropShadowChain(ID2D1DeviceContext dc, DropShadow dropShadow) : this(dc)
            {
                Apply(dropShadow);
            }

            public ID2D1Effect Offset { get; }
            public ID2D1Effect BlurX { get; }
            public ID2D1Effect BlurY { get; }
            public ID2D1Effect Opacity { get; }

            public void Dispose()
            {
                if (Opacity != null)
                {
                    Opacity.SetInput(0, null, true);
                    Opacity.Dispose();
                }

                if (BlurY != null)
                {
                    BlurY.SetInput(0, null, true);
                    BlurY.Dispose();
                }

                if (BlurX != null)
                {
                    BlurX.SetInput(0, null, true);
                    BlurX.Dispose();
                }

                if (Offset != null)
                {
                    Offset.SetInput(0, null, true);
                    Offset.Dispose();
                }
            }

            public void Apply(DropShadow dropShadow)
            {
                Offset.SetValue((int)AffineTransform2DProperties.TransformMatrix,
                    Matrix3x2.CreateTranslation(dropShadow.OffsetX, dropShadow.OffsetY));
                BlurX.SetValue((int)DirectionalBlurProperties.StandardDeviation, dropShadow.BlurX);
                BlurY.SetValue((int)DirectionalBlurProperties.StandardDeviation, dropShadow.BlurY);
                Opacity.SetValue((int)ColorMatrixProperties.ColorMatrix, CreateShadowColorMatrix(dropShadow.Color));
            }
        }
    }
}