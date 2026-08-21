namespace JREMonitors.Core.Layouts
{
    public interface ILayoutable
    {
        /// <summary>
        ///     期望的宽度
        /// </summary>
        LayoutLength PreferredWidth { get; }

        /// <summary>
        ///     期望的高度
        /// </summary>
        LayoutLength PreferredHeight { get; }

        /// <summary>
        ///     总的外边距宽
        /// </summary>
        float MarginWidth { get; }

        /// <summary>
        ///     总的外边距高
        /// </summary>
        float MarginHeight { get; }

        bool IncludeInTotalMajorDimensionSizeWhenVisible { get; }
        bool IncludeInTotalMajorDimensionSizeWhenHidden { get; }

        bool SkipArrangeWhenHidden { get; }

        void SetLayoutSize(float width, float height);
    }
}