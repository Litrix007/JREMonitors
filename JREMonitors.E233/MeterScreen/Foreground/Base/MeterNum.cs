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
    public abstract class MeterNum : Widget
    {
        private readonly float _fontSize;
        private readonly float _charOffset;
        private readonly IDWriteTextFormat _format;

        protected MeterNum(RenderContext context, float x, float y, float fontSize, float offsetX = 0,
            float offsetY = 0) : base(context,
            x, y)
        {
            _fontSize = fontSize;
            _charOffset = -fontSize * 3 / 5;
            Num = CreatePropertySlot<int>(DirtyType.Visual);
            OffsetX = CreatePropertySlot(DirtyType.Visual, offsetX);
            OffsetY = CreatePropertySlot(DirtyType.Visual, offsetY);
            _format = context.FontManager.GetOrCreateFormat(Fonts.MyriadProFamily, _fontSize,
                fontWeight: FontWeight.Bold);
            _format.TextAlignment = TextAlignment.Trailing;
        }

        public PropertySlot<int> Num { get; }
        public PropertySlot<float> OffsetX { get; }
        public PropertySlot<float> OffsetY { get; }

        private string Text => Num.Value.ToString();

        public override RectangleF SelfRelativeDirtyBounds
        {
            get
            {
                var width = MathHelper.Abs(_charOffset) * Text.Length + 20;
                return new RectangleF(OffsetX + -width, OffsetY + -_fontSize + _fontSize / 5, width, _fontSize);
            }
        }

        public override float? RefreshSpeed => IsFirstUpdate ? RefreshSpeeds.Fast : (float?)null;

        protected abstract override void OnDraw(float totalScale);

        protected void DrawText(float offsetX, float offsetY, Color4 color)
        {
            var text = Text;
            for (var i = text.Length - 1; i >= 0; i--)
            {
                var x = _charOffset * (text.Length - 1 - i);
                Context.CommonBrush.Color = color;
                Context.DeviceContext.DrawDynamicText(Context.DwFactory, text.Substring(i, 1),
                    offsetX + OffsetX + x, offsetY + OffsetY, _format,
                    Context.CommonBrush,
                    1,
                    1,
                    10);
            }
        }
    }
}