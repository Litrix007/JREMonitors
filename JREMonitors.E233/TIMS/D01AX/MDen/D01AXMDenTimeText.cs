using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Layouts.Text;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS.D01AX.MDen
{
    public class D01AXMDenTimeText : Widget
    {
        private readonly Arrow _arrow;
        private readonly BoundsDrawerWidget _indicatorText;
        private readonly BoundsDrawerWidget _text;

        public D01AXMDenTimeText(
            RenderContext context,
            ScopedRenderContext scopedContext,
            float x,
            float y,
            float indicatorRelativeX,
            IValueSignal<string> hoursAndMinutes,
            IValueSignal<string> seconds,
            IValueSignal<string> indicator,
            IValueSignal<bool> showArrow,
            IValueSignal<Color4> textColor,
            IValueSignal<Color4> indicatorColor
        ) : base(context, x, y)
        {
            var hasIndicatorText = CreateComputed(() => !string.IsNullOrEmpty(indicator.Value));
            _text = new BoundsDrawerWidget(scopedContext,
                this.CreateTIMSTextDrawer(
                    new[]
                    {
                        new BitmapScaleDrawer.DrawerProperties(
                            this.CreateTIMSTextLayout(context: scopedContext,
                                documentSource: CreateComputed(() => RichTextParser.Raw(hoursAndMinutes.Value))), 2),
                        new BitmapScaleDrawer.DrawerProperties(
                            this.CreateTIMSTextLayout(context: scopedContext,
                                documentSource: CreateComputed(() => RichTextParser.Raw(seconds.Value)),
                                format: scopedContext.FontManager.GetOrCreateFormat(Fonts.MsGothicFamily, 12,
                                    fontWeight: FontWeight.Bold)), 1, 1,
                            context.TIMS().TimeTableSecondsOffsetY
                        )
                    }, context: scopedContext, arrangement: ContentArrangement.Near),
                contentColor: MonitorColors.White);
            _text.IsVisible.Bind(CreateComputed(() => !string.IsNullOrEmpty(hoursAndMinutes.Value)
                                                      && !string.IsNullOrEmpty(seconds.Value)
                                                      && (!hasIndicatorText.Value || showArrow.Value)));
            _text.ContentColor.Bind(textColor);
            AddChild(_text);
            // 文字固定
            _indicatorText = new BoundsDrawerWidget(context,
                this.CreateTIMSTextDrawer(CreateComputed(() => RichTextParser.Raw(indicator.Value))),
                contentColor: MonitorColors.White,
                x: indicatorRelativeX);
            _indicatorText.IsVisible.Bind(CreateComputed(() => hasIndicatorText.Value && !showArrow.Value));
            _indicatorText.ContentColor.Bind(indicatorColor);
            AddChild(_indicatorText);
            _arrow = new Arrow(context, indicatorRelativeX);
            _arrow.IsVisible.Bind(showArrow);
            _arrow.Color.Bind(textColor);
            AddChild(_arrow);
        }

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;

        private class Arrow : Widget
        {
            private readonly Baker _baker;
            private readonly ID2D1PathGeometry _geometry;

            public Arrow(RenderContext context, float x) : base(context, x)
            {
                Color = CreatePropertySlot<Color4>(DirtyType.Visual);
                _geometry = CreateGeometry();
                RegisterResource(_geometry);
                _baker = new Baker(context, BakerPrescaleMode.AutoCubic);
                RegisterResource(_baker);
                WatchEffect(() => _baker.Refresh(), Color);
            }

            public PropertySlot<Color4> Color { get; }

            public override RectangleF SelfRelativeDirtyBounds => new RectangleF(0, 0, 17, 18);

            private ID2D1PathGeometry CreateGeometry()
            {
                var geometry = Context.D2D1Factory.CreatePathGeometry();
                using (var sink = geometry.Open())
                {
                    sink.BeginFigure(new Vector2(4.5f, 1.5f), FigureBegin.Filled);
                    sink.AddLine(new Vector2(12.5f, 1.5f));
                    sink.AddLine(new Vector2(12.5f, 9.5f));
                    sink.AddLine(new Vector2(16.5f, 9.5f));
                    sink.AddLine(new Vector2(8.5f, 17.5f));
                    sink.AddLine(new Vector2(0.5f, 9.5f));
                    sink.AddLine(new Vector2(4.5f, 9.5f));
                    sink.EndFigure(FigureEnd.Closed);
                    sink.Close();
                }

                return geometry;
            }

            protected override void OnDraw(float totalScale)
            {
                _baker.BakeAndDraw(SelfRelativeDirtyBounds, Draw);
            }

            private void Draw()
            {
                var oldAntialiasMode = Context.DeviceContext.AntialiasMode;
                Context.DeviceContext.AntialiasMode = AntialiasMode.Aliased;
                Context.CommonBrush.Color = Color;
                Context.DeviceContext.DrawGeometry(_geometry, Context.CommonBrush, 1.0f);
                Context.DeviceContext.AntialiasMode = oldAntialiasMode;
            }
        }
    }
}