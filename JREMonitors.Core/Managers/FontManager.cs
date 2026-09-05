using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using JREMonitors.Core.Debugger;
using Vortice.DirectWrite;

namespace JREMonitors.Core.Managers
{
    /// <summary>
    ///     字体管理器。
    /// </summary>
    public class FontManager : IDisposable
    {
        private const string FallbackFont = "Yu Gothic UI";
        private readonly IDebugger _debugger;

        private readonly Dictionary<string, IDWriteFontFallback> _fallbackCache =
            new Dictionary<string, IDWriteFontFallback>(StringComparer.OrdinalIgnoreCase);

        private readonly IDWriteFontCollection _fontCollection;

        private readonly
            Dictionary<(string family, float size, FontStyle style, FontWeight weight, FontStretch stretch),
                IDWriteTextFormat> _formatCache =
                new Dictionary<(string, float, FontStyle, FontWeight, FontStretch), IDWriteTextFormat>();

        private readonly IDWriteFontCollection _systemFontCollection;

        public FontManager(string fontsDirectoryPath, IDebugger debugger = null)
        {
            _debugger = debugger;
            DwFactory = DWrite.DWriteCreateFactory<IDWriteFactory5>();
            if (DwFactory == null) throw new PlatformNotSupportedException("DirectWrite5 factory creation failed.");
            _systemFontCollection = DwFactory.GetSystemFontCollection(false);

            using (var fontSetBuilder = DwFactory.CreateFontSetBuilder())
            {
                if (Directory.Exists(fontsDirectoryPath))
                {
                    var fontFiles = Directory.GetFiles(fontsDirectoryPath, "*.*", SearchOption.TopDirectoryOnly);
                    foreach (var file in fontFiles)
                    {
                        var ext = Path.GetExtension(file).ToLower();
                        if (ext != ".otf" && ext != ".ttf" && ext != ".otc" && ext != ".ttc") continue;
                        using (var fontFile = DwFactory.CreateFontFileReference(file))
                        {
                            fontSetBuilder.AddFontFile(fontFile);
                        }
                    }
                }

                using (var fontSet = fontSetBuilder.CreateFontSet())
                {
                    _fontCollection = DwFactory.CreateFontCollectionFromFontSet(fontSet);
                    _debugger?.AddLineLasting($"Loaded {_fontCollection.FontFamilyCount} fonts");
                }
            }
        }

        public IDWriteFactory5 DwFactory { get; }

        public void Dispose()
        {
            foreach (var format in _formatCache.Values) format.Dispose();
            _formatCache.Clear();

            foreach (var fallback in _fallbackCache.Values) fallback.Dispose();
            _fallbackCache.Clear();

            _fontCollection?.Dispose();
            _systemFontCollection?.Dispose();
            DwFactory?.Dispose();
        }

        /// <summary>
        ///     获取（或缓存创建）指定字体族列表的 DirectWrite 文本格式。
        /// </summary>
        /// <param name="familyNames">
        ///     逗号分隔的字体族候选列表（按优先级排列）；
        ///     首选在私有字体集合中解析，未命中时回退到系统字体集合，再未命中则以首个候选名创建并由
        ///     <c>ja-jp</c> 回退链兜底。
        /// </param>
        /// <param name="size">字号。</param>
        /// <param name="fontStyle">字形样式。</param>
        /// <param name="fontWeight">字重。</param>
        /// <param name="fontStretch">字体伸展。</param>
        /// <returns>缓存的 <see cref="IDWriteTextFormat" />。</returns>
        /// <remarks>
        ///     返回的格式对象按 <c>(familyNames, size, style, weight, stretch)</c> 字典缓存，
        ///     同一参数组合命中同一实例；其生命周期由 <see cref="FontManager" /> 持有（在
        ///     <see cref="Dispose" /> 时统一释放），<b>调用者不得自行 Dispose</b>，重复获取即可复用。
        /// </remarks>
        public IDWriteTextFormat GetOrCreateFormat(
            string familyNames,
            float size,
            FontStyle fontStyle = FontStyle.Normal,
            FontWeight fontWeight = FontWeight.Normal,
            FontStretch fontStretch = FontStretch.Normal)
        {
            var key = (familyNames, size, fontStyle, fontWeight, fontStretch);
            if (_formatCache.TryGetValue(key, out var cachedFormat)) return cachedFormat;

            var candidates = (familyNames ?? string.Empty).Split(',').Select(f => f.Trim())
                .Where(f => !string.IsNullOrEmpty(f)).ToArray();
            var (matchedFamily, collection) = ResolveFontFamily(candidates, fontWeight, fontStyle);
            var newFormat = DwFactory.CreateTextFormat(
                matchedFamily,
                collection,
                fontWeight,
                fontStyle,
                fontStretch,
                size,
                "ja-jp"
            );
            var fallback = GetOrCreateFallbackChain(candidates);
            if (fallback != null)
                using (var textFormat1 = newFormat.QueryInterface<IDWriteTextFormat1>())
                {
                    textFormat1.FontFallback = fallback;
                }

            _formatCache.Add(key, newFormat);
            return newFormat;
        }

