using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Svg;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;

namespace JREMonitors.E233.Buttons
{
    public class VectorBrightnessButton : Widget
    {
        private static readonly float[] StepValues = { 1f, 0.9f, 0.8f, 0.7f, 0.5f };
        private static readonly string[] ActivePathIds = { "4_4", "3_4", "2_4", "1_4", null };

        public VectorBrightnessButton(RenderContext context, Vector2 pos, LayoutLength width, LayoutLength height,
            VectorButtonStyle style)
            : base(context, pos.X, pos.Y)
        {
            AddChild(new CycleSvgIconSwitchButton(context, Vector2.Zero, width, height, style,
                new SvgDocumentProperties(Icons.Brightness),
                StepValues,
                ActivePathIds,
                () => Context.DisplayController.Brightness,
                val => Context.DisplayController.Brightness = val
            ));
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
    }
}