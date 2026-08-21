using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Services;
using JREMonitors.Core.Utils;
using JREMonitors.E233.Constants;

namespace JREMonitors.E233.TIMS
{
    public class TIMSDriverButtonGroup : TIMSCommonButtonGroup
    {
        public TIMSDriverButtonGroup(RenderContext context, string id) : base(context)
        {
            var row = Row.FromBounds(context,
                GeometryHelper.CreateRowBounds(7, 506, 8, new[] { new RowItemWidth(4, 90) }, 64), 8,
                new[]
                {
                    this.CreateTIMSFlexButton("運転士\nメニュー", 1, 1, ContentArrangement.Near,
                        fixedLineSpacing: 2, onClick: () =>
                        {
                            RequestBlock(TIMSBlockTypes.ChangeScreen, BlockingLevel.CurrentScreen, 1,
                                () => Context.DisplayController.RequestChangeScreen(ScreenIds.D00AA));
                        }),
                    this.CreateTIMSFlexButton("運転情報\n画面", 1, 1, ContentArrangement.Near,
                        fixedLineSpacing: 2, onClick: () =>
                        {
                            if (id == ScreenIds.D01AX) return;
                            RequestBlock(TIMSBlockTypes.ChangeScreen, BlockingLevel.CurrentScreen, 1,
                                () => Context.DisplayController.RequestChangeScreen(ScreenIds.D01AX));
                        }),
                    this.CreateTIMSFlexButton("応急マニ\nュァル", 1, 1, ContentArrangement.Near, true,
                        2),
                    this.CreateTIMSFlexButton("異常扱い", 1, 1, ContentArrangement.Near)
                });
            AddChild(row);
        }
    }
}