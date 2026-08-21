using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Services.Render;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS
{
    public class TIMSCommonBackground : Widget
    {
        private readonly BitmapScaleDrawer _textDrawer;

        public TIMSCommonBackground(RenderContext context) : base(context)
        {
            _textDrawer = this.CreateTIMSTextDrawer("\u3000シウコク\u3000ゾョウホウ",
                format: Context.FontManager.GetOrCreateFormat(Fonts.MsGothicFamily, 10));
            RegisterResource(_textDrawer);
        }

        public override RectangleF SelfRelativeDirtyBounds => new RectangleF(PointF.Empty, ScreenSizes.TIMSScreenSize);

        public override float? RefreshSpeed => IsFirstUpdate ? RefreshSpeeds.Fast : (float?)null;

        protected override void OnDraw(float totalScale)
        {
            Context.GetBakerCache().GetOrCreateBaker<TIMSCommonBackground, int>(Context, 0)
                .BakeAndDraw(SelfRelativeDirtyBounds, Draw);
        }

        protected virtual void Draw()
        {
            Context.CommonBrush.Color = MonitorColors.TIMSScreenBackground;
            Context.DeviceContext.FillRectangle(new RectangleF(PointF.Empty, ScreenSizes.TIMSScreenSize),
                Context.CommonBrush);
            Context.CommonBrush.Color = Colors.Black;
            const float height = 64;
            Context.DeviceContext.FillRectangle(new RectangleF(0, 0, 744, height), Context.CommonBrush);
            Context.DeviceContext.FillRectangle(new RectangleF(0, 500, 800, 100), Context.CommonBrush);
            Context.CommonBrush.Color = MonitorColors.MeterScreenBackground;
            const float rectHeight = 20;
            for (var i = 0; i < 2; i++)
                Context.DeviceContext.FillRectangle(new RectangleF(507, 6 + i * (rectHeight + 6), 175, rectHeight),
                    Context.CommonBrush);
            Context.DeviceContext.DrawLine(new Vector2(223, 0), new Vector2(223, height), Context.CommonBrush, 3);
            Context.DeviceContext.DrawLine(new Vector2(389, 0), new Vector2(389, height), Context.CommonBrush, 3);
            for (var i = 0; i < 3; i++)
                Context.DeviceContext.FillRectangle(new RectangleF(413, 506 + i * (rectHeight + 6), 285, rectHeight),
                    Context.CommonBrush);
            _textDrawer.Draw(new RectangleF(0, 0, 0, 0), "#0f0".ToColor4());
        }
    }
}