using System;
using System.Numerics;
using JREMonitors.Core.Contexts;
using Vortice.Direct2D1;

namespace JREMonitors.Core.Boosters
{
    public class CommandRecorder : IDisposable
    {
        private readonly RenderContext _context;

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
    }
}