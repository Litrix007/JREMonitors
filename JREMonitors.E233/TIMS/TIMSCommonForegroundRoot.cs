using System;
using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.State;
using JREMonitors.Core.Widgets;
using TextAntialiasMode = Vortice.Direct2D1.TextAntialiasMode;

namespace JREMonitors.E233.TIMS
{
    public class TIMSCommonForegroundRoot<TViewModel> : Widget<TViewModel>
        where TViewModel : TIMSCommonForegroundRootViewModel
    {
        private readonly ContentGroup _contentGroup;

        protected readonly TIMSScreenTitle ScreenTitle;

        public TIMSCommonForegroundRoot(RenderContext context, string id, string title,
            TIMSCommonBackground background = null)
            : base(context)
        {
            base.AddChild(background ?? new TIMSCommonBackground(context));
            _contentGroup = new ContentGroup(context);
            _contentGroup.IsVisible.Value = false;
            base.AddChild(_contentGroup);
            AddChild(new TIMSVehicleStateWidget(context, id.StartsWith("D")));
            ScreenTitle = new TIMSScreenTitle(context, id, title);
            AddChild(ScreenTitle);
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;

        protected new void AddChild(Widget widget, bool isGlobalPosition = false)
        {
            _contentGroup.AddChild(widget, isGlobalPosition);
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            _contentGroup.IsVisible.Value = !IsFirstUpdate;
        }

        protected override void Render(RectangleF globalClipRect, float parentScale, bool forceRender,
            bool ignoreHidden)
        {
            var oldTextAntialiasMode = Context.DeviceContext.TextAntialiasMode;
            Context.DeviceContext.TextAntialiasMode = TextAntialiasMode.Aliased;
            base.Render(globalClipRect, parentScale, forceRender, ignoreHidden);
            Context.DeviceContext.TextAntialiasMode = oldTextAntialiasMode;
        }

        private class ContentGroup : Widget
        {
            public ContentGroup(RenderContext context) : base(context)
            {
            }

            public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;

            public new void AddChild(Widget widget, bool isGlobalPosition = false)
            {
                base.AddChild(widget, isGlobalPosition);
            }
        }
    }

    public class TIMSCommonForegroundRootViewModel : ViewModel
    {
        private E233MonitorStates _monitorStates;
        public Signal<E233MonitorType> MonitorType { get; } = new Signal<E233MonitorType>();

        protected override void OnInitialize(DataHub dataHub)
        {
            _monitorStates = dataHub.Get<E233MonitorStates>();
        }


        protected override void OnUpdate(TimeSpan elapsed)
        {
            MonitorType.Value = _monitorStates.MonitorType;
        }
    }
}