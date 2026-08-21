using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Services;
using JREMonitors.Core.Utils;
using JREMonitors.E233.Constants;

namespace JREMonitors.E233.TIMS
{
    public class TIMSConductorButtonGroup : TIMSCommonButtonGroup
    {
        public TIMSConductorButtonGroup(RenderContext context, string id) : base(context)
        {
            var row = Row.FromBounds(context,
                GeometryHelper.CreateRowBounds(7, 506, 8, new[] { new RowItemWidth(2, 90) }, 64), 8,
                new[]
                {
                    this.CreateTIMSFlexButton("車掌\nメニュー", 1, 1, ContentArrangement.Near,
                        fixedLineSpacing: 2, onClick: () =>
                        {
                            RequestBlock(TIMSBlockTypes.ChangeScreen, BlockingLevel.CurrentScreen, 1,
                                () => Context.DisplayController.RequestChangeScreen(ScreenIds.C00AA));
                        }),
                    this.CreateTIMSFlexButton("車掌情報\n画面", 1, 1, ContentArrangement.Near,
                        fixedLineSpacing: 2, onClick: () =>
                        {
                            if (id == ScreenIds.C01AA) return;
                            RequestBlock(TIMSBlockTypes.ChangeScreen, BlockingLevel.CurrentScreen, 1,
                                () => Context.DisplayController.RequestChangeScreen(ScreenIds.C01AA));
                        })
                });
            AddChild(row);
        }
    }
}