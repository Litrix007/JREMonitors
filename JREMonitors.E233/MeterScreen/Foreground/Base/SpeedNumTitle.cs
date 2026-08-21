using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Utils.Render;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using Vortice.DirectWrite;
using Vortice.Mathematics;

namespace JREMonitors.E233.MeterScreen.Foreground.Base
{
    public class SpeedNumTitle : Widget
    {
        private const float FontSize = 105;
        private const float CharOffset = -FontSize * 3 / 5;
        private const float ShadowOffset = 6;
        private static readonly Color4 ShadowColor = new Color4(0, 0, 0, 128);
        private readonly IDWriteTextFormat _format;
        private readonly PropertySlot<int> _speed;


        public SpeedNumTitle(RenderContext context, float x, float y) : base(context, x, y)
        {
            _speed = CreatePropertySlot<int>(DirtyType.Visual);
            _format = context.FontManager.GetOrCreateFormat(Fonts.MyriadProFamily, FontSize,
                fontWeight: FontWeight.Bold);
            _format.TextAlignment = TextAlignment.Trailing;
        }

        public int Speed
        {
            get => _speed;
            set => _speed.Value = value;
        }

        private string Text => Speed.ToString();

        public override RectangleF SelfRelativeDirtyBounds
        {
            get
            {
                var width = MathHelper.Abs(CharOffset) * Text.Length + 20;
                return new RectangleF(-width, -FontSize + FontSize / 5, width, FontSize);
            }
        }

        public override float? RefreshSpeed => IsFirstUpdate ? RefreshSpeeds.Slow : (float?)null;

        protected override void OnDraw(float totalScale)
        {
            var text = Text;
            for (var i = text.Length - 1; i >= 0; i--)
            {
                var offsetX = CharOffset * (text.Length - 1 - i);
                Context.CommonBrush.Color = ShadowColor;
                Context.DeviceContext.DrawDynamicText(Context.DwFactory, text.Substring(i, 1),
                    0 + offsetX + ShadowOffset, 0 + ShadowOffset, _format,
                    Context.CommonBrush,
                    1, 1,
                    10);
                Context.CommonBrush.Color = MonitorColors.White;
                Context.DeviceContext.DrawDynamicText(Context.DwFactory, text.Substring(i, 1),
                    0 + offsetX, 0, _format,
                    Context.CommonBrush,
                    1,
                    1,
                    10);
            }
        }
    }
}