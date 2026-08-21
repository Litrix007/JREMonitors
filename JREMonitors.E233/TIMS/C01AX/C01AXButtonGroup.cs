using System.Drawing;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Services;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS.C01AX
{
    public class C01AXButtonGroup : TIMSConductorButtonGroup
    {
        public C01AXButtonGroup(RenderContext context, string id) : base(context, id)
        {
            FormationSpec = CreateRelayPropertySlot<TIMSFormationSpec>();
            var isFooterVisible = CreateComputed(() =>
            {
                var formationSpec = FormationSpec.Value;
                return formationSpec != null && formationSpec.HasGreenCar;
            });
            var footerRow = this.CreateFooterOptionsRow(new Widget[]
            {
                id == ScreenIds.C01AB
                    ? this.CreateTIMSFlexButton("前画面", 1, 1, onClick: () =>
                    {
                        RequestBlock(TIMSBlockTypes.ChangeScreen, BlockingLevel.CurrentScreen, 1,
                            () => { Context.DisplayController.RequestChangeScreen(ScreenIds.C01AA); });
                    })
                    : null,
                id == ScreenIds.C01AA
                    ? this.CreateTIMSFlexButton("次画面", 1, 1, onClick: () =>
                    {
                        RequestBlock(TIMSBlockTypes.ChangeScreen, BlockingLevel.CurrentScreen, 1,
                            () =>
                            {
                                if (!isFooterVisible) return;
                                Context.DisplayController.RequestChangeScreen(ScreenIds.C01AB);
                            });
                    })
                    : null,
                new PlaceHolder(context)
            });
            footerRow.IsVisible.Bind(isFooterVisible);
            var promptText = id == ScreenIds.C01AA ? "※グリーン車の室温は、次画面を参照して下さい。" : "※普通車の情報は、前画面を参照して下さい。";
            var footerPrompt = new BoundsDrawerWidget(Context, this.CreateTIMSTextDrawer(promptText),
                contentColor: Colors.Aqua);
            footerPrompt.IsVisible.Bind(isFooterVisible);
            footerPrompt.Y.Bind(CreateComputed(() => footerRow.Y + 10));
            footerPrompt.X.Value = 50;
            AddChild(footerPrompt);
            AddChild(footerRow);
        }

        public PropertySlot<TIMSFormationSpec> FormationSpec { get; }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
    }
}