        private IDWriteFontFallback GetOrCreateFallbackChain(string[] candidates)
        {
            var fallbackFonts = new List<string>();
            if (candidates.Length > 1) fallbackFonts.AddRange(candidates.Skip(1));

            if (!fallbackFonts.Contains(FallbackFont, StringComparer.OrdinalIgnoreCase))
                fallbackFonts.Add(FallbackFont);
            var cacheKey = string.Join(";", fallbackFonts);
            if (_fallbackCache.TryGetValue(cacheKey, out var cachedFallback))
                return cachedFallback;

            using (var builder = DwFactory.CreateFontFallbackBuilder())
            {
                var allUnicodeRange = new[] { new UnicodeRange { First = 0, Last = 0x10FFFF } };
                AddMappingHelper(
                    builder,
                    allUnicodeRange,
                    fallbackFonts.ToArray(),
                    _systemFontCollection
                );
                using (var systemFallback = DwFactory.SystemFontFallback)
                {
                    builder.AddMappings(systemFallback);
                }

                var fallback = builder.CreateFontFallback();
                _fallbackCache[cacheKey] = fallback;
                return fallback;
            }
        }

        private (string FamilyName, IDWriteFontCollection Collection) ResolveFontFamily(
            string[] candidates,
            FontWeight weight,
            FontStyle style)
        {
            if (candidates == null || candidates.Length == 0)
                return (FallbackFont, _systemFontCollection);

            foreach (var candidate in candidates)
            {
                if (FamilySupportsTraits(_fontCollection, candidate, weight, style))
                    return (candidate, _fontCollection);

                if (FamilySupportsTraits(_systemFontCollection, candidate, weight, style))
                    return (candidate, _systemFontCollection);
            }

            var fallbackName = candidates.FirstOrDefault() ?? FallbackFont;
            return (fallbackName, _systemFontCollection);
        }

        private static bool FamilySupportsTraits(IDWriteFontCollection collection, string familyName, FontWeight weight,
            FontStyle style)
        {
            if (collection == null) return false;
            if (!collection.FindFamilyName(familyName, out var index)) return false;

            using (var family = collection.GetFontFamily(index))
            {
                var needWeight = weight != FontWeight.Normal;
                var needItalic = style != FontStyle.Normal;

                if (!needWeight && !needItalic) return true;

                var hasNativeWeight = false;
                var hasNativeItalic = false;

                var fontCount = family.FontCount;
                for (var i = 0; i < fontCount; i++)
                    using (var font = family.GetFont(i))
                    {
                        if (needWeight && font.Weight == weight) hasNativeWeight = true;
                        if (needItalic && font.Style == style) hasNativeItalic = true;
                    }

                if (needWeight && needItalic) return hasNativeWeight && hasNativeItalic;
                if (needWeight) return hasNativeWeight;
                return hasNativeItalic;
            }
        }

        private static unsafe void AddMappingHelper(
            IDWriteFontFallbackBuilder builder,
            UnicodeRange[] ranges,
            string[] targetFamilyNames,
            IDWriteFontCollection fontCollection = null,
            string localeName = null,
            string baseFamilyName = null,
            float scale = 1.0f)
        {
            if (targetFamilyNames == null || targetFamilyNames.Length == 0) return;

            var ptrs = new IntPtr[targetFamilyNames.Length];
            try
            {
                for (var i = 0; i < targetFamilyNames.Length; i++)
                    ptrs[i] = Marshal.StringToHGlobalUni(targetFamilyNames[i]);

                fixed (IntPtr* pPtrs = ptrs)
                {
                    builder.AddMapping(
                        ranges,
                        ranges.Length,
                        (IntPtr)pPtrs,
                        targetFamilyNames.Length,
                        fontCollection,
                        localeName,
                        baseFamilyName,
                        scale
                    );
                }
            }
            finally
            {
                for (var i = 0; i < ptrs.Length; i++)
                    if (ptrs[i] != IntPtr.Zero)
                        Marshal.FreeHGlobal(ptrs[i]);
            }
        }
    }
}