namespace JREMonitors.Core.Layouts
{
    public interface IExpandableElement<out TState>
    {
        TState ExpansionState { get; }

        /// <summary>
        ///     最大期望外扩极限
        /// </summary>
        float MaxDynamicExpansion { get; }

        // 四个方向上的静态外延量
        float StaticExtensionLeft { get; }
        float StaticExtensionRight { get; }
        float StaticExtensionTop { get; }
        float StaticExtensionBottom { get; }

        void SetDynamicExpansionLimits(float left, float right, float top, float bottom);
    }
}