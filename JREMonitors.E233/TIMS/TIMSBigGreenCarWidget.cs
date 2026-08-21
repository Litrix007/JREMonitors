using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Layouts.Text;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Services.Render;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Utils.Render;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using Vortice.Direct2D1;

namespace JREMonitors.E233.TIMS
{
    public class TIMSBigGreenCarWidget : Widget, ILayoutable
    {
        public const float Width = 138;
        public const float Height = 74;

        private static readonly float[] RoofPoints =
        {
            25, 9, 26, 8, 26, 7, 30, 3, 30, 2, 32, 0,
            104, 0, 106, 2, 106, 3, 110, 7, 110, 8, 111, 9
        };

        private static readonly float[] MiddlePartitionPoints =
        {
            25, 11, 26, 12, 26, 13, 30, 17, 30, 18, 34, 22, 34, 23, 38, 27,
            38, 28, 40, 30, 96, 30, 98, 32, 98, 33, 102, 37, 102, 38, 106, 42,
            106, 43, 110, 47, 110, 48, 111, 49
        };

        private static readonly float[] BottomBoundaryPoints =
        {
            25, 51, 26, 52, 26, 53, 30, 57, 30, 58, 32, 60,
            104, 60, 106, 58, 106, 57, 110, 53, 110, 52, 111, 51
        };

        private readonly ID2D1StrokeStyle _strokeStyle;

        public TIMSBigGreenCarWidget(RenderContext context, float x = 0, float y = 0) : base(context, x, y)
        {
            _strokeStyle = Context.D2D1Factory.CreateStrokeStyle(GeometryHelper.SquareStrokeStyleProperties);
            RegisterResource(_strokeStyle);
            CarNumber = CreatePropertySlot<int>(DirtyType.Visual);
            MarginWidth = CreatePropertySlot<float>(DirtyType.Layout);
            MarginHeight = CreatePropertySlot<float>(DirtyType.Layout);
            AddChild(new BoundsDrawerWidget(context,
                this.CreateTIMSTextDrawer(CreateComputed(() =>
                        RichTextParser.Raw(CarNumber.Value.ToString().ToFullWidth() + "号車")), horizontalAlignment: 1,
                    verticalAlignment: 1),
                contentColor: MonitorColors.TIMSTitleGrey, x: 22, y: 9));
        }

        public PropertySlot<int> CarNumber { get; }
        public override RectangleF SelfRelativeDirtyBounds => new RectangleF(0, 0, Width, Height);
        public PropertySlot<float> MarginWidth { get; }
        public PropertySlot<float> MarginHeight { get; }
        public LayoutLength PreferredWidth { get; } = LayoutLength.Absolute(Width);
        public LayoutLength PreferredHeight { get; } = LayoutLength.Absolute(Height);
        float ILayoutable.MarginWidth => MarginWidth;
        float ILayoutable.MarginHeight => MarginHeight;
        public bool SkipArrangeWhenHidden => true;
        public bool IncludeInTotalMajorDimensionSizeWhenVisible => true;
        public bool IncludeInTotalMajorDimensionSizeWhenHidden => false;

        public void SetLayoutSize(float width, float height)
        {
        }

        protected override void OnDraw(float totalScale)
        {
            var baker = Context.GetBakerCache()
                .GetOrCreateBaker<TIMSBigGreenCarWidget, int>(Context, 0, BakerPrescaleMode.AutoCubic);
            baker.BakeAndDraw(SelfRelativeDirtyBounds, DrawContent);
        }

        private void DrawContent()
        {
            var oldAntialiasMode = Context.DeviceContext.AntialiasMode;
            Context.DeviceContext.AntialiasMode = AntialiasMode.Aliased;
            Context.CommonBrush.Color = MonitorColors.TIMSTitleGrey;
            const float wheelRadius = 11f;
            const float wheelCenterY = 61.5f;
            Context.DeviceContext.DrawEllipse(new Ellipse(new Vector2(12.5f, wheelCenterY), wheelRadius, wheelRadius),
                Context.CommonBrush);
            Context.DeviceContext.DrawEllipse(new Ellipse(new Vector2(124.5f, wheelCenterY), wheelRadius, wheelRadius),
                Context.CommonBrush);
            Context.DeviceContext.DrawRectangle(new RectangleF(0.5f, 10.5f, 24, 40), Context.CommonBrush, 1,
                _strokeStyle);
            Context.DeviceContext.DrawRectangle(new RectangleF(112.5f, 10.5f, 24, 40), Context.CommonBrush, 1,
                _strokeStyle);
            Context.DeviceContext.DrawAliasedPolyLine(Context.CommonBrush, _strokeStyle, RoofPoints);
            Context.DeviceContext.DrawAliasedPolyLine(Context.CommonBrush, _strokeStyle, MiddlePartitionPoints);
            Context.DeviceContext.DrawAliasedPolyLine(Context.CommonBrush, _strokeStyle, BottomBoundaryPoints);
            var format = Context.TIMS().Format18;
            Context.DeviceContext.DrawDynamicText(Context.DwFactory, "Ａ", 13, 30, format, Context.CommonBrush, 0.5f,
                0.5f);
            Context.DeviceContext.DrawDynamicText(Context.DwFactory, "Ｂ", 68, 15, format, Context.CommonBrush, 0.5f,
                0.5f);
            Context.DeviceContext.DrawDynamicText(Context.DwFactory, "Ｃ", 68, 45, format, Context.CommonBrush, 0.5f,
                0.5f);
            Context.DeviceContext.DrawDynamicText(Context.DwFactory, "Ｄ", 125, 30, format, Context.CommonBrush, 0.5f,
                0.5f);
            Context.DeviceContext.AntialiasMode = oldAntialiasMode;
        }
    }
}