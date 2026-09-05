using System;
using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Services.Render;
using JREMonitors.Core.State;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Utils.Render;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.TIMS;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.E233.TidScreen.Background
{
    public class TidChangeToTIMSWarningBackgroundRoot : Widget<TidChangeToTIMSWarningBackgroundRootViewModel>
    {
        private const float ImageWidth = 250;
        private const float BoundsWidth = 700;
        private const float BoundsHeight = 150;
        private const float PaddingTop = 20;
        private static readonly PropertyKey ImageCacheKey = new PropertyKey("TidChangeToTIMSWarningImageCache");
        private readonly ID2D1Bitmap _meterScreenWithSafetyLampsBitmap;
        private readonly ID2D1Bitmap _meterScreenWithSafetyLampsWithoutTascBitmap;
        private readonly BitmapScaleDrawer _textDrawer;
        private readonly ID2D1Bitmap _tidScreenWithSafetyLampsBitmap;
        private readonly ID2D1Bitmap _tidScreenWithSafetyLampsWithoutTascBitmap;

        public TidChangeToTIMSWarningBackgroundRoot(RenderContext context, string meterScreenWithSafetyLampsName,
            string tidScreenWithSafetyLampsName, string meterScreenWithSafetyLampsWithoutTascName = null,
            string tidScreenWithSafetyLampsWithoutTascName = null) :
            base(context)
        {
            ViewModel = new TidChangeToTIMSWarningBackgroundRootViewModel();
            var resourceCache = context.GetResourceCache<string, ID2D1Bitmap>(ImageCacheKey);
            _meterScreenWithSafetyLampsBitmap = resourceCache.GetOrCreate(meterScreenWithSafetyLampsName, () =>
                context.DeviceContext.LoadBitmapFromResource(context.WicImagingFactory,
                    typeof(Images), meterScreenWithSafetyLampsName));
            _tidScreenWithSafetyLampsBitmap = resourceCache.GetOrCreate(tidScreenWithSafetyLampsName,
                () => context.DeviceContext.LoadBitmapFromResource(context.WicImagingFactory, typeof(Images),
                    tidScreenWithSafetyLampsName));
            _meterScreenWithSafetyLampsWithoutTascBitmap =
                LoadOptionalBitmap(resourceCache, context, meterScreenWithSafetyLampsWithoutTascName);
            _tidScreenWithSafetyLampsWithoutTascBitmap =
                LoadOptionalBitmap(resourceCache, context, tidScreenWithSafetyLampsWithoutTascName);
            _textDrawer = this.CreateTIMSTextDrawer(
                this.CreateTIMSTextLayout("この表示器をＴＩＭＳ表示器に\n切り替える場合は、左端の表示器に必ず\n保安表示灯を表示して下さい。",
                    arrangement: ContentArrangement.Near), 2, horizontalAlignment: 0.5f,
                verticalAlignment: 0.5f);
            RegisterResource(_textDrawer);
            WatchEffect(ViewModel.SupportsTasc);
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;

        private static ID2D1Bitmap LoadOptionalBitmap(ResourceCache<string, ID2D1Bitmap> resourceCache,
            RenderContext context, string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName)) return null;
            return resourceCache.GetOrCreate(resourceName, () =>
                context.DeviceContext.LoadBitmapFromResource(context.WicImagingFactory, typeof(Images),
                    resourceName));
        }

        protected override void OnDraw(float totalScale)
        {
            var hasWithoutTascImages = _meterScreenWithSafetyLampsWithoutTascBitmap != null &&
                                       _tidScreenWithSafetyLampsWithoutTascBitmap != null;
            var showWithoutTasc = hasWithoutTascImages && !ViewModel.SupportsTasc;
            var meterBitmap = showWithoutTasc
                ? _meterScreenWithSafetyLampsWithoutTascBitmap
                : _meterScreenWithSafetyLampsBitmap;
            var tidBitmap = showWithoutTasc
                ? _tidScreenWithSafetyLampsWithoutTascBitmap
                : _tidScreenWithSafetyLampsBitmap;
            Context.DeviceContext.DrawBitmap(meterBitmap,
                new RectangleF(200 - ImageWidth / 2, PaddingTop, ImageWidth,
                    ImageWidth * meterBitmap.Size.Height /
                    meterBitmap.Size.Width),
                1, BitmapInterpolationMode.Linear, null);
            Context.DeviceContext.DrawBitmap(tidBitmap,
                new RectangleF(600 - ImageWidth / 2, PaddingTop, ImageWidth,
                    ImageWidth * tidBitmap.Size.Height /
                    tidBitmap.Size.Width),
                1, BitmapInterpolationMode.Linear, null);
            Context.CommonBrush.Color = "#fdfd72".ToColor4();
            Context.DeviceContext.FillRectangle(
                new RectangleF(400 - BoundsWidth / 2, 300 - BoundsHeight / 2, BoundsWidth, BoundsHeight),
                Context.CommonBrush);
            _textDrawer.Draw(
                new RectangleF(400, 300 - BoundsHeight / 2, 0, BoundsHeight), Colors.Black);
        }
    }

    public class TidChangeToTIMSWarningBackgroundRootViewModel : ViewModel
    {
        private E233MonitorStates _monitorStates;
        public Signal<bool> SupportsTasc { get; } = new Signal<bool>();

        protected override void OnInitialize(DataHub dataHub)
        {
            _monitorStates = dataHub.Get<E233MonitorStates>();
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            SupportsTasc.Value = _monitorStates.SupportsTasc;
        }
    }
}