using System;

namespace JREMonitors.Core.Contexts
{
    public class ScopedRenderContext : RenderContext, IDisposable
    {
        public ScopedRenderContext(RenderContext parent)
            : base(parent, new ContextDictionary(parent.Properties))
        {
        }

        public void Dispose()
        {
            if (Properties is IDisposable disposable) disposable.Dispose();
        }
    }
}