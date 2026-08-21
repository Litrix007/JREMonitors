using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using JREMonitors.Core.Utils.Render;
using Vortice.Direct2D1;

namespace JREMonitors.Core.Shadows
{
    public class InnerShadowEffectChain : IDisposable
    {
        private readonly List<ID2D1Effect> _blurEffects = new List<ID2D1Effect>();
        private readonly ID2D1DeviceContext _dc;
        private readonly List<ID2D1Effect> _maskTransformEffects = new List<ID2D1Effect>();
        private readonly List<IInnerShadowEffectNode> _nodes = new List<IInnerShadowEffectNode>();

        public InnerShadowEffectChain(ID2D1DeviceContext dc)
        {
            _dc = dc;
        }

        public void Dispose()
        {
            foreach (var node in _nodes)
                if (node != null)
                {
                    node.ClearInputs();
                    node.Dispose();
                }

            foreach (var blur in _blurEffects)
                if (blur != null)
                {
                    blur.SetInput(0, null, true);
                    blur.Dispose();
                }

            foreach (var trans in _maskTransformEffects)
                if (trans != null)
                {
                    trans.SetInput(0, null, true);
                    trans.Dispose();
                }

            _nodes.Clear();
            _blurEffects.Clear();
            _maskTransformEffects.Clear();
        }

        public void AddOffsetShadows(IReadOnlyList<OffsetInnerShadow> shadows)
        {
            if (shadows == null || shadows.Count == 0) return;

            foreach (var s in shadows)
            {
                var blurEffect = new ID2D1Effect(_dc.CreateEffect(EffectGuids.GaussianBlur));
                _blurEffects.Add(blurEffect);
                _maskTransformEffects.Add(null);
                var effect = new OffsetInnerShadowEffect(_dc);
                effect.UpdateConstants(s);
                _nodes.Add(effect);
            }
        }

        public ImageInnerShadowEffect AddImageShadow(ImageInnerShadow? shadow = null)
        {
            var blurEffect = new ID2D1Effect(_dc.CreateEffect(EffectGuids.GaussianBlur));
            _blurEffects.Add(blurEffect);
            var transformEffect = new ID2D1Effect(_dc.CreateEffect(EffectGuids.AffineTransform2D));
            _maskTransformEffects.Add(transformEffect);
            var effect = new ImageInnerShadowEffect(_dc);
            if (shadow.HasValue) effect.Update(shadow.Value);

            _nodes.Add(effect);
            return effect;
        }

        public void ApplyAndDraw(ID2D1Image baseGeometry, float worldScale, float totalScale,
            Matrix3x2 currentTransform)
        {
            if (_nodes.Count == 0)
            {
                _dc.DrawImage(baseGeometry);
                return;
            }

            ID2D1Effect prevNodeEffect = null;
            for (var i = 0; i < _nodes.Count; i++)
            {
                var node = _nodes[i];
                if (!node.IsEnabled) continue;
                var blurEffect = _blurEffects[i];
                var blur = node.Blur;
                var transformEffect = _maskTransformEffects[i];

                blurEffect.SetValue((int)GaussianBlurProperties.StandardDeviation, blur * worldScale);
                node.SetScale(worldScale, totalScale);
                if (prevNodeEffect == null)
                    node.SetAccumulatedInput(baseGeometry);
                else
                    node.SetAccumulatedInputEffect(prevNodeEffect);

                node.SetBaseGeometry(baseGeometry);
                node.SetBlurredMaskInputEffect(blurEffect);

                switch (node)
                {
                    case OffsetInnerShadowEffect _:
                        blurEffect.SetInput(0, baseGeometry, true);
                        break;

                    case ImageInnerShadowEffect imageNode:
                        if (transformEffect != null && imageNode.RawShadowMask != null)
                        {
                            transformEffect.SetValue((int)AffineTransform2DProperties.TransformMatrix,
                                currentTransform);
                            transformEffect.SetInput(0, imageNode.RawShadowMask, true);
                            blurEffect.SetInputEffect(0, transformEffect);
                        }
                        else if (imageNode.RawShadowMask != null)
                        {
                            blurEffect.SetInput(0, imageNode.RawShadowMask, true);
                        }

                        break;
                }

                prevNodeEffect = node as ID2D1Effect;
            }

            if (prevNodeEffect != null)
                _dc.DrawImage(prevNodeEffect);
            else
                _dc.DrawImage(baseGeometry);

            foreach (var node in _nodes) node.ClearInputs();
            foreach (var blur in _blurEffects) blur.SetInput(0, null, true);
            foreach (var trans in _maskTransformEffects) trans?.SetInput(0, null, true);
        }
    }

    public class InnerShadowProcessor : IDisposable
    {
        private readonly ID2D1DeviceContext _dc;

        private readonly Dictionary<IReadOnlyList<OffsetInnerShadow>, InnerShadowEffectChain> _offsetChains =
            new Dictionary<IReadOnlyList<OffsetInnerShadow>, InnerShadowEffectChain>(OffsetInnerShadowListComparer
                .Instance);

        private bool _disposed;

        public InnerShadowProcessor(ID2D1DeviceContext dc)
        {
            _dc = dc;
        }

        public void Dispose()
        {
            if (_disposed) return;
            foreach (var chain in _offsetChains.Values) chain.Dispose();
            _offsetChains.Clear();
            _disposed = true;
        }

        public static void Register(ID2D1Factory1 factory)
        {
            OffsetInnerShadowEffect.Register(factory);
            ImageInnerShadowEffect.Register(factory);
        }

        public static void Unregister(ID2D1Factory1 factory)
        {
            OffsetInnerShadowEffect.Unregister(factory);
            ImageInnerShadowEffect.Unregister(factory);
        }

        public void DrawWithInnerShadows(InnerShadowEffectChain chain, Action drawingAction)
        {
            if (_disposed || drawingAction == null || chain == null) return;
            using (var commandList = _dc.CreateCommandList())
            {
                var sw = Stopwatch.StartNew();
                using (var oldTarget = _dc.Target)
                {
                    _dc.Target = commandList;
                    drawingAction.Invoke();
                    _dc.Target = oldTarget;
                }

                sw.Stop();
                commandList.Close();
                _dc.GetDpi(out var dpiX, out _);
                var dpiScale = dpiX / 96f;
                _dc.GetWorldScale(out var scaleX, out var scaleY);
                var worldScale = Math.Max(scaleX, scaleY);
                var totalScale = dpiScale * worldScale;
                var oldTransform = _dc.Transform;
                _dc.Transform = Matrix3x2.Identity;
                chain.ApplyAndDraw(commandList, worldScale, totalScale, oldTransform);
                _dc.Transform = oldTransform;
            }
        }

        public void DrawWithInnerShadows(IReadOnlyList<OffsetInnerShadow> shadows,
            Action drawingAction)
        {
            if (_disposed || drawingAction == null) return;
            if (shadows == null || shadows.Count == 0)
            {
                drawingAction.Invoke();
                return;
            }

            if (!_offsetChains.TryGetValue(shadows, out var chain))
            {
                chain = new InnerShadowEffectChain(_dc);
                chain.AddOffsetShadows(shadows);
                _offsetChains[shadows] = chain;
            }

            DrawWithInnerShadows(chain, drawingAction);
        }
    }
}