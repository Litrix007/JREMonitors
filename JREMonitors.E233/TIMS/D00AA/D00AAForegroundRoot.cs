using JREMonitors.Core.Contexts;
using JREMonitors.E233.Constants;

namespace JREMonitors.E233.TIMS.D00AA
{
    public class D00AAForegroundRoot : TIMSCommonForegroundRoot<TIMSCommonForegroundRootViewModel>
    {
        public D00AAForegroundRoot(RenderContext context, D00AAButtonGroup buttonGroup) : base(
            context, ScreenIds.D00AA, "運転士メニュー")
        {
            ViewModel = new TIMSCommonForegroundRootViewModel();
            buttonGroup.SupportsTasc.Bind(ViewModel.SupportsTasc);
            AddChild(buttonGroup);
        }
    }
}