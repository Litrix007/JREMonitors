using JREMonitors.Core.Contexts;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace JREMonitors.Core.Services.Render
{
    /// <summary>
    ///     <see cref="ID2D1SolidColorBrush" /> 全局缓存。
    /// </summary>
    public class BrushCache : ResourceCache<Color4, ID2D1SolidColorBrush>
    {
        public static readonly PropertyKey Key = new PropertyKey(nameof(BrushCache));
        private readonly ID2D1DeviceContext _context;

        public BrushCache(RenderContext context)
        {
            _context = context.DeviceContext;
        }

        public ID2D1SolidColorBrush GetOrCreateBrush(Color4 color)
        {
            return GetOrCreate(color, color, c => _context.CreateSolidColorBrush(c));
        }
    }

    public static class BrushRenderServiceExtensions
    {
        public static BrushCache GetBrushCache(this RenderContext context)
        {
            if (!context.Properties.TryGetValue(BrushCache.Key, out var value))
            {
                value = new BrushCache(context);
                context.Properties[BrushCache.Key] = value;
            }

            return (BrushCache)value;
        }
    }
}