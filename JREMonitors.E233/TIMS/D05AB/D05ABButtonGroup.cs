using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Services;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;

namespace JREMonitors.E233.TIMS.D05AB
{
    public class D05ABButtonGroup : TIMSDriverButtonGroup
    {
        public D05ABButtonGroup(RenderContext context) : base(context, ScreenIds.D05AB)
        {
            AddChild(this.CreateFooterOptionsRow(new Widget[]
            {
                this.CreateTIMSFlexButton("前画面", 1, 1, onClick: () =>
                {
                    RequestBlock(TIMSBlockTypes.ChangeScreen, BlockingLevel.CurrentScreen, 1,
                        () => { Context.DisplayController.RequestChangeScreen(ScreenIds.D05AA); });
                }),
                this.CreateTIMSFlexButton("次画面", 1, 1, clickable: new Signal<bool>()),
                new PlaceHolder(context)
            }));
        }
    }
}