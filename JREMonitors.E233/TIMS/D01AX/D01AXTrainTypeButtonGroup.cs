using System;
using System.Collections.Generic;
using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Providers;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Services;
using JREMonitors.Core.State;
using JREMonitors.Core.Widgets;
using JREMonitors.JRE.Constants;

namespace JREMonitors.E233.TIMS.D01AX
{
    public class D01AXTrainTypeButtonGroup : Widget<D01AXTrainTypeButtonGroupViewModel>
    {
        private readonly Row _row;

        public D01AXTrainTypeButtonGroup(RenderContext context, bool showTrainTypeButtons,
            D01AXTrainTypeButtonGroupViewModel viewModel = null)
            : base(context)
        {
            ViewModel = viewModel ?? new D01AXTrainTypeButtonGroupViewModel();
            var widgets = new List<Widget>();
            if (showTrainTypeButtons)
            {
                var selectTrainTypeButton = this.CreateTIMSFlexButton("列車選別", 1, 1, playPressAnimation: false);
                selectTrainTypeButton.PressedOverride.Bind(ViewModel.IsTrainSelectionPressed);
                selectTrainTypeButton.OnClick += ViewModel.SelectTrainType;
                widgets.Add(selectTrainTypeButton);
                var setTrainTypeButton = this.CreateTIMSFlexButton("設\u3000定", 1, 1);
                setTrainTypeButton.Highlighted.Bind(ViewModel.IsSetButtonHighlighted);
                setTrainTypeButton.OnClick += ViewModel.Apply;
                widgets.Add(setTrainTypeButton);
            }

            _row = this.CreateFooterOptionsRow(widgets);
            base.AddChild(_row);
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;

        protected new void AddChild(Widget widget, bool isGlobalPosition = false)
        {
            _row.AddChild(widget, isGlobalPosition);
        }

        protected new void InsertChild(Widget widget, int index = 0, bool isGlobalPosition = false)
        {
            _row.InsertChild(widget, index, isGlobalPosition);
        }

        protected new void InsertChildAfter(Widget widget, Widget afterWidget, bool isGlobalPosition = false)
        {
            _row.InsertChildAfter(widget, afterWidget, isGlobalPosition);
        }
    }

    public class D01AXTrainTypeButtonGroupViewModel : ViewModel
    {
        private TickTracker _blinkTickTracker;
        private E233MonitorStates _monitorStates;
        private TIMSService _timsService;
        public Signal<bool> SupportsTasc { get; } = new Signal<bool>();
        public Signal<bool> IsTrainSelectionPressed { get; } = new Signal<bool>();
        public Signal<bool> IsSetButtonHighlighted { get; } = new Signal<bool>();

        protected override void OnInitialize(DataHub dataHub)
        {
            var delayService = dataHub.Get<DelayService>();
            _blinkTickTracker = new TickTracker(delayService.GetDelayProvider(DelayTypes.TIMSBlink));
            _timsService = dataHub.Get<TIMSService>();
            _monitorStates = dataHub.Get<E233MonitorStates>();
        }

        protected override void OnEnter()
        {
            _timsService.TrainSelectionCount++;
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            var isTrainSelectionActive = _timsService.CurrentSelectionType == TIMSSelectionType.TrainSelection;
            IsTrainSelectionPressed.Value = isTrainSelectionActive;
            IsSetButtonHighlighted.Value = isTrainSelectionActive && TIMSHelper.IsBlink(_blinkTickTracker.Sync());
            SupportsTasc.Value = _monitorStates.SupportsTasc;
        }

        public void SelectTrainType()
        {
            _timsService.ToggleTrainSelection();
        }

        public void Apply()
        {
            _timsService.Apply();
        }

        protected override void OnReset()
        {
            _blinkTickTracker.Reset();
        }

        protected override void OnExit()
        {
            _timsService.TrainSelectionCount--;
        }
    }

    public class D01AXTrainTypeButtonGroup0 : D01AXTrainTypeButtonGroup
    {
        public D01AXTrainTypeButtonGroup0(RenderContext context) : base(context, true)
        {
            var button1 = this.CreateTIMSFlexButton("次駅停車\nクリア", 1, 1, clickable: new Signal<bool>());
            button1.IsVisible.Bind(ViewModel.SupportsTasc);
            InsertChild(button1);
            var button2 = this.CreateTIMSFlexButton("ＴＡＳＣ\n次駅停車", 1, 1, clickable: new Signal<bool>());
            button2.IsVisible.Bind(ViewModel.SupportsTasc);
            InsertChild(button2);
            WatchEffect(() =>
            {
                if (IsOffScreen || IsFirstUpdate) return;
                Context.DisplayController.RequestReset();
            }, ViewModel.SupportsTasc);
        }
    }
}