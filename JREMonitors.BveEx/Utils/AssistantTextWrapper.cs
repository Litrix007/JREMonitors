using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using BveEx.PluginHost;
using BveTypes.ClassWrappers;
using JREMonitors.BveEx.Configs.Runtime;
using Mackoy.Bvets;

namespace JREMonitors.BveEx.Utils
{
    public class AssistantTextWrapper : IDisposable
    {
        private const double AlertDurationMs = 1500;
        private const string DefaultName = "[JREMonitors]";
        private const int DefaultScale = 18;
        private readonly AssistantText _assistantText;
        private readonly IBveHacker _bveHacker;
        private readonly Color _initialBackgroundColor = Color.FromArgb(128, 0, 0, 0);
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private double _alertStartTime = -1;
        private bool _attached;
        private Color _targetBackgroundColor;

        public AssistantTextWrapper(IBveHacker bveHacker, InfoAssistantConfig config)
        {
            _bveHacker = bveHacker;
            _assistantText = new AssistantText(new AssistantSettings
            {
                Name = DefaultName,
                Anchor = (AnchorStyles)config.Anchor,
                AutoLocation = false,
                Visible = true,
                Location = new Point((int)config.Position.X, (int)config.Position.Y)
            })
            {
                BackgroundColor = _initialBackgroundColor,
                Color = Color.White,
                Text = string.Empty
            };
            SyncScale();
        }

        public string Text
        {
            get => _assistantText.Text;
            set => _assistantText.Text = value;
        }

        public int Anchor => (int)_assistantText.AssistantSettings.Anchor;

        public Vector2 Position => new Vector2(_assistantText.AssistantSettings.Location.X,
            _assistantText.AssistantSettings.Location.Y);

        public void Dispose()
        {
            Detach();
            _assistantText.Dispose();
        }

        public void SyncScale()
        {
            _assistantText.AssistantSettings.Scale =
                _bveHacker.Assistants.Items.FirstOrDefault()?.AssistantSettings.Scale ?? DefaultScale;
        }

        public void Attach()
        {
            if (_attached) return;
            _attached = true;
            _bveHacker.Assistants.Items.Add(_assistantText);
        }

        public void Detach()
        {
            if (!_attached) return;
            _attached = false;
            _bveHacker.Assistants.Items.Remove(_assistantText);
        }

        public void Tick()
        {
            if (_alertStartTime < 0) return;
            var elapsed = _stopwatch.Elapsed.TotalMilliseconds - _alertStartTime;
            if (elapsed < AlertDurationMs)
            {
                var progress = (float)(elapsed / AlertDurationMs);
                _assistantText.BackgroundColor = Lerp(_targetBackgroundColor, _initialBackgroundColor, progress);
            }
            else
            {
                _assistantText.BackgroundColor = _initialBackgroundColor;
                _alertStartTime = -1;
            }
        }

        public void Alert(Color color)
        {
            _targetBackgroundColor = color;
            _assistantText.BackgroundColor = color;
            _alertStartTime = _stopwatch.Elapsed.TotalMilliseconds;
        }

        private static Color Lerp(Color from, Color to, float t)
        {
            return Color.FromArgb(
                (int)Math.Round(from.A + (to.A - from.A) * t),
                (int)Math.Round(from.R + (to.R - from.R) * t),
                (int)Math.Round(from.G + (to.G - from.G) * t),
                (int)Math.Round(from.B + (to.B - from.B) * t));
        }
    }
}