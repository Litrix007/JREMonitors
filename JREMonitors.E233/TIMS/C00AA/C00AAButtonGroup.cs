using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Layouts.Text;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Services;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;

namespace JREMonitors.E233.TIMS.C00AA
{
    public class C00AAButtonGroup : TIMSCommonButtonGroup
    {
        public C00AAButtonGroup(RenderContext context, TIMSVehicleSpec spec) : base(context)
        {
            const float buttonWidth = 240;
            const float buttonHeight = 65;
            const float rowSpacing = 35;
            IsTIMSMain = CreatePropertySlot<bool>(DirtyType.Visual);

            var colWidgets = new[] { GetCol1(spec), GetCol2(spec), GetCol3(spec) }
                .Where(c => c != null)
                .Select(c => (Widget)new Col(
                    context,
                    fallbackFlexUnitWidth: buttonWidth,
                    fallbackFlexUnitHeight: buttonHeight,
                    widgetSpacing: rowSpacing,
                    widgets: c,
                    positionSnapToPixels: true
                ))
                .ToList();

            var buttonRow = new Row(
                context,
                400,
                300,
                buttonWidth,
                buttonHeight,
                ColSpacing,
                colWidgets,
                0.5f,
                0.5f,
                positionSnapToPixels: true
            );
            AddChild(buttonRow);
        }

        protected virtual float ColSpacing => 10;

        public PropertySlot<bool> IsTIMSMain { get; }

        public override bool IsPointerDownBlocked => IsTypeBlocked(TIMSBlockTypes.ChangeScreen);

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;

        protected virtual ICollection<Widget> GetCol1(TIMSVehicleSpec spec)
        {
            return new Widget[]
            {
                this.CreateTIMSFlexButton("車 掌 情 報", 1, 2,
                    onClick: () =>
                    {
                        RequestBlock(TIMSBlockTypes.ChangeScreen, BlockingLevel.CurrentScreen, 1,
                            () => { Context.DisplayController.RequestChangeScreen(ScreenIds.C01AA); });
                    }),
                this.CreateTIMSFlexButton("異 常 扱 い", 1, 2),
                this.CreateTIMSFlexButton(
                    CreateComputed(() => RichTextParser.Raw(IsTIMSMain ? "運 行 情 報" : "")), 1, 2, clickable: IsTIMSMain),
                this.CreateTIMSFlexButton(
                    CreateComputed(() => RichTextParser.Raw(IsTIMSMain ? "案 内 簡 易 設 定" : "")), 1, 2,
                    clickable: IsTIMSMain)
            };
        }

        protected virtual ICollection<Widget> GetCol2(TIMSVehicleSpec spec)
        {
            return new Widget[]
            {
                this.CreateTIMSFlexButton("行 先 設 定", 1, 2),
                this.CreateTIMSFlexButton(
                    CreateComputed(() => RichTextParser.Raw(IsTIMSMain ? "サ一ビス機器制御" : "")), 1, 2,
                    clickable: IsTIMSMain),
                this.CreateTIMSFlexButton(
                    CreateComputed(() => RichTextParser.Raw(IsTIMSMain ? "案 内 設 定" : "")), 1, 2, clickable: IsTIMSMain),
                this.CreateTIMSFlexButton(
                    CreateComputed(() => RichTextParser.Raw(IsTIMSMain ? "案 内 引 継" : "")), 1, 2, clickable: IsTIMSMain)
            };
        }

        protected virtual ICollection<Widget> GetCol3(TIMSVehicleSpec spec)
        {
            return new Widget[]
            {
                this.CreateTIMSFlexButton("開 扉 選 択", 1, 2),
                this.CreateTIMSFlexButton("Ｖ Ｉ Ｓ 状 態", 1, 2),
                this.CreateTIMSFlexButton(
                    CreateComputed(() => RichTextParser.Raw(IsTIMSMain ? "異 常 時 案 内" : "")), 1, 2,
                    clickable: IsTIMSMain)
            };
        }
    }

    public class C00AAButtonGroup1000 : C00AAButtonGroup
    {
        public C00AAButtonGroup1000(RenderContext context, TIMSVehicleSpec spec) : base(context, spec)
        {
        }

