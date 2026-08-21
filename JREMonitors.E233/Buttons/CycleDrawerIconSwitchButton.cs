using System;
using System.Drawing;
using System.Numerics;
using JREMonitors.Core.Constants;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Widgets;

namespace JREMonitors.E233.Buttons
{
    public class CycleDrawerIconSwitchButton : Widget, IButton
    {
        private readonly IButton _button;
        private readonly LevelIconDrawer _drawer;
        private readonly bool _reverseLevelMapping;
        private readonly Func<float> _sourceGetter;
        private readonly Action<float> _sourceSetter;
        private readonly float[] _stepValues;

        private CycleDrawerIconSwitchButton(
            RenderContext context,
            Vector2 pos,
            LevelIconDrawer drawer,
            float[] stepValues,
            Func<float> sourceGetter,
            Action<float> sourceSetter,
            bool reverseLevelMapping
        ) : base(context, pos.X, pos.Y)
        {
            _drawer = drawer ?? throw new ArgumentNullException(nameof(drawer));
            _stepValues = stepValues ?? throw new ArgumentNullException(nameof(stepValues));
            _sourceGetter = sourceGetter ?? throw new ArgumentNullException(nameof(sourceGetter));
            _sourceSetter = sourceSetter ?? throw new ArgumentNullException(nameof(sourceSetter));
            _reverseLevelMapping = reverseLevelMapping;
            RegisterResource(_drawer);
            WatchEffect(_drawer);
        }

        public CycleDrawerIconSwitchButton(
            RenderContext context,
            Vector2 pos,
            LayoutLength width,
            LayoutLength height,
            TIMSButtonStyle style,
            LevelIconDrawer drawer,
            float[] stepValues,
            Func<float> sourceGetter,
            Action<float> sourceSetter,
            bool reverseLevelMapping = true
        ) : this(context, pos, drawer, stepValues, sourceGetter, sourceSetter, reverseLevelMapping)
        {
            _button = new TIMSButton(context, Vector2.Zero, width, height, style, _drawer);
            _button.OnClick += CycleStep;
            AddChild((Widget)_button);
        }

        public CycleDrawerIconSwitchButton(
            RenderContext context,
            Vector2 pos,
            LayoutLength width,
            LayoutLength height,
            VectorButtonStyle style,
            LevelIconDrawer drawer,
            float[] stepValues,
            Func<float> sourceGetter,
            Action<float> sourceSetter,
            bool reverseLevelMapping = true
        ) : this(context, pos, drawer, stepValues, sourceGetter, sourceSetter, reverseLevelMapping)
        {
            _button = new VectorButton(context, Vector2.Zero, width, height, style, _drawer);
            _button.OnClick += CycleStep;
            AddChild((Widget)_button);
        }

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

        private int GetStepIndexFromSource()
        {
            var value = _sourceGetter();
            for (var i = 0; i < _stepValues.Length; i++)
                if (value >= _stepValues[i] - Epsilons.FloatEpsilon)
                    return i;

            return 0;
        }

        private void CycleStep()
        {
            var currentStepIndex = GetStepIndexFromSource();
            var nextStepIndex = (currentStepIndex + 1) % _stepValues.Length;
            _sourceSetter(_stepValues[nextStepIndex]);
        }

        protected override void OnUpdate(TimeSpan elapsed)
        {
            var stepIndex = GetStepIndexFromSource();
            var maxLevel = _stepValues.Length - 1;
            var level = _reverseLevelMapping ? maxLevel - stepIndex : stepIndex;
            _drawer.Level.Value = level;
        }

        protected override void OnDispose()
        {
            _button.OnClick -= CycleStep;
        }
    }
}