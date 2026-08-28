using System;
using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts.Text;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Utils.Render;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS.D01AX.EDen
{
    public class D01AXEDenNextDutyInfo : Widget<D01AXEDenNextDutyInfoViewModel>
    {
        private const float BaseWidth = 240;
        private readonly D01AXSpec _spec;
        private readonly float _width;

        public D01AXEDenNextDutyInfo(RenderContext context, ScopedRenderContext scopedContext, TIMSVehicleSpec spec) :
            base(context, y: 310)
        {
            ViewModel = new D01AXEDenNextDutyInfoViewModel();
            _width = spec.D01AXSpec.HideNextDutyBackgroundWhenEmpty ? BaseWidth + 8 : BaseWidth;
            var paddingRight = spec.D01AXSpec.HideNextDutyBackgroundWhenEmpty ? 0 : 30;
            X.Bind(CreateComputed(() =>
                ViewModel.VehicleDirection.Value == TIMSVehicleDirection.Left
                    ? spec.D01AXSpec.MayShowRouteSetInformation ? 137 : 30
                    : 800 - paddingRight - _width));
            _spec = spec.D01AXSpec;
            var title = new BoundsDrawerWidget(context,
                this.CreateTIMSTextDrawer(
                    CreateComputed(() =>
                        RichTextParser.Raw(ViewModel.IsEmpty.Value ? "\u3000\u3000\u3000" : "次行路"))
                ),
                contentColor: MonitorColors.TIMSTitleGrey,
                backgroundColor: Colors.Black,
                y: 6);
            AddChild(title);
            var trainNumberText = new BoundsDrawerWidget(scopedContext,
                this.CreateTIMSTextDrawer(
                    CreateComputed(() => RichTextParser.Raw(ViewModel.TrainNumber.Value)),
                    context: scopedContext
                ),
                contentColor: MonitorColors.White,
                backgroundColor: Colors.Black,
                y: 26);
            AddChild(trainNumberText);
            var arrivalText = new D01AXNormalTimeText(
                scopedContext,
                _width,
                6,
                1,
                false,
                ViewModel.ArrivalHoursAndMinutes,
                ViewModel.ArrivalSeconds,
                new Signal<string>("\u3000"),
                1
            );
            arrivalText.BackgroundColor.Value = Colors.Black;
            AddChild(arrivalText);
            var departureText = new D01AXNormalTimeText(
                scopedContext,
                _width,
                26,
                1,
                false,
                ViewModel.DepartureHoursAndMinutes,
                ViewModel.DepartureSeconds,
                ViewModel.DepartureChar,
                1
            );
            departureText.BackgroundColor.Value = Colors.Black;
            AddChild(departureText);
        }

        public override RectangleF SelfRelativeDirtyBounds => new RectangleF(0, 0, _width, 52);

        private bool ShowBackground => !ViewModel.IsEmpty.Value || !_spec.HideNextDutyBackgroundWhenEmpty;

        protected override void OnDraw(float totalScale)
        {
            if (ShowBackground)
                Context.DeviceContext.WithAliasedIfNeeded(() =>
                {
                    Context.CommonBrush.Color = Colors.Black;
                    Context.DeviceContext.FillRectangle(SelfRelativeDirtyBounds, Context.CommonBrush);
                });
        }
    }

    public class D01AXEDenNextDutyInfoViewModel : TIMSViewModel
    {
        private readonly Signal<string> _rawTrainNumber = new Signal<string>(string.Empty);

        public D01AXEDenNextDutyInfoViewModel()
        {
            TrainNumber = CreateComputed(() =>
                string.IsNullOrEmpty(_rawTrainNumber)
                    ? "\u3000\u3000\u3000\u3000\u3000\u3000\u3000"
                    : TIMSHelper.FormatTrainNumber(_rawTrainNumber, ' ', true).ToFullWidth());
        }

        public Signal<bool> IsEmpty { get; } = new Signal<bool>();
        public Signal<string> DepartureChar { get; } = new Signal<string>(string.Empty);
        public Computed<string> TrainNumber { get; }
        public Signal<string> ArrivalHoursAndMinutes { get; } = new Signal<string>(string.Empty);
        public Signal<string> ArrivalSeconds { get; } = new Signal<string>(string.Empty);
        public Signal<string> DepartureHoursAndMinutes { get; } = new Signal<string>(string.Empty);
        public Signal<string> DepartureSeconds { get; } = new Signal<string>(string.Empty);

        protected override void OnUpdate(TimeSpan elapsed)
        {
            base.OnUpdate(elapsed);
            VehicleDirection.Value = TIMSService.VehicleDirection;
            ClearText();
            var nextDuty = ICCardService.NextDuty;
            IsEmpty.Value = nextDuty == null;
            DepartureChar.Value = IsEmpty.Value ? "\u3000" : "発";
            if (nextDuty == null) return;
            _rawTrainNumber.Value = nextDuty.TrainNumber;
            ArrivalHoursAndMinutes.Value = TIMSHelper.GetHoursAndMinutes(null, nextDuty.ArrivalTime).PadLeft(5);
            ArrivalSeconds.Value = TIMSHelper.GetSeconds(nextDuty.ArrivalTime, false, true);
            DepartureHoursAndMinutes.Value =
                TIMSHelper.GetHoursAndMinutes(null, nextDuty.DepartureTime).PadLeft(5);
            DepartureSeconds.Value = TIMSHelper.GetSeconds(nextDuty.DepartureTime, false, true);
        }

        private void ClearText()
        {
            _rawTrainNumber.Value = string.Empty;
            ArrivalHoursAndMinutes.Value = "\u3000\u3000 \u3000\u3000";
            ArrivalSeconds.Value = "  ";
            DepartureHoursAndMinutes.Value = "\u3000\u3000 \u3000\u3000";
            DepartureSeconds.Value = "  ";
        }
    }
}