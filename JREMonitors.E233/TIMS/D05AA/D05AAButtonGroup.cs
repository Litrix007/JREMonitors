using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Services;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;

namespace JREMonitors.E233.TIMS.D05AA
{
    public class D05AAButtonGroup : TIMSDriverButtonGroup
    {
        public D05AAButtonGroup(RenderContext context) : base(context, ScreenIds.D05AA)
        {
            AddChild(this.CreateFooterOptionsRow(new Widget[]
            {
                this.CreateTIMSFlexButton("次画面", 1, 1, onClick: () =>
                {
                    RequestBlock(TIMSBlockTypes.ChangeScreen, BlockingLevel.CurrentScreen, 1,
                        () => { Context.DisplayController.RequestChangeScreen(ScreenIds.D05AB); });
                }),
                new PlaceHolder(context)
            }));
        }
    }
}