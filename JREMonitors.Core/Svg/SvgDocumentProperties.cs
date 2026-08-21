using Vortice.Mathematics;

namespace JREMonitors.Core.Svg
{
    public struct SvgDocumentProperties
    {
        public readonly string XmlContent;
        public readonly float? TargetWidth;
        public readonly float? TargetHeight;
        public readonly Color4? GlobalColor;

        public SvgDocumentProperties(
            string xmlContent,
            float? targetWidth = null,
            float? targetHeight = null,
            Color4? globalColor = null
        )
        {
            XmlContent = xmlContent;
            TargetWidth = targetWidth;
            TargetHeight = targetHeight;
            GlobalColor = globalColor;
        }
    }
}