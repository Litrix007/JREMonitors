using System.Drawing;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Utils.Render;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using Vortice.Direct2D1;

namespace JREMonitors.E233.TIMS.S00AA
{
    public class S00AABackgroundRoot : Widget
    {
        private readonly Baker _baker;
        private readonly ID2D1Bitmap _jreLogoBitmap;

        public S00AABackgroundRoot(RenderContext context) : base(context)
        {
            AddChild(new TIMSScreenTitle(context, ScreenIds.S00AA, string.Empty));
            _jreLogoBitmap =
                context.DeviceContext.LoadBitmapFromResource(context.WicImagingFactory, typeof(Images), Images.JRELogo);
            RegisterResource(_jreLogoBitmap);
            _baker = new Baker(context, BakerPrescaleMode.AutoCubic);
            RegisterResource(_baker);
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;

        protected override void OnDraw(float totalScale)
        {
            const float logoWidth = 60;
            const float logoSpacing = 20;
            const float rowSpacing = 40;
            using (var titleMeasureResult =
                   Context.DwFactory.MeasureText("E233系 TIMS", Context.TIMS().Format18,
                       new[] { new TextSpacing { Index = 0, Length = 9, TrailingSpacing = 3 } }))
            using (var prompt1MeasureResult =
                   Context.DwFactory.MeasureText("ただいま準備中です。", Context.TIMS().Format18))
            using (var prompt2MeasureResult =
                   Context.DwFactory.MeasureText("しばらくお待ち下さい。", Context.TIMS().Format18))
            {
                var logoHeight = _jreLogoBitmap.Size.Height * logoWidth / _jreLogoBitmap.Size.Width;
                var titleBounds = new RectangleF(PointF.Empty, titleMeasureResult.Size).SnapToPixels();
                var prompt1Bounds = new RectangleF(PointF.Empty, prompt1MeasureResult.Size).SnapToPixels();
                var prompt2Bounds = new RectangleF(PointF.Empty, prompt2MeasureResult.Size).SnapToPixels();
                var totalHeight = titleBounds.Height * 4 + prompt1Bounds.Height * 2 + prompt2Bounds.Height * 2 +
                                  rowSpacing * 2;
                var row1Width = logoWidth + logoSpacing + titleBounds.Width * 3;
                var y = (600 - totalHeight) / 2;
                Context.DeviceContext.DrawBitmap(_jreLogoBitmap,
                    new RectangleF(400 - row1Width / 2, y + (titleBounds.Height * 4 - logoHeight) / 2, logoWidth,
                        logoHeight), 1f, BitmapInterpolationMode.Linear, null);
                Context.CommonBrush.Color = MonitorColors.White;
                _baker.BakeAndDraw(titleBounds,
                    () => { Context.DeviceContext.DrawDynamicText(titleMeasureResult, 0, 0, Context.CommonBrush); },
                    new RectangleF(400 - row1Width / 2 + logoWidth + logoSpacing, y, titleBounds.Width * 3,
                        titleBounds.Height * 4));
                y += rowSpacing + titleBounds.Height * 4;
                _baker.Refresh();
                Context.CommonBrush.Color = "#7bdef7".ToColor4();
                _baker.BakeAndDraw(prompt1Bounds,
                    () => { Context.DeviceContext.DrawDynamicText(prompt1MeasureResult, 0, 0, Context.CommonBrush); },
                    new RectangleF(400 - row1Width / 2, y, prompt1Bounds.Width * 2,
                        prompt1Bounds.Height * 2)
                );
                y += rowSpacing + prompt1Bounds.Height * 2;
                _baker.Refresh();
                _baker.BakeAndDraw(prompt2Bounds,
                    () => { Context.DeviceContext.DrawDynamicText(prompt2MeasureResult, 0, 0, Context.CommonBrush); },
                    new RectangleF(400 + row1Width / 2 - prompt2Bounds.Width * 2 + 13, y,
                        prompt2Bounds.Width * 2, prompt2Bounds.Height * 2)
                );
            }
        }
    }
}