using System.Drawing;
using System.Linq;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Shadows;
using JREMonitors.E233.Constants;

namespace JREMonitors.E233.MeterScreen.Foreground.Base
{
    public class ShadowedMeterNum : MeterNum
    {
        private readonly DropShadow[] _dropShadows;

        public ShadowedMeterNum(RenderContext context, float x, float y, float fontSize, DropShadow[] dropShadows,
            float offsetX = 0, float offsetY = 0)
            : base(context, x, y, fontSize, offsetX, offsetY)
        {
            _dropShadows = dropShadows;
        }

        public override RectangleF SelfRelativeDirtyBounds
        {
            get
            {
                var bounds = base.SelfRelativeDirtyBounds;
                float maxSpread = 0;
                if (_dropShadows != null && _dropShadows.Length > 0)
                {
                    maxSpread = _dropShadows.Max(s => s.MaxSpread);
                }

                bounds.Inflate(maxSpread, maxSpread);
                return bounds;
            }
        }

        protected override void OnDraw(float totalScale)
        {
            Context.DropShadowProcessor.DrawWithDropShadows(_dropShadows, () => DrawText(0, 0, MonitorColors.White));
        }
    }
}