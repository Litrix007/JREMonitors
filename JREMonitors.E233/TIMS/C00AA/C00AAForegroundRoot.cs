using JREMonitors.Core.Contexts;
using JREMonitors.E233.Constants;

namespace JREMonitors.E233.TIMS.C00AA
{
    public class C00AAForegroundRoot : TIMSCommonForegroundRoot<TIMSCommonForegroundRootViewModel>
    {
        public C00AAForegroundRoot(RenderContext context, C00AAButtonGroup buttonGroup) : base(context, ScreenIds.C00AA,
            "車掌メニュー")
        {
            ViewModel = new TIMSCommonForegroundRootViewModel();
            buttonGroup.IsTIMSMain.Bind(CreateComputed(() => ViewModel.MonitorType == E233MonitorType.TIMSMain));
            AddChild(buttonGroup);
        }
    }
}