using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using JREMonitors.Core.Utils;
using SharpGen.Runtime;
using Vortice;
using Vortice.Direct2D1;

namespace JREMonitors.Core.Shadows
{
    public interface IInnerShadowEffectNode : IDisposable
    {
        float Blur { get; }
        bool IsEnabled { get; set; }
        void SetScale(float worldScale, float totalScale);
        void SetAccumulatedInput(ID2D1Image accum);
        void SetAccumulatedInputEffect(ID2D1Effect accumEffect);
        void SetBaseGeometry(ID2D1Image baseGeometry);
        void SetBlurredMaskInputEffect(ID2D1Effect blurredMaskEffect);
        void SetBlurredMaskInput(ID2D1Image blurredMask);
        void ClearInputs();
    }

    public class OffsetInnerShadowEffect : ID2D1Effect, IInnerShadowEffectNode
    {
        private readonly Implementation _impl;
        private float _maxSpread;
        private OffsetInnerShadow _shadow;

        public OffsetInnerShadowEffect(ID2D1DeviceContext context) : base(
            context.CreateEffect(typeof(Implementation).GUID))
        {
            _impl = Implementation.LastCreatedInstance;
            Implementation.LastCreatedInstance = null;
        }

        public float Blur => _shadow.Blur;
        public bool IsEnabled { get; set; } = true;

        public void SetScale(float worldScale, float totalScale)
        {
            var constants = new OffsetShadowConstants(_shadow, worldScale);
            _impl.UpdateConstants(constants);
            var expand = (int)Math.Ceiling(_maxSpread * totalScale) + 2;
            _impl.SetExpand(expand);
        }

        public void SetAccumulatedInput(ID2D1Image accum)
        {
            SetInput(0, accum, true);
        }

        public void SetAccumulatedInputEffect(ID2D1Effect accumEffect)
        {
            SetInputEffect(0, accumEffect);
        }

        public void SetBaseGeometry(ID2D1Image baseGeo)
        {
            SetInput(1, baseGeo, true);
        }

        public void SetBlurredMaskInputEffect(ID2D1Effect blurredMaskEffect)
        {
            SetInputEffect(2, blurredMaskEffect);
        }

        public void SetBlurredMaskInput(ID2D1Image blurredMask)
        {
            SetInput(2, blurredMask, true);
        }

        public void ClearInputs()
        {
            SetInput(0, null, true);
            SetInput(1, null, true);
            SetInput(2, null, true);
        }

        public static void Register(ID2D1Factory1 factory)
        {
            factory.RegisterEffect<Implementation>();
        }

        public static void Unregister(ID2D1Factory1 factory)
        {
            factory.UnregisterEffect(typeof(Implementation).GUID);
        }

        public void UpdateConstants(OffsetInnerShadow shadow)
        {
            _shadow = shadow;
            _maxSpread = shadow.MaxSpread;
        }

        [CustomEffect(3)]
        private class Implementation : CustomEffectBase
        {
            public static Implementation LastCreatedInstance;
            private ShaderTransform _shaderTransform;

            public Implementation()
            {
                LastCreatedInstance = this;
            }

            protected override void DisposeCore(bool disposing)
            {
                if (disposing) _shaderTransform?.Dispose();
                base.DisposeCore(disposing);
            }

            public override void Initialize(ID2D1EffectContext effectContext, ID2D1TransformGraph transformGraph)
            {
                base.Initialize(effectContext, transformGraph);
                _shaderTransform =
                    new ShaderTransform(effectContext, "JREMonitors.Core.Shadows.OffsetInnerShadow.cso", 3,
                        PixelOptions.None, typeof(OffsetInnerShadowEffect));
                transformGraph.SetSingleTransformNode(_shaderTransform);
            }

            public void UpdateConstants(OffsetShadowConstants c)
            {
                _shaderTransform?.UpdateConstants(c);
            }

            public void SetExpand(int e)
            {
                _shaderTransform?.SetExpand(e);
            }
        }
    }

    public class ImageInnerShadowEffect : ID2D1Effect, IInnerShadowEffectNode
    {
        private readonly Implementation _impl;

        public ImageInnerShadowEffect(ID2D1DeviceContext context) : base(
            context.CreateEffect(typeof(Implementation).GUID))
        {
            _impl = Implementation.LastCreatedInstance;
            Implementation.LastCreatedInstance = null;
        }

        public ID2D1Image RawShadowMask { get; private set; }

        public float Blur { get; private set; }
        public bool IsEnabled { get; set; } = true;

        public void SetScale(float worldScale, float totalScale)
        {
            var expand = (int)Math.Ceiling(Blur * 3.0f * totalScale) + 2;
            _impl.SetExpand(expand);
        }

        public void SetAccumulatedInput(ID2D1Image accum)
        {
            SetInput(0, accum, true);
        }

        public void SetAccumulatedInputEffect(ID2D1Effect accumEffect)
        {
            SetInputEffect(0, accumEffect);
        }

        public void SetBaseGeometry(ID2D1Image baseGeo)
        {
            SetInput(1, baseGeo, true);
        }

        public void SetBlurredMaskInputEffect(ID2D1Effect blurredMaskEffect)
        {
            SetInputEffect(2, blurredMaskEffect);
        }

        public void SetBlurredMaskInput(ID2D1Image blurredMask)
        {
            SetInput(2, blurredMask, true);
        }

