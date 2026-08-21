using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;

namespace JREMonitors.E233.TIMS.D01AX
{
    public class D01AXButtonGroup : Widget
    {
        public D01AXButtonGroup(RenderContext context, D01AXTrainTypeButtonGroup trainTypeButtonGroup) : base(context)
        {
            AddChild(new TIMSDriverButtonGroup(context, ScreenIds.D01AX));
            AddChild(trainTypeButtonGroup);
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;

        public override bool IsPointerDownBlocked => IsTypeBlocked(TIMSBlockTypes.ChangeScreen) ||
                                                     IsTypeBlocked(TIMSBlockTypes.SetPassSetting);
    }
}