using System;
using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts.Text;
using JREMonitors.Core.Providers;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Services;
using JREMonitors.Core.State;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.JRE.Constants;
using JREMonitors.JRE.Providers;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS
{
    public class TIMSVehicleStateWidget : Widget<TIMSVehicleStateViewModel>
    {
        public TIMSVehicleStateWidget(RenderContext context, bool isDriverScreen) : base(context)
        {
            ViewModel = new TIMSVehicleStateViewModel();
            var timeWidget = new BoundsDrawerWidget(context,
                this.CreateTIMSTextDrawer(CreateComputed(() => RichTextParser.Raw(ViewModel.Time.Value)),
                    horizontalAlignment: 1, verticalAlignment: 0, cache: false), x: 387, y: 0,
                contentColor: MonitorColors.White);
            AddChild(timeWidget);
            var speedWidget = new BoundsDrawerWidget(context,
                this.CreateTIMSTextDrawer(
                    CreateComputed(() =>
                        new RichTextBuilder().Append(ViewModel.Speed).Append("km/h", color: MonitorColors.TIMSTitleGrey)
                            .Build()), horizontalAlignment: 1, verticalAlignment: 0, cache: false), x: 387, y: 21,
                contentColor: MonitorColors.White);
            AddChild(speedWidget);
            var locationWidget = new BoundsDrawerWidget(context,
                this.CreateTIMSTextDrawer(
                    CreateComputed(() =>
                        new RichTextBuilder().Append(ViewModel.Mileage)
                            .Append(ViewModel.ShowMileageInMeter ? "m " : "km ", color: MonitorColors.TIMSTitleGrey)
                            .Build()), horizontalAlignment: 1, verticalAlignment: 0, cache: false), x: 387, y: 42);
            locationWidget.ContentColor.Bind(CreateComputed(() =>
                ViewModel.ShowMileageInMeter ? Colors.Yellow : MonitorColors.White));
            AddChild(locationWidget);
            if (isDriverScreen)
            {
                var alertWidget = new BoundsDrawerWidget(context,
                    this.CreateTIMSTextDrawer(CreateComputed(() => RichTextParser.Raw(ViewModel.AlertText)),
                        verticalAlignment: 1), contentColor: MonitorColors.White, backgroundColor: Colors.Red,
                    x: 400, y: 599);
                alertWidget.IsVisible.Bind(CreateComputed(() => !string.IsNullOrEmpty(ViewModel.AlertText)));
                AddChild(alertWidget);
            }
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
        public override float? RefreshSpeed => IsFirstUpdate ? RefreshSpeeds.Fast : (float?)null;
    }

    public class TIMSVehicleStateViewModel : TIMSViewModel
    {
        private DelayedSpeedProvider _delayedSpeedProvider;
        private TickTracker _normalTickTracker;
        private ITimeProvider _timeProvider;
        public Signal<string> Time { get; } = new Signal<string>(string.Empty);
        public Signal<string> Speed { get; } = new Signal<string>(string.Empty);
        public Signal<string> Mileage { get; } = new Signal<string>(string.Empty);
        public Signal<bool> ShowMileageInMeter { get; } = new Signal<bool>();
        public Signal<string> AlertText { get; } = new Signal<string>(string.Empty);

        protected override void OnInitialize(DataHub dataHub)
        {
            base.OnInitialize(dataHub);
            _timeProvider = dataHub.Get<ITimeProvider>();
            var delayService = dataHub.Get<DelayService>();
            _normalTickTracker = new TickTracker(delayService.GetDelayProvider(DelayTypes.Normal));
            _delayedSpeedProvider = dataHub.Get<DelayedSpeedProvider>();
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            base.OnUpdate(elapsed);
            var currentTime = _timeProvider.CurrentTime;
            var hour = currentTime.Hours;
            var minute = currentTime.Minutes;
            var second = currentTime.Seconds;
            Time.Value = $"{hour:D2}:{minute:D2}:{second:D2}".ToFullWidth();
            Speed.Value = $"{_delayedSpeedProvider.Speed}".ToFullWidth();
            ShowMileageInMeter.Value = TIMSService.ShowMileageInMeter;
            if (ShowMileageInMeter)
            {
                var mileage = $"{(int)ICCardService.CurrentMileage}";
                if (mileage.Length <= 8)
                {
                    mileage = mileage.ToFullWidth();
                }

                Mileage.Value = mileage;
            }
            else
            {
                if (_normalTickTracker.TrackAndSync())
                {
                    var value = ICCardService.CurrentMileage / 1000;
                    var truncatedValue = Math.Truncate(value * 10) / 10;
                    Mileage.Value = truncatedValue.ToString("F1").ToFullWidth();
                }
            }

            AlertText.Value = string.Empty;
            if (!ICCardService.Inserted)
                AlertText.Value = TIMSService.IsSettingCompleted ? "仕業カードを挿入して下さい。" : "仕業カード挿入か､列番を設定して下さい｡";
        }

        protected override void OnReset()
        {
            base.OnReset();
            _normalTickTracker.Reset();
        }
    }
}