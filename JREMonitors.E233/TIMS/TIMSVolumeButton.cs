using System;
using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.State;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Buttons;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS
{
    public class TIMSVolumeButton : Widget<VolumeButtonViewModel>
    {
        private readonly CycleDrawerIconSwitchButton _iconButton;

        public TIMSVolumeButton(RenderContext context, Vector2 pos, LayoutLength width, LayoutLength height)
            : base(context, pos.X, pos.Y)
        {
            ViewModel = new VolumeButtonViewModel();
            _iconButton = new CycleDrawerIconSwitchButton(
                context,
                Vector2.Zero,
                width,
                height,
                context.TIMS().FooterIconButtonStyle,
                new SpeakerIconDrawer(context),
                new[] { 1f, 0.6f, 0.2f },
                () => ViewModel.Volume,
                val => ViewModel.Volume = val
            );
            AddChild(_iconButton);
        }

        public override RectangleF SelfRelativeDirtyBounds => _iconButton.SelfRelativeDirtyBounds;

        #region Nested SpeakerIconDrawer

        private class SpeakerIconDrawer : LevelIconDrawer
        {
            private const float IconWidth = 16f;
            private const float IconHeight = 15f;

            public SpeakerIconDrawer(RenderContext context)
                : base(context, new SizeF(IconWidth, IconHeight))
            {
            }

            protected override void DrawIcon(float originX, float originY, Color4 color, int level)
            {
                var dc = Context.DeviceContext;
                var oldAntialiasMode = dc.AntialiasMode;
                dc.AntialiasMode = AntialiasMode.Aliased;
                Context.CommonBrush.Color = color;
                var oldTransform = dc.Transform;
                dc.Transform = Matrix3x2.CreateTranslation(originX, originY) * oldTransform;
                dc.FillRectangle(new RectangleF(0, 4, 1, 7), Context.CommonBrush);
                dc.FillRectangle(new RectangleF(0, 4, 6, 1), Context.CommonBrush);
                dc.FillRectangle(new RectangleF(0, 10, 6, 1), Context.CommonBrush);
                dc.FillRectangle(new RectangleF(2, 5, 1, 5), Context.CommonBrush);
                dc.FillRectangle(new RectangleF(6, 3, 3, 1), Context.CommonBrush);
                dc.FillRectangle(new RectangleF(9, 2, 2, 1), Context.CommonBrush);
                dc.FillRectangle(new RectangleF(11, 1, 2, 1), Context.CommonBrush);
                dc.FillRectangle(new RectangleF(13, 0, 3, 1), Context.CommonBrush);
                dc.FillRectangle(new RectangleF(6, 11, 3, 1), Context.CommonBrush);
                dc.FillRectangle(new RectangleF(9, 12, 2, 1), Context.CommonBrush);
                dc.FillRectangle(new RectangleF(11, 13, 2, 1), Context.CommonBrush);
                dc.FillRectangle(new RectangleF(13, 14, 3, 1), Context.CommonBrush);
                dc.FillRectangle(new RectangleF(15, 1, 1, 13), Context.CommonBrush);
                if (level >= 1)
                {
                    dc.FillRectangle(new RectangleF(3, 5, 3, 5), Context.CommonBrush);
                    dc.FillRectangle(new RectangleF(6, 4, 4, 7), Context.CommonBrush);
                    dc.FillRectangle(new RectangleF(9, 3, 1, 1), Context.CommonBrush);
                    dc.FillRectangle(new RectangleF(9, 11, 1, 1), Context.CommonBrush);
                }

                if (level >= 2)
                {
                    dc.FillRectangle(new RectangleF(13, 1, 2, 1), Context.CommonBrush);
                    dc.FillRectangle(new RectangleF(11, 2, 4, 1), Context.CommonBrush);
                    dc.FillRectangle(new RectangleF(10, 3, 5, 9), Context.CommonBrush);
                    dc.FillRectangle(new RectangleF(11, 12, 4, 1), Context.CommonBrush);
                    dc.FillRectangle(new RectangleF(13, 13, 2, 1), Context.CommonBrush);
                }

                dc.Transform = oldTransform;
                dc.AntialiasMode = oldAntialiasMode;
            }
        }

        #endregion
    }

    public class VolumeButtonViewModel : ViewModel
    {
        private readonly Signal<float> _volume = new Signal<float>();
        private MonitorSoundController _monitorSoundController;

        public float Volume
        {
            get => _volume.Value;
            set
            {
                if (_monitorSoundController == null) return;
                _monitorSoundController.Volume = value;
            }
        }

        protected override void OnInitialize(DataHub dataHub)
        {
            _monitorSoundController = dataHub.GetOrNull<MonitorSoundController>();
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            if (_monitorSoundController != null) _volume.Value = _monitorSoundController.Volume;
        }
    }
}