        protected override ICollection<Widget> GetCol2(TIMSVehicleSpec spec)
        {
            return new Widget[]
            {
                this.CreateTIMSFlexButton("行 先 設 定", 1, 2),
                this.CreateTIMSFlexButton(
                    CreateComputed(() => RichTextParser.Raw(IsTIMSMain ? "サ一ビス機器制御" : "")), 1, 2,
                    clickable: IsTIMSMain),
                this.CreateTIMSFlexButton(
                    CreateComputed(() => RichTextParser.Raw(IsTIMSMain ? "案 内 設 定" : "")), 1, 2, clickable: IsTIMSMain),
                this.CreateTIMSFlexButton("Ｖ Ｉ Ｓ 状 態", 1, 2)
            };
        }

        protected override ICollection<Widget> GetCol3(TIMSVehicleSpec spec)
        {
            return new Widget[]
            {
                this.CreateTIMSFlexButton(
                    CreateComputed(() => RichTextParser.Raw(IsTIMSMain ? "異 常 時 案 内" : "")), 1, 2,
                    clickable: IsTIMSMain)
            };
        }
    }

    public class C00AAButtonGroup3000 : C00AAButtonGroup
    {
        public C00AAButtonGroup3000(RenderContext context, TIMSVehicleSpec spec) : base(context, spec)
        {
        }

        protected override float ColSpacing => 80;

        protected override ICollection<Widget> GetCol1(TIMSVehicleSpec spec)
        {
            return new Widget[]
            {
                this.CreateTIMSFlexButton("車 掌 情 報", 1, 2,
                    onClick: () =>
                    {
                        RequestBlock(TIMSBlockTypes.ChangeScreen, BlockingLevel.CurrentScreen, 1,
                            () => { Context.DisplayController.RequestChangeScreen(ScreenIds.C01AA); });
                    }),
                this.CreateTIMSFlexButton("異 常 扱 い", 1, 2),
                this.CreateTIMSFlexButton(
                    CreateComputed(() => RichTextParser.Raw(IsTIMSMain ? "運 行 情 報" : "")), 1, 2, clickable: IsTIMSMain),
                this.CreateTIMSFlexButton(
                    CreateComputed(() => RichTextParser.Raw(IsTIMSMain ? "非 常 換 気 制 御" : "")), 1, 2,
                    clickable: IsTIMSMain)
            };
        }

        protected override ICollection<Widget> GetCol2(TIMSVehicleSpec spec)
        {
            return new Widget[]
            {
                this.CreateTIMSFlexButton("行 先 設 定", 1, 2),
                this.CreateTIMSFlexButton(
                    CreateComputed(() => RichTextParser.Raw(IsTIMSMain ? "サ一ビス機器制御" : "")), 1, 2,
                    clickable: IsTIMSMain),
                this.CreateTIMSFlexButton(
                    CreateComputed(() => RichTextParser.Raw(IsTIMSMain ? "案 内 設 定" : "")), 1, 2, clickable: IsTIMSMain),
                this.CreateTIMSFlexButton(
                    CreateComputed(() => RichTextParser.Raw(IsTIMSMain ? "案 内 簡 易 設 定" : "")), 1, 2,
                    clickable: IsTIMSMain)
            };
        }

        protected override ICollection<Widget> GetCol3(TIMSVehicleSpec spec)
        {
            return null;
        }
    }

    public class C00AAButtonGroup5000 : C00AAButtonGroup
    {
        public C00AAButtonGroup5000(RenderContext context, TIMSVehicleSpec spec) : base(context, spec)
        {
        }

        protected override ICollection<Widget> GetCol3(TIMSVehicleSpec spec)
        {
            return new Widget[]
            {
                this.CreateTIMSFlexButton("Ｖ Ｉ Ｓ 状 態", 1, 2),
                this.CreateTIMSFlexButton(
                    CreateComputed(() => RichTextParser.Raw(IsTIMSMain ? "異 常 時 案 内" : "")), 1, 2,
                    clickable: IsTIMSMain)
            };
        }
    }
}