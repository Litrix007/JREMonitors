using System;

namespace JREMonitors.Core.Reactive
{
    /// <summary>
    ///     惰性响应式资源槽，按依赖跟踪按需创建资源，依赖失效时重建并释放旧 <see cref="IDisposable" /> 实例。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="Update" /> 在提交环节运行内部效果：效果体读到的信号即建立依赖，检测失效后先
    ///         <c>Dispose</c> 旧值再执行 <c>factory</c> 重建；重建后返回 <see cref="Value" /> 供渲染使用。
    ///     </para>
    ///     <para>
    ///         用法：Widget 经 <c>WatchResource(factory)</c> 创建并随组件释放；适用于随状态变化
    ///         需要重建的渲染资源（依赖变化即自动回收旧资源并重建）。
    ///     </para>
    /// </remarks>
    public class ResourceSlot<T> : IResourceSlot where T : class, IDisposable
    {
        private readonly ReactiveEffect _effect;

        public ResourceSlot(Func<T> factory, bool enableDynamicUnbinding = true)
        {
            _effect = new ReactiveEffect(() =>
            {
                var oldValue = Value;
                Value = null;
                oldValue?.Dispose();
                Value = factory();
            }, enableDynamicUnbinding);
        }

        public T Value { get; private set; }

        public event Action OnInvalidated
        {
            add => _effect.OnInvalidated += value;
            remove => _effect.OnInvalidated -= value;
        }

        public bool Update(bool force = false)
        {
            return _effect.Run(force);
        }

        public void Dispose()
        {
            var oldValue = Value;
            Value = null;
            oldValue?.Dispose();
            _effect.Dispose();
        }
    }
}