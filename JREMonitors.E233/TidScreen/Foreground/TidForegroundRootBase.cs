using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Buttons;
using JREMonitors.E233.Constants;
using JREMonitors.E233.ViewModels;

namespace JREMonitors.E233.TidScreen.Foreground
{
    public abstract class TidForegroundRootBase : Widget<TidForegroundRootBaseViewModel>
    {
        private const string IdWithTasc = "WithTasc";
        private const string IdWithoutTasc = "WithoutTasc";

        private readonly WidgetSwitcher _layoutSwitcher;

        protected TidForegroundRootBase(RenderContext context) : base(context)
        {
            ViewModel = new TidForegroundRootBaseViewModel();
            _layoutSwitcher = new WidgetSwitcher(context, WidgetSwitcher.RefreshPolicy.FadeOutThenFadeIn);
            AddChild(_layoutSwitcher);
            WatchEffect(EffectPhase.State, () => _layoutSwitcher.SetActiveWidget(ResolveActiveLayoutId()));
            WatchEffect(() =>
            {
                if (IsOffScreen || IsFirstUpdate) return;
                context.DisplayController.RequestReset();
            }, ViewModel.SupportsTasc);
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;

        protected InfoButtonGroup CreateInfoButtonGroup(bool addHomeButton = true)
        {
            var group = new InfoButtonGroup(Context, 10, new Vector2(1024, 768), addHomeButton);
            group.HomeButton.OnClick += OnHomeButtonClick;
            return group;
        }

        protected virtual string ResolveActiveLayoutId()
        {
            return ViewModel.SupportsTasc ? IdWithTasc : IdWithoutTasc;
        }

        protected void AddLayout(string id, Widget layout)
        {
            _layoutSwitcher.Add(id, layout);
        }

        protected void AddTascLayout(Widget layout)
        {
            AddLayout(IdWithTasc, layout);
        }

        protected void AddNonTascLayout(Widget layout)
        {
            AddLayout(IdWithoutTasc, layout);
        }

        private void OnHomeButtonClick()
        {
            Context.DisplayController.RequestChangeScreen(ScreenIds.TidChangeToTIMSWarning);
        }
    }
}