using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Services;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;

namespace JREMonitors.E233.TIMS.D00AA
{
    public class D00AAButtonGroup : TIMSCommonButtonGroup
    {
        public D00AAButtonGroup(RenderContext context, TIMSVehicleSpec spec) : base(context)
        {
            const float bigButtonWidth = 218;
            const float bigButtonHeight = 80;
            const float bigButtonSpacing = 25;
            var bigButtonGrid = new Col(context,
                40, 129,
                bigButtonWidth,
                bigButtonHeight,
                bigButtonSpacing,
                new List<Widget>
                {
                    this.CreateTIMSFlexButton("運転情報", 2, 2,
                        onClick: () =>
                        {
                            RequestBlock(TIMSBlockTypes.ChangeScreen, BlockingLevel.CurrentScreen, 1,
                                () => { Context.DisplayController.RequestChangeScreen(ScreenIds.D01AX); });
                        }),
                    this.CreateTIMSFlexButton(new[]
                    {
                        new BitmapScaleDrawer.DrawerProperties(this.CreateTIMSTextLayout("応急"), 2, 2),
                        new BitmapScaleDrawer.DrawerProperties(this.CreateTIMSTextLayout("マニュァル"), 1, 2)
                    }),
                    this.CreateTIMSFlexButton("異常扱い", 2, 2)
                });
            AddChild(bigButtonGrid);
            const float smallButtonWidth = 142;
            const float smallButtonHeight = 50;
            const float smallButtonColSpacing = 15;
            var externalRow = GetExternalRow(spec);
            var rowWidgets = GetSmallButtonRows(spec)
                .Concat(new[] { externalRow })
                .Where(r => r != null)
                .Select(r => (Widget)new Row(
                    context,
                    widgetSpacing: smallButtonColSpacing,
                    widgets: r,
                    positionSnapToPixels: true
                ))
                .ToList();
            var smallButtonRowSpacing = (346 - smallButtonHeight * rowWidgets.Count) / (rowWidgets.Count - 1);
            var smallButtonCol = new Col(
                context,
                278,
                129,
                smallButtonWidth,
                explicitAvailableHeight: 346,
                widgetSpacing: smallButtonRowSpacing,
                widgets: rowWidgets,
                positionSnapToPixels: true
            );
            AddChild(smallButtonCol);
        }

        public override bool IsPointerDownBlocked => IsTypeBlocked(TIMSBlockTypes.ChangeScreen);

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;

        protected virtual Widget GetLastWidget(TIMSVehicleSpec spec)
        {
            return null;
        }

        protected virtual IList<ICollection<Widget>> GetSmallButtonRows(TIMSVehicleSpec spec)
        {
            return new List<ICollection<Widget>>
            {
                new Widget[]
                {
                    this.CreateTIMSFlexButton("車両情報", 1, 1, onClick: () =>
                    {
                        RequestBlock(TIMSBlockTypes.ChangeScreen, BlockingLevel.CurrentScreen, 1,
                            () => { Context.DisplayController.RequestChangeScreen(ScreenIds.D02AA); });
                    }),
                    this.CreateTIMSFlexButton("出区情報", 1, 1),
                    this.CreateTIMSFlexButton("列番設定", 1, 1)
                },
                new Widget[]
                {
                    this.CreateTIMSFlexButton("ブレーキ\n確認情報", 1, 1, onClick: () =>
                    {
                        RequestBlock(TIMSBlockTypes.ChangeScreen, BlockingLevel.CurrentScreen, 1,
                            () => { Context.DisplayController.RequestChangeScreen(ScreenIds.D05AA); });
                    }),
                    this.CreateTIMSFlexButton("起動確認情報", 1, 1),
                    this.CreateTIMSFlexButton("時計設定", 1, 1)
                },
                new Widget[]
                {
                    this.CreateTIMSFlexButton("三相給電", 1, 1),
                    this.CreateTIMSFlexButton("積算電力", 1, 1),
                    new PlaceHolder(Context)
                },
                new Widget[]
                {
                    this.CreateTIMSFlexButton("TIMS伝送情報", 1, 1),
                    this.CreateTIMSFlexButton("機器開放", 1, 1),
                    new PlaceHolder(Context)
                },
                new[]
                {
                    this.CreateTIMSFlexButton("BLB開放", 1, 1),
                    this.CreateTIMSFlexButton("列車無線情報", 1, 1),
                    GetLastWidget(spec)
                }
            };
        }

        protected virtual ICollection<Widget> GetExternalRow(TIMSVehicleSpec spec)
        {
            return null;
        }
    }

    public class D00AAButtonGroup0 : D00AAButtonGroup
    {
        public D00AAButtonGroup0(RenderContext context, TIMSVehicleSpec spec) : base(context, spec)
        {
        }

        protected override Widget GetLastWidget(TIMSVehicleSpec spec)
        {
            return this.CreateTIMSFlexButton("TASC設定·確認", 1, 1);
        }
    }

    public class D00AAButtonGroup1000 : D00AAButtonGroup
    {
        public D00AAButtonGroup1000(RenderContext context, TIMSVehicleSpec spec) : base(context, spec)
        {
        }

        protected override Widget GetLastWidget(TIMSVehicleSpec spec)
        {
            return this.CreateTIMSFlexButton("ＡＴＣ", 1, 1);
        }
    }

    public class D00AAButtonGroup5000 : D00AAButtonGroup
    {
        public D00AAButtonGroup5000(RenderContext context, TIMSVehicleSpec spec) : base(context, spec)
        {
        }

        protected override Widget GetLastWidget(TIMSVehicleSpec spec)
        {
            return this.CreateTIMSFlexButton("駅間電力情報", 1, 1);
        }
    }
}