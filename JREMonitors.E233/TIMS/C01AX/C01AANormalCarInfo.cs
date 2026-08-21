using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Layouts.Text;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Utils.Render;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS.C01AX
{
    public class C01AANormalCarInfo : Widget<C01AXCarInfoViewModel>
    {
        public C01AANormalCarInfo(RenderContext context, TIMSVehicleSpec spec) : base(context)
        {
            ViewModel = new C01AXCarInfoViewModel(spec);
            FirstCarX = CreateRelayPropertySlot<float>();
            var items = new List<Widget>
            {
                new Item(context, spec, "放\u3000送", FirstCarX, ViewModel.FormationSpec, i =>
                    new TextConfig(new Signal<string>("入"), new Signal<bool>(true),
                        new Signal<Color4>("#F5E4E0".ToColor4()),
                        new Signal<Color4>(MonitorColors.TIMSScreenBackground), true, true)),
                new Item(context, spec, "室内灯", FirstCarX, ViewModel.FormationSpec, i =>
                    new TextConfig(new Signal<string>("入"), new Signal<bool>(true),
                        new Signal<Color4>("#F5E4E0".ToColor4()),
                        new Signal<Color4>(MonitorColors.TIMSScreenBackground), true, true)),
                new Item(context, spec, "空\u3000調", FirstCarX, ViewModel.FormationSpec, i =>
                    new TextConfig(new Signal<string>("自"), new Signal<bool>(true),
                        new Signal<Color4>("#BCF1D4".ToColor4()),
                        new Signal<Color4>(MonitorColors.TIMSScreenBackground), true, true)),
                new Item(context, spec, "送\u3000風", FirstCarX, ViewModel.FormationSpec, i =>
                    new TextConfig(new Signal<string>("自"), CreateComputed(() =>
                        {
                            var formationSpec = ViewModel.FormationSpec.Value;
                            if (formationSpec == null || i >= formationSpec.CarCount) return false;
                            var carIdx = spec.GetCarIndex(formationSpec.CarCount, i);
                            return formationSpec[carIdx].CarType != TIMSCarType.GreenCar;
                        }),
                        new Signal<Color4>("#BCF1D4".ToColor4()),
                        new Signal<Color4>(MonitorColors.TIMSScreenBackground), true, true))
            };

            var waterTankItem = new Item(context, spec, "タンク水量", FirstCarX, ViewModel.FormationSpec, i =>
                new TextConfig(new Signal<string>("有"), CreateComputed(() =>
                    {
                        var formationSpec = ViewModel.FormationSpec.Value;
                        if (formationSpec == null || i >= formationSpec.CarCount) return false;
                        var carIdx = spec.GetCarIndex(formationSpec.CarCount, i);
                        return formationSpec[carIdx].HasWaterTank;
                    }),
                    new Signal<Color4>(Colors.Aqua),
                    new Signal<Color4>(MonitorColors.TIMSScreenBackground), true, true));
            waterTankItem.IsVisible.Bind(CreateComputed(() =>
            {
                var formationSpec = ViewModel.FormationSpec.Value;
                return formationSpec != null && formationSpec.HasWaterTank;
            }));
            items.Add(waterTankItem);

            items.AddRange(new[]
            {
                new Item(context, spec, "室温(℃)", FirstCarX, ViewModel.FormationSpec, i =>
                    new TextConfig(
                        CreateComputed(() =>
                        {
                            var formationSpec = ViewModel.FormationSpec.Value;
                            if (formationSpec == null || i >= formationSpec.CarCount) return string.Empty;
                            var carIdx = spec.GetCarIndex(formationSpec.CarCount, i);
                            var temps = ViewModel.Temperatures[carIdx];
                            if (temps.Count == 1) return temps[0].ToString("F1");
                            if (temps.Count > 1) return "※";
                            return string.Empty;
                        }),
                        new Signal<bool>(true),
                        new Signal<Color4>(),
                        CreateComputed(() =>
                        {
                            var formationSpec = ViewModel.FormationSpec.Value;
                            if (formationSpec == null || i >= formationSpec.CarCount) return MonitorColors.White;
                            var carIdx = spec.GetCarIndex(formationSpec.CarCount, i);
                            var temps = ViewModel.Temperatures[carIdx];
                            return temps.Count > 1 ? MonitorColors.TIMSTitleGrey : MonitorColors.White;
                        }),
                        true,
                        false,
                        CreateComputed(() =>
                        {
                            var formationSpec = ViewModel.FormationSpec.Value;
                            if (formationSpec == null || i >= formationSpec.CarCount) return false;
                            var carIdx = spec.GetCarIndex(formationSpec.CarCount, i);
                            return ViewModel.Temperatures[carIdx].Count > 1;
                        })
                    )),
                new Item(context, spec, "湿度(％)", FirstCarX, ViewModel.FormationSpec, i =>
                        new TextConfig(
                            CreateComputed(() =>
                            {
                                var formationSpec = ViewModel.FormationSpec.Value;
                                if (formationSpec == null || i >= formationSpec.CarCount) return string.Empty;
                                var carIdx = spec.GetCarIndex(formationSpec.CarCount, i);
                                var hum = ViewModel.Humidities[carIdx];
                                return (hum > 0 ? hum.ToString() : string.Empty).PadLeft(4);
                            }),
                            new Signal<bool>(true),
                            new Signal<Color4>(),
                            new Signal<Color4>(MonitorColors.White),
                            true,
                            false
                        ),
                    new TextConfig(new Signal<string>("編成計"), new Signal<bool>(spec.C01AASpec.CountPassengers),
                        new Signal<Color4>(),
                        new Signal<Color4>(MonitorColors.TIMSTitleGrey), false, true)),
                new Item(context, spec, "乗車率(％)", FirstCarX, ViewModel.FormationSpec, i =>
                        new TextConfig(
                            CreateComputed(() =>
                            {
                                var formationSpec = ViewModel.FormationSpec.Value;
                                if (formationSpec == null || i >= formationSpec.CarCount) return string.Empty;
                                var carIdx = spec.GetCarIndex(formationSpec.CarCount, i);
                                return ViewModel.LoadFactors[carIdx].ToString().PadLeft(4);
                            }),
                            new Signal<bool>(true),
                            new Signal<Color4>(), new Signal<Color4>(MonitorColors.White), true, false
                        ),
                    new TextConfig(
                        CreateComputed(() => ViewModel.AverageLoadFactor.Value.ToString().PadLeft(4)),
                        new Signal<bool>(spec.C01AASpec.CountPassengers),
                        new Signal<Color4>(),
                        new Signal<Color4>(MonitorColors.White),
                        true,
                        false
                    )),
                new Item(context, spec, "乗車人員(人)", FirstCarX, ViewModel.FormationSpec, i =>
                        new TextConfig(
                            CreateComputed(() =>
                            {
                                var formationSpec = ViewModel.FormationSpec.Value;
                                if (formationSpec == null || i >= formationSpec.CarCount) return string.Empty;
                                var carIdx = spec.GetCarIndex(formationSpec.CarCount, i);
                                return ViewModel.PassengerCounts[carIdx].ToString().PadLeft(4);
                            }),
                            new Signal<bool>(true),
                            new Signal<Color4>(),
                            new Signal<Color4>(MonitorColors.White),
                            true,
                            false
                        ),
                    new TextConfig(
                        CreateComputed(() => ViewModel.TotalPassengerCount.Value.ToString().PadLeft(4)),
                        new Signal<bool>(spec.C01AASpec.CountPassengers),
                        new Signal<Color4>(),
                        new Signal<Color4>(MonitorColors.White),
                        true,
                        false
                    ))
            });

            var col = new Col(context, widgetSpacing: 2, widgets: items, positionSnapToPixels: true);
            col.Y.Bind(CreateComputed<float>(() =>
            {
                var formationSpec = ViewModel.FormationSpec.Value;
                if (formationSpec != null && formationSpec.HasGreenCar) return 15;
                return 20;
            }));
            AddChild(col);
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;
        public override float? RefreshSpeed => IsFirstUpdate ? RefreshSpeeds.Fast : (float?)null;
        protected override bool MergeChildrenDirtyBounds => IsFirstUpdate;

        public PropertySlot<float> FirstCarX { get; }

        protected override void OnDraw(float totalScale)
        {
            Context.DeviceContext.WithAliasedIfNeeded(() =>
            {
                Context.CommonBrush.Color = Colors.Black;
                Context.DeviceContext.FillRectangle(SelfRelativeDirtyBounds, Context.CommonBrush);
            });
        }

        private struct TextConfig
        {
            public IValueSignal<string> Text { get; }
            public IValueSignal<bool> IsVisible { get; }
            public IValueSignal<Color4> BackgroundColor { get; }
            public IValueSignal<Color4> ContentColor { get; }
            public bool Stroke { get; }
            public bool Cache { get; }
            public IValueSignal<bool> UseHorizontalOverhangMetrics { get; }

            public TextConfig(
                IValueSignal<string> text,
                IValueSignal<bool> isVisible,
                IValueSignal<Color4> backgroundColor,
                IValueSignal<Color4> contentColor,
                bool stroke,
                bool cache,
                IValueSignal<bool> useHorizontalOverhangMetrics = null
            )
            {
                Text = text;
                IsVisible = isVisible;
                BackgroundColor = backgroundColor;
                ContentColor = contentColor;
                Stroke = stroke;
                Cache = cache;
                UseHorizontalOverhangMetrics = useHorizontalOverhangMetrics;
            }
        }

        private class Item : Widget, ILayoutable
        {
            public Item(
                RenderContext context,
                TIMSVehicleSpec spec,
                string title,
                IValueSignal<float> firstCarX,
                IValueSignal<TIMSFormationSpec> formationSpecSource,
                Func<int, TextConfig> textConfigFactory,
                TextConfig? externalConfig = null
            ) : base(context)
            {
                var titleText = new BoundsDrawerWidget(context,
                    this.CreateTIMSTextDrawer(title, verticalAlignment: 0.5f),
                    targetBounds: new RectangleF(0, 0, 0, 20), contentColor: MonitorColors.TIMSTitleGrey, x: 20);
                AddChild(titleText);
                var textWidgets = Enumerable.Range(0, TIMSFormationSpec.MaxCarCount).Select(textConfigFactory)
                    .Select((config, i) =>
                    {
                        var widget = CreateText(spec, config);
                        widget.SkipArrangeWhenHidden.Bind(CreateComputed(() =>
                        {
                            var formationSpec = formationSpecSource.Value;
                            return formationSpec != null && i >= formationSpec.CarCount;
                        }));
                        widget.IsVisible.Bind(CreateComputed(() =>
                        {
                            var formationSpec = formationSpecSource.Value;
                            return config.IsVisible.Value && formationSpec != null && i < formationSpec.CarCount;
                        }));
                        return (Widget)widget;
                    }).ToList();
                if (externalConfig != null)
                {
                    var config = externalConfig.Value;
                    var textWidget = CreateText(spec, config);
                    textWidget.IncludeInTotalMajorDimensionSizeWhenVisible.Value = false;
                    textWidget.IsVisible.Bind(config.IsVisible);
                    textWidgets.Add(textWidget);
                }

                var row = new Row(context, widgetSpacing: 1, widgets: textWidgets, positionSnapToPixels: true);
                row.X.Bind(firstCarX);
                AddChild(row);
            }

            public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;

            public LayoutLength PreferredWidth => LayoutLength.Absolute(0);
            public LayoutLength PreferredHeight => LayoutLength.Absolute(20);
            public float MarginWidth => 0;
            public float MarginHeight => 0;
            public bool IncludeInTotalMajorDimensionSizeWhenVisible => true;
            public bool IncludeInTotalMajorDimensionSizeWhenHidden => false;
            public bool SkipArrangeWhenHidden => true;

            public void SetLayoutSize(float width, float height)
            {
            }

            private BoundsDrawerWidget CreateText(TIMSVehicleSpec spec, TextConfig config)
            {
                var layout = this.CreateTIMSTextLayout(
                    documentSource: CreateComputed(() => RichTextParser.Raw(config.Text.Value)),
                    cacheMetrics: config.Cache);
                if (config.UseHorizontalOverhangMetrics != null)
                    layout.UseHorizontalOverhangMetrics.Bind(config.UseHorizontalOverhangMetrics);

                var drawer = this.CreateTIMSTextDrawer(
                    layout, cacheBaker: config.Cache,
                    horizontalAlignment: 0.5f, verticalAlignment: 0.5f);
                var textWidget = new BoundsDrawerWidget(Context,
                    drawer,
                    targetBounds: new RectangleF(0, 0, 40, 20),
                    useBaker: true,
                    strokeColor: MonitorColors.TIMSTitleGrey,
                    strokeWidth: config.Stroke ? 1 : 0, backgroundAntialiasMode: AntialiasMode.Aliased,
                    preferredWidth: LayoutLength.Absolute(40));
                textWidget.MarginWidth.Value = TIMSCarGroup.GetUnitWidth(spec) - 40;
                textWidget.BackgroundColor.Bind(config.BackgroundColor);
                textWidget.ContentColor.Bind(config.ContentColor);
                return textWidget;
            }
        }
    }
}