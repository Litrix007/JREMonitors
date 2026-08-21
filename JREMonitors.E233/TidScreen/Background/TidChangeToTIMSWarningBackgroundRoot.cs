using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Utils.Render;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.TIMS;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.E233.TidScreen.Background
{
    public class TidChangeToTIMSWarningBackgroundRoot : Widget
    {
        private const float ImageWidth = 250;
        private const float BoundsWidth = 700;
        private const float BoundsHeight = 150;
        private const float PaddingTop = 20;
        private readonly ID2D1Bitmap _meterScreenWithSafetyLampsBitmap;
        private readonly BitmapScaleDrawer _textDrawer;
        private readonly ID2D1Bitmap _tidScreenWithSafetyLampsBitmap;

        public TidChangeToTIMSWarningBackgroundRoot(RenderContext context, string meterScreenWithSafetyLampsName,
            string tidScreenWithSafetyLampsName) :
            base(context)
        {
            _meterScreenWithSafetyLampsBitmap = context.DeviceContext.LoadBitmapFromResource(context.WicImagingFactory,
                typeof(Images), meterScreenWithSafetyLampsName);
            RegisterResource(_meterScreenWithSafetyLampsBitmap);
            _tidScreenWithSafetyLampsBitmap = context.DeviceContext.LoadBitmapFromResource(context.WicImagingFactory,
                typeof(Images), tidScreenWithSafetyLampsName);
            RegisterResource(_tidScreenWithSafetyLampsBitmap);
            _textDrawer = this.CreateTIMSTextDrawer(
                this.CreateTIMSTextLayout("この表示器をＴＩＭＳ表示器に\n切り替える場合は、左端の表示器に必ず\n保安表示灯を表示して下さい。",
                    arrangement: ContentArrangement.Near), 2, horizontalAlignment: 0.5f,
                verticalAlignment: 0.5f);
            RegisterResource(_textDrawer);
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;

        protected override void OnDraw(float totalScale)
        {
            Context.DeviceContext.DrawBitmap(_meterScreenWithSafetyLampsBitmap,
                new RectangleF(200 - ImageWidth / 2, PaddingTop, ImageWidth,
                    ImageWidth * _meterScreenWithSafetyLampsBitmap.Size.Height /
                    _meterScreenWithSafetyLampsBitmap.Size.Width),
                1, BitmapInterpolationMode.Linear, null);
            Context.DeviceContext.DrawBitmap(_tidScreenWithSafetyLampsBitmap,
                new RectangleF(600 - ImageWidth / 2, PaddingTop, ImageWidth,
                    ImageWidth * _tidScreenWithSafetyLampsBitmap.Size.Height /
                    _tidScreenWithSafetyLampsBitmap.Size.Width),
                1, BitmapInterpolationMode.Linear, null);
            Context.CommonBrush.Color = "#fdfd72".ToColor4();
            Context.DeviceContext.FillRectangle(
                new RectangleF(400 - BoundsWidth / 2, 300 - BoundsHeight / 2, BoundsWidth, BoundsHeight),
                Context.CommonBrush);
            _textDrawer.Draw(
                new RectangleF(400, 300 - BoundsHeight / 2, 0, BoundsHeight), Colors.Black);
        }
    }
}