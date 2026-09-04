using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using JREMonitors.Core.Boosters;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Shadows;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Constants;
using JREMonitors.E233.ViewModels;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.E233.Lamps
{
    public class LampPanel : Widget<LampPanelViewModel>
    {
        private static readonly DropShadow[] PanelShadows =
        {
            new DropShadow
                { OffsetX = -2, OffsetY = 0, BlurX = 2, BlurY = 0, Color = new Color4(0, 0, 0, 100) },
            new DropShadow
                { OffsetX = 0, OffsetY = 3, BlurX = 0, BlurY = 3, Color = new Color4(0, 0, 0, 200) }
        };

        private readonly Baker _baker;
        private readonly float _borderRadius;
        private readonly ID2D1PathGeometry _geometry;

        public LampPanel(
            RenderContext context,
            float x,
            float baseWidth,
            float baseHeight,
            float extraWidth = 0,
            float extraHeight = 0,
            IEnumerable<Widget> children = null,
            float borderRadius = 8,
            bool clickable = true,
            bool keepLampOffWhenFirstRender = false
        ) : base(context, x)
        {
            ViewModel = new LampPanelViewModel(clickable);
            _borderRadius = borderRadius;
            BaseWidth = baseWidth;
            BaseHeight = baseHeight;
            ExtraHeight = extraHeight;
            ExtraWidth = extraWidth;

            if (children != null)
            {
                foreach (var child in children) AddChild(child, true);
                foreach (var child in GetChildrenDeep())
                    if (child is Lamp lamp)
                        lamp.KeepOffWhenFirstRender = false;
            }

            _baker = new Baker(context);
            RegisterResource(_baker);
            _geometry = CreateGeometry();
            RegisterResource(_geometry);
        }

        public override float? RefreshSpeed => RefreshSpeeds.Fast;

        public float BaseWidth { get; }
        public float BaseHeight { get; }
        public float ExtraHeight { get; }
        public float ExtraWidth { get; }

        private float EffectiveExtraWidth => ExtraHeight > 0 ? ExtraWidth : 0;

        public override RectangleF SelfRelativeDirtyBounds
        {
            get
            {
                var shadowWidth = PanelShadows.Max(shadow => shadow.MaxSpread);
                var left = -BaseWidth - (ExtraHeight > 0 ? ExtraWidth : 0) - shadowWidth;
                var bottom = BaseHeight + shadowWidth;
                return RectangleF.FromLTRB(left, 0, 1 + shadowWidth, bottom);
            }
        }

        protected override IList<RectangleF> LocalClickBoundsList
        {
            get
            {
                var list = new List<RectangleF>
                {
                    new RectangleF(-BaseWidth + 1, 0, BaseWidth, BaseHeight)
                };
                var extraBounds = new RectangleF(-BaseWidth - EffectiveExtraWidth + 1, 0, EffectiveExtraWidth,
                    ExtraHeight);
                if (!extraBounds.IsEmpty) list.Add(extraBounds);

                return list;
            }
        }

        public bool Clickable
        {
            get => ViewModel.Clickable;
            set => ViewModel.Clickable = value;
        }

        public event Action OnClick
        {
            add => ViewModel.OnClick += value;
            remove => ViewModel.OnClick -= value;
        }

        private ID2D1PathGeometry CreateGeometry()
        {
            var geometry = Context.D2D1Factory.CreatePathGeometry();
            using (var sink = geometry.Open())
            {
                sink.BeginFigure(new Vector2(1, 0), FigureBegin.Filled);
                var effectiveExtraWidth = EffectiveExtraWidth;
                if (ExtraHeight > 0)
                {
                    sink.AddLine(new Vector2(-BaseWidth - effectiveExtraWidth + 1, 0));
                    var pos = new Vector2(-BaseWidth - effectiveExtraWidth + 1, ExtraHeight - _borderRadius - 1);
                    sink.AddLine(pos);
                    sink.AddRoundedCorner(pos,
                        new Vector2(-(BaseWidth + effectiveExtraWidth - _borderRadius) + 1, ExtraHeight - 1), false);
                    sink.AddLine(new Vector2(-BaseWidth + 1, ExtraHeight - 1));
                }
                else
                {
                    sink.AddLine(new Vector2(-BaseWidth + 1, 0));
                }

                var pos2 = new Vector2(-BaseWidth + 1, BaseHeight - _borderRadius - 1);
                sink.AddLine(pos2);
                sink.AddRoundedCorner(pos2, new Vector2(-BaseWidth + _borderRadius + 1, BaseHeight - 1), false);
                sink.AddLine(new Vector2(1, BaseHeight - 1));
                sink.EndFigure(FigureEnd.Closed);
                sink.Close();
            }

            return geometry;
        }

        protected override void OnStaticWarmUp(float totalScale)
        {
            _baker.BakeAndDraw(SelfRelativeDirtyBounds, Draw);
        }

        protected override void OnDraw(float totalScale)
        {
            _baker.BakeAndDraw(SelfRelativeDirtyBounds, Draw);
        }

        private void Draw()
        {
            Context.DropShadowProcessor.DrawWithDropShadows(PanelShadows,
                () =>
                {
                    Context.CommonBrush.Color = MonitorColors.PanelColor;
                    Context.DeviceContext.FillGeometry(_geometry, Context.CommonBrush);
                });
        }

        protected override bool OnPointerDown(Vector2 localPoint)
        {
            ViewModel.TryClick();
            return true;
        }
    }
}