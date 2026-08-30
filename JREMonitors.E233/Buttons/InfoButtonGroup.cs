using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Shadows;
using JREMonitors.Core.Svg;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using Vortice.Mathematics;

namespace JREMonitors.E233.Buttons
{
    public class InfoButtonGroup : Widget
    {
        private static readonly VectorButtonStyle HomeButtonStyle = new VectorButtonStyle(
            7,
            8,
            Constants.Buttons.IdleBackgroundColor,
            null,
            Constants.Buttons.PressedBackgroundColor,
            "#333".ToColor4(),
            new[]
            {
                new DropShadow
                    { OffsetX = 3, OffsetY = 3, BlurX = 1, BlurY = 1, Color = Colors.Black.MultiplyAlpha(0.3f) }
            },
            new VectorButtonInnerShadow(
                3.5f,
                new ImageInnerShadow(1.5f, new Color4(255, 255, 255, 120))
            ),
            new VectorButtonInnerShadow(
                3.5f,
                new ImageInnerShadow(1.5f, new Color4(0, 0, 0, 0.75f))
            ),
            new VectorButtonInnerShadow(
                3.5f,
                new ImageInnerShadow(1.5f, new Color4(0, 0, 0, 0.75f))
            ),
            new VectorButtonInnerShadow(
                3.5f,
                new ImageInnerShadow(1.5f, new Color4(255, 255, 255, 100))
            )
        );

        private static readonly VectorButtonStyle BrightnessButtonStyle = new VectorButtonStyle(
            7,
            7,
            Constants.Buttons.IdleBackgroundColor,
            null,
            Constants.Buttons.PressedBackgroundColor,
            "#333".ToColor4(),
            new[]
            {
                new DropShadow
                    { OffsetX = 3, OffsetY = 3, BlurX = 1, BlurY = 1, Color = Colors.Black.MultiplyAlpha(0.3f) }
            },
            new VectorButtonInnerShadow(
                3.5f,
                new ImageInnerShadow(1.5f, new Color4(255, 255, 255, 120))
            ),
            new VectorButtonInnerShadow(
                3.5f,
                new ImageInnerShadow(1.5f, new Color4(0, 0, 0, 0.75f))
            ),
            new VectorButtonInnerShadow(
                3.5f,
                new ImageInnerShadow(1.5f, new Color4(0, 0, 0, 0.75f))
            ),
            new VectorButtonInnerShadow(
                3.5f,
                new ImageInnerShadow(1.5f, new Color4(255, 255, 255, 100))
            )
        );

        public InfoButtonGroup(
            RenderContext context,
            float padding,
            Vector2 screenSize,
            bool mountHomeButton = true
        ) : base(context)
        {
            HomeButton = new VectorButton(context, new Vector2(padding),
                LayoutLength.Absolute(Constants.Buttons.SizeSmall.Width),
                LayoutLength.Absolute(Constants.Buttons.SizeSmall.Height),
                HomeButtonStyle,
                new SvgBoundsDrawer(context, new SvgDocumentProperties(Icons.Home)));
            if (mountHomeButton) AddChild(HomeButton);
            BrightnessButton = new VectorBrightnessButton(context,
                screenSize - new Vector2(1 + padding) - Constants.Buttons.SizeSmall.ToVector2(),
                LayoutLength.Absolute(Constants.Buttons.SizeSmall.Width),
                LayoutLength.Absolute(Constants.Buttons.SizeSmall.Height),
                BrightnessButtonStyle
            );
            AddChild(BrightnessButton);
        }

        public VectorButton HomeButton { get; }
        public VectorBrightnessButton BrightnessButton { get; }
        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
    }
}