using JREMonitors.Core.Contexts;
using JREMonitors.E233.Constants;
using Vortice.Mathematics;

namespace JREMonitors.E233.MeterScreen.Foreground.Base
{
    public class MeterSpeedNum : MeterNum
    {
        private const float ShadowOffset = 6;
        private static readonly Color4 ShadowColor = new Color4(0, 0, 0, 128);

        public MeterSpeedNum(RenderContext context, float x, float y)
            : base(context, x, y, fontSize: 105)
        {
        }

        protected override void OnDraw(float totalScale)
        {
            DrawText(ShadowOffset, ShadowOffset, ShadowColor);
            DrawText(0, 0, MonitorColors.White);
        }
    }
}