        public void ClearInputs()
        {
            SetInput(0, null, true);
            SetInput(1, null, true);
            SetInput(2, null, true);
            RawShadowMask = null;
        }

        public static void Register(ID2D1Factory1 factory)
        {
            factory.RegisterEffect<Implementation>();
        }

        public static void Unregister(ID2D1Factory1 factory)
        {
            factory.UnregisterEffect(typeof(Implementation).GUID);
        }

        /// <summary>
        ///     更新阴影遮罩。
        ///     要求遮罩处于 Scene 坐标而非相对坐标，请使用<see cref="JREMonitors.Core.Boosters.CommandRecorder.RecordTransformed" />。
        /// </summary>
        public void Update(ImageInnerShadow shadow, ID2D1Image shadowMask)
        {
            Update(shadow);
            Update(shadowMask);
        }

        public void Update(ImageInnerShadow shadow)
        {
            Blur = shadow.Blur;
            var constants = new ImageShadowConstants
            {
                Blur = shadow.Blur,
                Choke = shadow.Choke,
                Color = shadow.Color
            };
            _impl.UpdateConstants(constants);
        }

        /// <summary>
        ///     更新阴影遮罩。
        ///     要求遮罩处于 Scene 坐标而非相对坐标，请使用<see cref="JREMonitors.Core.Boosters.CommandRecorder.RecordTransformed" />。
        /// </summary>
        public void Update(ID2D1Image imageMask)
        {
            RawShadowMask = imageMask;
        }

        [CustomEffect(3)]
        private class Implementation : CustomEffectBase
        {
            public static Implementation LastCreatedInstance;
            private ShaderTransform _shaderTransform;

            public Implementation()
            {
                LastCreatedInstance = this;
            }

            protected override void DisposeCore(bool disposing)
            {
                if (disposing) _shaderTransform?.Dispose();
                base.DisposeCore(disposing);
            }

            public override void Initialize(ID2D1EffectContext effectContext, ID2D1TransformGraph transformGraph)
            {
                base.Initialize(effectContext, transformGraph);
                _shaderTransform =
                    new ShaderTransform(effectContext, "JREMonitors.Core.Shadows.ImageInnerShadow.cso", 3,
                        PixelOptions.TrivialSampling, typeof(ImageInnerShadowEffect));
                transformGraph.SetSingleTransformNode(_shaderTransform);
            }

            public void UpdateConstants(ImageShadowConstants c)
            {
                _shaderTransform?.UpdateConstants(c);
            }

            public void SetExpand(int e)
            {
                _shaderTransform?.SetExpand(e);
            }
        }
    }

    internal class ShaderTransform : CallbackBase, ID2D1DrawTransform
    {
        private static readonly ConcurrentDictionary<string, Guid> ShaderGuids =
            new ConcurrentDictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        private readonly int _inputCount;
        private readonly PixelOptions _pixelOptions;
        private readonly Guid _shaderId;
        private ID2D1DrawInfo _drawInfo;
        private int _expand;

        public ShaderTransform(
            ID2D1EffectContext effectContext,
            string resourcePath,
            int inputCount,
            PixelOptions pixelOptions,
            Type resourceAssemblyType)
        {
            _shaderId = GetOrCreateShaderGuid(resourcePath);
            _inputCount = inputCount;
            _pixelOptions = pixelOptions;
            var shaderBytes = ResourceHelper.GetResourceBytes(resourceAssemblyType, resourcePath);
            effectContext.LoadPixelShader(_shaderId, shaderBytes, shaderBytes.Length);
        }

        public int GetInputCount()
        {
            return _inputCount;
        }

        public void MapInputRectsToOutputRect(RawRect[] inputRects, RawRect[] inputOpaqueSubRects,
            out RawRect outputRect, out RawRect outputOpaqueSubRect)
        {
            outputRect = inputRects[0];
            outputOpaqueSubRect = new RawRect(0, 0, 0, 0);
        }

        public void MapOutputRectToInputRects(RawRect outputRect, RawRect[] inputRects)
        {
            inputRects[0] = outputRect;
            var expandedRect = new RawRect(outputRect.Left - _expand, outputRect.Top - _expand,
                outputRect.Right + _expand, outputRect.Bottom + _expand);
            for (var i = 1; i < inputRects.Length; i++) inputRects[i] = expandedRect;
        }

        public RawRect MapInvalidRect(int inputIndex, RawRect invalidInputRect)
        {
            return invalidInputRect;
        }

        public void SetDrawInfo(ID2D1DrawInfo drawInfo)
        {
            drawInfo.SetPixelShader(_shaderId, _pixelOptions);
            _drawInfo = drawInfo;
        }

        private static Guid GetOrCreateShaderGuid(string resourcePath)
        {
            return ShaderGuids.GetOrAdd(resourcePath, path =>
            {
                using (var md5 = MD5.Create())
                {
                    var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(path));
                    return new Guid(hash);
                }
            });
        }

        public void UpdateConstants<T>(T constants) where T : unmanaged
        {
            _drawInfo?.SetPixelShaderConstantBuffer(constants);
        }

        public void SetExpand(int expand)
        {
            _expand = expand;
        }

        protected override void DisposeCore(bool disposing)
        {
            _drawInfo = null;
            base.DisposeCore(disposing);
        }
    }
}