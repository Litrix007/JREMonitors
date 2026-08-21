using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JREMonitors.Core.Debugger;
using Vortice.DirectWrite;

namespace JREMonitors.Core.Managers
{
    public class FontManager : IDisposable
    {
        private const string FallbackFont = "Yu Gothic UI";
        private readonly IDebugger _debugger;
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
                    PrintCollectionFonts(_fontCollection);
                }
            }
        }

        public IDWriteFactory5 DwFactory { get; }

        public void Dispose()
        {
            foreach (var format in _formatCache.Values) format.Dispose();

            _formatCache.Clear();
            _fontCollection?.Dispose();
            _systemFontCollection?.Dispose();
            DwFactory?.Dispose();
        }

        public IDWriteTextFormat GetOrCreateFormat(string familyNames, float size,
            FontStyle fontStyle = FontStyle.Normal,
            FontWeight fontWeight = FontWeight.Normal, FontStretch fontStretch = FontStretch.Normal)
        {
            var key = (familyNames, size, fontStyle, fontWeight, fontStretch);
            if (_formatCache.TryGetValue(key, out var cachedFormat)) return cachedFormat;

            var (matchedFamily, collection) = ResolveFontFamily(familyNames, fontWeight, fontStyle);
            var newFormat = DwFactory.CreateTextFormat(
                matchedFamily,
                collection,
                fontWeight,
                fontStyle,
                fontStretch,
                size,
                "ja-jp"
            );

            _formatCache.Add(key, newFormat);
            return newFormat;
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

                switch (needWeight)
                {
                    case true when needItalic:
                        return hasNativeWeight && hasNativeItalic;
                    case true:
                        return hasNativeWeight;
                    default:
                        return hasNativeItalic;
                }
            }
        }

        private (string FamilyName, IDWriteFontCollection Collection) ResolveFontFamily(string familyNames,
            FontWeight weight, FontStyle style)
        {
            if (string.IsNullOrWhiteSpace(familyNames)) return (FallbackFont, _systemFontCollection);

            var candidates = familyNames.Split(',').Select(f => f.Trim()).ToArray();
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

        private void PrintCollectionFonts(IDWriteFontCollection collection)
        {
            var familyCount = collection.FontFamilyCount;
            for (var i = 0; i < familyCount; i++)
                using (var fontFamily = collection.GetFontFamily(i))
                {
                    using (var localizedNames = fontFamily.FamilyNames)
                    {
                        var name = GetFontName(localizedNames);
                        _debugger?.AddLineLasting($"- {name} ({fontFamily.FontCount} styles)");
                        var fontCount = fontFamily.FontCount;
                        for (var j = 0; j < fontCount; j++)
                            using (var font = fontFamily.GetFont(j))
                            {
                                _debugger?.AddLineLasting($" - {font.Weight} {font.Style}");
                            }
                    }
                }
        }

        private static string GetFontName(IDWriteLocalizedStrings localizedNames)
        {
            if (localizedNames == null || localizedNames.Count == 0) return "Unknown Font";
            if (localizedNames.FindLocaleName("ja-jp", out var index) ||
                localizedNames.FindLocaleName("en-us", out index))
                return localizedNames.GetString(index);

            return localizedNames.GetString(0);
        }
    }
}