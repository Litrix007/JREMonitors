using System;
using System.Drawing;
using System.Linq;
using System.Numerics;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Svg;
using JREMonitors.Core.Widgets;

namespace JREMonitors.E233.Buttons
{
    public class CycleSvgIconSwitchButton : Widget, IButton
    {
        private readonly string[] _activePathIds;
        private readonly IButton _button;
        private readonly SvgBoundsDrawer _drawer;
        private readonly Func<float> _sourceGetter;
        private readonly Action<float> _sourceSetter;
        private readonly PropertySlot<int> _step;
        private readonly float[] _stepValues;

        private CycleSvgIconSwitchButton(
            RenderContext context,
            Vector2 pos,
            SvgDocumentProperties properties,
            float[] stepValues,
            string[] activePathIds,
            Func<float> sourceGetter,
            Action<float> sourceSetter
        ) : base(context, pos.X, pos.Y)
        {
            _stepValues = stepValues;
            _activePathIds = activePathIds;
            _sourceGetter = sourceGetter;
            _sourceSetter = sourceSetter;
            _drawer = new SvgBoundsDrawer(context, properties);
            _step = CreatePropertySlot<int>(DirtyType.Visual);
            WatchEffect(EffectPhase.State, () =>
            {
                var stepVal = _step.Value;
                var activePathId = _activePathIds[stepVal];
                var pathCount = PathIds.Length;
                for (var i = 0; i < pathCount; i++)
                    _drawer.Document.SetVisibility(PathIds[i], activePathId == PathIds[i]);
            });
        }

        public CycleSvgIconSwitchButton(
            RenderContext context,
            Vector2 pos,
            LayoutLength width,
            LayoutLength height,
            VectorButtonStyle style,
            SvgDocumentProperties properties,
            float[] stepValues,
            string[] activePathIds,
            Func<float> sourceGetter,
            Action<float> sourceSetter
        ) : this(context, pos, properties, stepValues, activePathIds, sourceGetter, sourceSetter)
        {
            _button = new VectorButton(context, Vector2.Zero, width, height, style, _drawer);
            _button.OnClick += CycleStep;
            AddChild((Widget)_button);
        }

        public CycleSvgIconSwitchButton(
            RenderContext context,
            Vector2 pos,
            LayoutLength width,
            LayoutLength height,
            TIMSButtonStyle style,
            SvgDocumentProperties properties,
            float[] stepValues,
            string[] activePathIds,
            Func<float> sourceGetter,
            Action<float> sourceSetter
        ) : this(context, pos, properties, stepValues, activePathIds, sourceGetter, sourceSetter)
        {
            _button = new TIMSButton(context, Vector2.Zero, width, height, style, _drawer);
            _button.OnClick += CycleStep;
            AddChild((Widget)_button);
        }

        private string[] PathIds => _activePathIds.Where(id => id != null).ToArray();

        public override RectangleF SelfRelativeDirtyBounds => RectangleF.Empty;

        public LayoutLength PreferredWidth => _button.PreferredWidth;
        public LayoutLength PreferredHeight => _button.PreferredHeight;
        public float MarginWidth => _button.MarginWidth;
        public float MarginHeight => _button.MarginHeight;
        public bool SkipArrangeWhenHidden => _button.SkipArrangeWhenHidden;
        public bool IncludeInTotalMajorDimensionSizeWhenVisible => _button.IncludeInTotalMajorDimensionSizeWhenVisible;
        public bool IncludeInTotalMajorDimensionSizeWhenHidden => _button.IncludeInTotalMajorDimensionSizeWhenHidden;

        public void SetLayoutSize(float width, float height)
        {
            _button.SetLayoutSize(width, height);
        }

        public event Action OnClick
        {
            add => _button.OnClick += value;
            remove => _button.OnClick -= value;
        }

        private int GetStep()
        {
            var value = _sourceGetter();
            for (var i = 0; i < _stepValues.Length; i++)
                if (value >= _stepValues[i])
                    return i;

            return 0;
        }

        private void CycleStep()
        {
            var nextStep = (_step.Value + 1) % _stepValues.Length;
            _step.Value = nextStep;
            _sourceSetter(_stepValues[nextStep]);
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            _step.Value = GetStep();
        }

        protected override void OnDispose()
        {
            _button.OnClick -= CycleStep;
        }
    }
}