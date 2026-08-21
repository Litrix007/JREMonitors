using System;
using System.Collections.Generic;
using System.Numerics;
using JREMonitors.Core.Debugger;
using JREMonitors.Core.DirtyUpdate;
using JREMonitors.Core.Managers;
using JREMonitors.Core.Shadows;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.WIC;

namespace JREMonitors.Core.Contexts
{
    public class RenderContext
    {
        private readonly Func<Vector2> _physicalScaleGetter;
        private readonly Func<bool> _showDebugRectGetter;

        public RenderContext(
            MonitorContext context,
            Func<bool> showDebugRectGetter,
            Func<Vector2> physicalScaleGetter,
            Func<string> activeScreenIdGetter
        )
        {
            Properties = context.Properties;
            D2D1Factory = context.D2D1Factory;
            WicImagingFactory = context.WicImagingFactory;
            FontManager = context.FontManager;
            DwFactory = context.FontManager.DwFactory;
            DirtyUpdateManager = new DirtyUpdateManager();
            Device = context.D2D1Device;
            DeviceContext = context.D2D1Context;
            CommonBrush = context.Brush;
            DropShadowProcessor = context.DropShadowProcessor;
            InnerShadowProcessor = context.InnerShadowProcessor;
            DisplayController = new DisplayController(activeScreenIdGetter);
            _showDebugRectGetter = showDebugRectGetter ?? throw new ArgumentNullException(nameof(showDebugRectGetter));
            _physicalScaleGetter = physicalScaleGetter;
            Debugger = context.Debugger;
        }

        protected RenderContext(RenderContext parent, IDictionary<PropertyKey, object> scopedProperties)
        {
            Properties = scopedProperties;
            D2D1Factory = parent.D2D1Factory;
            WicImagingFactory = parent.WicImagingFactory;
            FontManager = parent.FontManager;
            DwFactory = parent.DwFactory;
            DirtyUpdateManager = parent.DirtyUpdateManager;
            Device = parent.Device;
            DeviceContext = parent.DeviceContext;
            CommonBrush = parent.CommonBrush;
            DropShadowProcessor = parent.DropShadowProcessor;
            InnerShadowProcessor = parent.InnerShadowProcessor;
            DisplayController = parent.DisplayController;
            _showDebugRectGetter = parent._showDebugRectGetter;
            _physicalScaleGetter = parent._physicalScaleGetter;
            Debugger = parent.Debugger;
        }

        public IDictionary<PropertyKey, object> Properties { get; }
        public ID2D1Factory1 D2D1Factory { get; }
        public IWICImagingFactory WicImagingFactory { get; }
        public FontManager FontManager { get; }
        public IDWriteFactory5 DwFactory { get; }
        public DirtyUpdateManager DirtyUpdateManager { get; }
        public ID2D1Device Device { get; }
        public ID2D1DeviceContext DeviceContext { get; }
        public ID2D1SolidColorBrush CommonBrush { get; }
        public DropShadowProcessor DropShadowProcessor { get; }
        public InnerShadowProcessor InnerShadowProcessor { get; }
        public Vector2 PhysicalScale => _physicalScaleGetter();
        public DisplayController DisplayController { get; }
        public bool ShowDebugRect => _showDebugRectGetter();
        public IDebugger Debugger { get; }

        public T GetService<T>(PropertyKey key) where T : class, IDisposable, new()
        {
            if (!Properties.TryGetValue(key, out var value))
            {
                var service = new T();
                Properties[key] = service;
                value = service;
            }

            return (T)value;
        }

        public T GetService<T>(PropertyKey key, Func<T> factory) where T : class, IDisposable
        {
            if (!Properties.TryGetValue(key, out var value))
            {
                var service = factory();
                Properties[key] = service;
                value = service;
            }

            return (T)value;
        }

        public TService GetService<TService, TState>(PropertyKey key, TState state, Func<TState, TService> factory)
            where TService : class, IDisposable
        {
            if (!Properties.TryGetValue(key, out var value))
            {
                var service = factory(state);
                Properties[key] = service;
                value = service;
            }

            return (TService)value;
        }
    }
}