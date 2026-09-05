using System;
using System.Numerics;
using JREMonitors.Core.Constants;
using JREMonitors.Core.Contexts;
using Vortice.Direct2D1;

namespace JREMonitors.Core.Boosters
{
    /// <summary>
    ///     <see cref="ID2D1CommandList" /> 录制器。
    /// </summary>
    public class CommandRecorder : IDisposable
    {
        private readonly RenderContext _context;
        private Matrix3x2 _recordedTransform;
        private bool _transformed;

        public CommandRecorder(RenderContext context)
        {
            _context = context;
        }

        public ID2D1CommandList CommandList { get; private set; }

        public void Dispose()
        {
            Invalidate();
        }

        public void Invalidate()
        {
            CommandList?.Dispose();
            CommandList = null;
        }

        public ID2D1CommandList Record(Action action)
        {
            if (CommandList != null) return CommandList;
            var dc = _context.DeviceContext;
            var oldTransform = dc.Transform;
            using (var oldTarget = dc.Target)
            {
                CommandList = dc.CreateCommandList();
                dc.Target = CommandList;
                dc.Transform = Matrix3x2.Identity;
                dc.Clear(null);
                action();
                CommandList.Close();
                dc.Transform = oldTransform;
                dc.Target = oldTarget;
            }

            return CommandList;
        }

        /// <summary>
        ///     带世界变换录制，让矢量几何烘焙进 Scene 坐标。
        ///     用于 <see cref="JREMonitors.Core.Shadows.ImageInnerShadowEffect" /> 的 Mask 遮罩。
        /// </summary>
        public ID2D1CommandList RecordTransformed(Action action)
        {
            var dc = _context.DeviceContext;
            var current = dc.Transform;
            if (CommandList != null && _transformed && TransformMatches(current)) return CommandList;

            Invalidate();
            using (var oldTarget = dc.Target)
            {
                CommandList = dc.CreateCommandList();
                dc.Target = CommandList;
                dc.Clear(null);
                action();
                CommandList.Close();
                dc.Target = oldTarget;
            }

            _recordedTransform = current;
            _transformed = true;
            return CommandList;
        }

        private bool TransformMatches(Matrix3x2 current)
        {
            const float epsilon = Epsilons.FloatEpsilon;
            var d = _recordedTransform - current;
            return Math.Abs(d.M11) < epsilon && Math.Abs(d.M12) < epsilon &&
                   Math.Abs(d.M21) < epsilon && Math.Abs(d.M22) < epsilon &&
                   Math.Abs(d.M31) < epsilon && Math.Abs(d.M32) < epsilon;
        }
    }
}