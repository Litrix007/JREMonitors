using System.Numerics;
using System.Runtime.InteropServices;
using JREMonitors.Core.Shadows;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.Core.Lighting
{
    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    public struct AdaptiveScreenLightingConstants
    {
        public Matrix4x4 HTotal;
        public Vector2 BrightExtent;
        public Vector2 ShadowExtent;
        public Vector4 CornerRect;
        public float CornerRadius;
        public float Mode;
        public float AmbientFactor;
        public float CompressThreshold;
        public float LinearBrightness;
        public float GlassReflectanceLight;
        public float GlassReflectanceDark;
        public float Brightness;
        public Color3 Leakage;
        private float _pad2;
        public Color3 GlareColor;
        private float _pad3;
    }

    public class AdaptiveScreenLightingEffect : ID2D1Effect
    {
        private readonly Implementation _impl;

        public AdaptiveScreenLightingEffect(ID2D1DeviceContext context) : base(
            context.CreateEffect(typeof(Implementation).GUID))
        {
            _impl = Implementation.LastCreatedInstance;
            Implementation.LastCreatedInstance = null;
        }

        public void UpdateConstants(AdaptiveScreenLightingConstants constants)
        {
            _impl?.UpdateConstants(constants);
        }

        public static void Register(ID2D1Factory1 factory)
        {
            factory.RegisterEffect<Implementation>();
        }

        public static void Unregister(ID2D1Factory1 factory)
        {
            factory.UnregisterEffect(typeof(Implementation).GUID);
        }

        [CustomEffect(1)]
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

            public override void Initialize(ID2D1EffectContext effectContext,
                ID2D1TransformGraph transformGraph)
            {
                base.Initialize(effectContext, transformGraph);
                _shaderTransform = new ShaderTransform(effectContext,
                    "JREMonitors.Core.Lighting.AdaptiveScreenLighting.cso", 1, PixelOptions.None,
                    typeof(AdaptiveScreenLightingEffect));
                transformGraph.SetSingleTransformNode(_shaderTransform);
            }

            public void UpdateConstants(AdaptiveScreenLightingConstants c)
            {
                _shaderTransform?.UpdateConstants(c);
            }
        }
    }
}