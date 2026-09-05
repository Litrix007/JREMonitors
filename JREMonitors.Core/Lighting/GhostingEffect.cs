using System.Runtime.InteropServices;
using JREMonitors.Core.Shadows;
using Vortice.Direct2D1;

namespace JREMonitors.Core.Lighting
{
    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    public struct GhostingConstants
    {
        public float DecayAlpha;
        public float ConvergeBand;
        private float _pad0;
        private float _pad1;
    }

    /// <summary>
    ///     Ghosting 状态更新特效：纯 EMA 衰减 + 带内收敛收缩。
    ///     输入 0 = 当前帧（ActualBitmap），输入 1 = 上一帧余影（StateOld）；
    ///     输出写入 ping-pong 的 StateNew（8bit，D3D11-backed）。
    ///     收敛带（半径 ≥ 0.5/(1−α)/255 覆盖 8bit round 停滞区）内每帧距离减半并扣量子步，
    ///     有限步内精确收敛到当前内容。。
    /// </summary>
    public class GhostingEffect : ID2D1Effect
    {
        private readonly Implementation _impl;

        public GhostingEffect(ID2D1DeviceContext context) : base(
            context.CreateEffect(typeof(Implementation).GUID))
        {
            _impl = Implementation.LastCreatedInstance;
            Implementation.LastCreatedInstance = null;
        }

        public void UpdateConstants(float decayAlpha, float convergeBand)
        {
            _impl?.UpdateConstants(new GhostingConstants
            {
                DecayAlpha = decayAlpha,
                ConvergeBand = convergeBand
            });
        }

        public static void Register(ID2D1Factory1 factory)
        {
            factory.RegisterEffect<Implementation>();
        }

        public static void Unregister(ID2D1Factory1 factory)
        {
            factory.UnregisterEffect(typeof(Implementation).GUID);
        }

        [CustomEffect(2)]
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
                    "JREMonitors.Core.Lighting.Ghosting.cso", 2, PixelOptions.None,
                    typeof(GhostingEffect));
                transformGraph.SetSingleTransformNode(_shaderTransform);
            }

            public void UpdateConstants(GhostingConstants c)
            {
                _shaderTransform?.UpdateConstants(c);
            }
        }
    }
}