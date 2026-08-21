using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Layouts.Text;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.TIMS.D01AX;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS.C01AX
{
    public class C01ABGreenCarInfo : Widget<C01AXCarInfoViewModel>
    {
        public C01ABGreenCarInfo(RenderContext context, TIMSVehicleSpec spec) : base(context)
        {
            ViewModel = new C01AXCarInfoViewModel(spec);
            var cars = new TIMSBigGreenCarWidget[TIMSFormationSpec.MaxGreenCarCount];
            var infoItems = new Col[TIMSFormationSpec.MaxGreenCarCount];

            for (var i = 0; i < TIMSFormationSpec.MaxGreenCarCount; i++)
            {
                var j = i;
                var greenCarSpec = CreateComputed<(int greenCarIdx, TIMSCarSpec car)?>(() =>
                {
                    var formationSpec = ViewModel.FormationSpec.Value;
                    if (formationSpec == null) return null;
                    var greenCarCount = formationSpec.GreenCarIndices.Count;
                    if (j >= greenCarCount) return null;

                    var greenCarSpecIdx = spec.GetCarIndex(greenCarCount, j);
                    var greenCarIdx = formationSpec.GreenCarIndices[greenCarSpecIdx];
                    return (greenCarIdx, formationSpec[greenCarIdx]);
                });

                var header = new BoundsDrawerWidget(context,
                    this.CreateTIMSTextDrawer("Ａ\u3000Ｂ\u3000Ｃ\u3000Ｄ", verticalAlignment: 0.5f),
                    contentColor: MonitorColors.White);
                header.CustomPreferredHeight.Value = LayoutLength.Flex();

                var texts = new List<Widget>
                {
                    header,
                    CreateText(new TextConfig(new Signal<string>("入"), new Signal<bool>(true),
                        new Signal<Color4>("#F5E4E0".ToColor4()),
                        new Signal<Color4>(MonitorColors.TIMSScreenBackground), LayoutLength.Flex(), true, true)),
                    CreateText(new TextConfig(new Signal<string>("入"), new Signal<bool>(true),
                        new Signal<Color4>("#F5E4E0".ToColor4()),
                        new Signal<Color4>(MonitorColors.TIMSScreenBackground), LayoutLength.Flex(), true, true)),
                    CreateText(new TextConfig(new Signal<string>("自"), new Signal<bool>(true),
                        new Signal<Color4>("#BCF1D4".ToColor4()),
                        new Signal<Color4>(MonitorColors.TIMSScreenBackground), LayoutLength.Flex(), true, true)),
                    CreateText(new TextConfig(new Signal<string>("有"),
                        CreateComputed(() => greenCarSpec.Value?.car.HasWaterTank ?? false),
                        new Signal<Color4>(Colors.Aqua),
                        new Signal<Color4>(MonitorColors.TIMSScreenBackground), LayoutLength.Flex(), true, true)),
                    new PlaceHolder(context)
                };

                var tempRow = new Row(context, widgetSpacing: 1, spreadWidthFlex: false,
                    positionSnapToPixels: true,
                    widgets: Enumerable.Range(0, 4)
                        .Select(k => CreateText(new TextConfig(CreateComputed(() =>
                            {
                                var greenCarIdx = greenCarSpec.Value?.greenCarIdx;
                                if (greenCarIdx == null) return "   0";
                                var temps = ViewModel.Temperatures[greenCarIdx.Value];
                                if (k >= temps.Count) return "   0";
                                return temps[k].ToString("F1").PadLeft(4);
                            }), CreateComputed(() =>
                            {
                                var greenCarIdx = greenCarSpec.Value?.greenCarIdx;
                                if (greenCarIdx == null) return false;
                                var temps = ViewModel.Temperatures[greenCarIdx.Value];
                                return k < temps.Count;
                            }), new Signal<Color4>(), new Signal<Color4>(MonitorColors.White),
                            LayoutLength.Absolute(40), true, false), true)).ToArray());
                texts.Add(tempRow);

                texts.AddRange(new[]
                {
                    CreateText(new TextConfig(CreateComputed(() =>
                        {
                            var greenCarIdx = greenCarSpec.Value?.greenCarIdx;
                            if (greenCarIdx == null) return "0";
                            return ViewModel.Humidities[greenCarIdx.Value].ToString();
                        }), new Signal<bool>(true),
                        new Signal<Color4>(), new Signal<Color4>(MonitorColors.White),
                        LayoutLength.Flex(), true, true)),
                    CreateText(new TextConfig(CreateComputed(() =>
                        {
                            var greenCarIdx = greenCarSpec.Value?.greenCarIdx;
                            if (greenCarIdx == null) return "0";
                            return ViewModel.LoadFactors[greenCarIdx.Value].ToString();
                        }), new Signal<bool>(true),
                        new Signal<Color4>(), new Signal<Color4>(MonitorColors.White),
                        LayoutLength.Flex(), true, true)),
                    CreateText(new TextConfig(CreateComputed(() =>
                        {
                            var greenCarIdx = greenCarSpec.Value?.greenCarIdx;
                            if (greenCarIdx == null) return "0";
                            return ViewModel.PassengerCounts[greenCarIdx.Value].ToString();
                        }), new Signal<bool>(true),
                        new Signal<Color4>(), new Signal<Color4>(MonitorColors.White),
                        LayoutLength.Flex(), true, true))
                });

                infoItems[j] = new Col(context, widgets: texts, widgetSpacing: 1, fallbackFlexUnitHeight: 21,
                    widgetHorizontalAlignment: 0.5f, spreadHeightFlex: false, positionSnapToPixels: true);
                var isVisible = CreateComputed(() => greenCarSpec.Value != null);
                infoItems[j].IsVisible.Bind(isVisible);
                infoItems[j].MarginWidth.Bind(CreateComputed(() =>
                    TIMSBigGreenCarWidget.Width - infoItems[j].PreferredWidth.Value.AbsoluteValue));
                cars[j] = new TIMSBigGreenCarWidget(context);
                cars[j].IsVisible.Bind(isVisible);
                cars[j].CarNumber.Bind(CreateComputed(() => greenCarSpec.Value?.car.CarNumber ?? 0));
            }

            var carRow = new Row(context, 400, 100, widgetSpacing: 100, rowHorizontalAlignment: 0.5f, widgets: cars,
                positionSnapToPixels: true);
            AddChild(carRow);

            AddChild(new BoundsDrawerWidget(context,
                this.CreateTIMSTextDrawer(this.CreateTIMSTextLayout("Ａ：後位車端室\nＢ：階上室\nＣ：階下室\nＤ：前位車端室",
                    fixedLineSpacing: 2,
                    arrangement: ContentArrangement.Near), horizontalAlignment: 0.5f, verticalAlignment: 0.5f),
                contentColor: MonitorColors.TIMSTitleGrey, strokeColor: MonitorColors.TIMSTitleGrey,
                targetBounds: new RectangleF(0, 0, 150, 100),
                strokeWidth: 1, strokeDashWidth: 6, strokeGapWidth: 2,
                backgroundAntialiasMode: AntialiasMode.Aliased, x: 640, y: D01AXBaseInfoGroup.SecondRowY));

            var titles = new List<Widget>
            {
                null,
                CreateTitle("放\u3000送"),
                CreateTitle("室内灯"),
                CreateTitle("空\u3000調"),
                CreateTitle("タンク水量", CreateComputed(() =>
                {
                    var formationSpec = ViewModel.FormationSpec.Value;
                    return formationSpec != null && formationSpec.HasWaterTank;
                })),
                null,
                CreateTitle("室温(℃)"),
                CreateTitle("湿度(％)"),
                CreateTitle("乗車率(％)"),
                CreateTitle("乗車人員(人)")
            };
            var titleCol = new Col(context, 35, 205, widgetSpacing: 1, fallbackFlexUnitHeight: 21, widgets: titles,
                positionSnapToPixels: true);
            AddChild(titleCol);

            var infoRow = new Row(context, 400, 205, widgetSpacing: 100, widgets: infoItems,
                rowHorizontalAlignment: 0.5f, positionSnapToPixels: true);
            AddChild(infoRow);
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
        public override float? RefreshSpeed => IsFirstUpdate ? RefreshSpeeds.Fast : (float?)null;

        private BoundsDrawerWidget CreateTitle(string title, IValueSignal<bool> isVisible = null)
        {
            var widget = new BoundsDrawerWidget(Context, this.CreateTIMSTextDrawer(title, verticalAlignment: 0.5f),
                contentColor: MonitorColors.TIMSTitleGrey);
            widget.CustomPreferredHeight.Value = LayoutLength.Flex();
            widget.SkipArrangeWhenHidden.Value = false;
            if (isVisible != null) widget.IsVisible.Bind(isVisible);

            return widget;
        }

        private BoundsDrawerWidget CreateText(TextConfig config, bool skipArrangeWhenHidden = false)
        {
            var drawer = this.CreateTIMSTextDrawer(
                CreateComputed(() => RichTextParser.Raw(config.Text.Value)),
                cache: config.Cache,
                horizontalAlignment: 0.5f, verticalAlignment: 0.5f);
            var textWidget = new BoundsDrawerWidget(Context,
                drawer,
                targetBounds: new RectangleF(0, 0, 40, 20),
                useBaker: true,
                strokeColor: MonitorColors.TIMSTitleGrey,
                strokeWidth: config.Stroke ? 1 : 0, backgroundAntialiasMode: AntialiasMode.Aliased,
                preferredWidth: config.PreferredWidth);
            textWidget.SkipArrangeWhenHidden.Value = skipArrangeWhenHidden;
            textWidget.IsVisible.Bind(config.IsVisible);
            textWidget.CustomPreferredHeight.Value = LayoutLength.Flex();
            textWidget.BackgroundColor.Bind(config.BackgroundColor);
            textWidget.ContentColor.Bind(config.ContentColor);
            return textWidget;
        }

        private struct TextConfig
        {
            public IValueSignal<string> Text { get; }
            public IValueSignal<Color4> BackgroundColor { get; }
            public IValueSignal<bool> IsVisible { get; }
            public IValueSignal<Color4> ContentColor { get; }
            public LayoutLength? PreferredWidth { get; }
            public bool Stroke { get; }
            public bool Cache { get; }

            public TextConfig(
                IValueSignal<string> text,
                IValueSignal<bool> isVisible,
                IValueSignal<Color4> backgroundColor,
                IValueSignal<Color4> contentColor,
                LayoutLength? preferredWidth,
                bool stroke,
                bool cache
            )
            {
                Text = text;
                IsVisible = isVisible;
                BackgroundColor = backgroundColor;
                ContentColor = contentColor;
                PreferredWidth = preferredWidth;
                Stroke = stroke;
                Cache = cache;
            }
        }
    }
}