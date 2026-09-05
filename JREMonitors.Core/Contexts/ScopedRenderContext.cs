using System;

namespace JREMonitors.Core.Contexts
{
    /// <summary>
    ///     在父上下文之上叠加一层临时属性字典，且不复制设备级资源。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         通过 <see cref="ContextDictionary" /> 以父上下文的 <see cref="RenderContext.Properties" />
    ///         为回退层，写入仅对本作用域可见，读取先命中本层再回退到父层。
    ///     </para>
    ///     <para>
    ///         用于在局部组件树内注册仅供该子树使用的服务/缓存；
    ///         释放时仅清理本层字典，不影响父层与共享设备资源。
    ///     </para>
    /// </remarks>
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