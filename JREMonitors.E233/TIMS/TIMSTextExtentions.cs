using System.Collections.Generic;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Layouts.Text;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Widgets;
using Vortice.DirectWrite;

namespace JREMonitors.E233.TIMS
{
    public static partial class TIMSExtensions
    {
        private const bool DefaultBypassBakerOn1X = true;

        public static TextLayout CreateTIMSTextLayout(
            this Widget widget,
            string text,
            ContentOrientation? orientation = null,
            float sizeLimit = 0,
            bool useBounds = false,
            ContentFlowDirection? flowDirection = null,
            ContentArrangement? arrangement = null,
            float scaleX = 1,
            float scaleY = 1,
            IEnumerable<float> finalOffsetsMainAxis = null,
            float finalOffsetCrossAxis = 0,
            float? fixedLineSpacing = null,
            bool useHorizontalOverhangMetrics = false,
            bool useVerticalOverhangMetrics = false,
            bool ceilContentBounds = false,
            bool cacheMetrics = true,
            RenderContext context = null,
            IDWriteTextFormat format = null
        )
        {
            if (context == null) context = widget.Context;
            return new TextLayout(
                context,
                format ?? context.TIMS().Format18,
                text,
                orientation,
                sizeLimit,
                useBounds,
                flowDirection,
                arrangement,
                scaleX,
                scaleY,
                finalOffsetsMainAxis,
                finalOffsetCrossAxis,
                fixedLineSpacing,
                useHorizontalOverhangMetrics,
                useVerticalOverhangMetrics,
                ceilContentBounds,
                true,
                cacheMetrics
            );
        }

        public static TextLayout CreateTIMSTextLayout(
            this Widget widget,
            IValueSignal<RichTextDocument> documentSource,
            ContentOrientation? orientation = null,
            float sizeLimit = 0,
            bool useBounds = false,
            ContentFlowDirection? flowDirection = null,
            ContentArrangement? arrangement = null,
            float scaleX = 1,
            float scaleY = 1,
            IEnumerable<float> finalOffsetsMainAxis = null,
            float finalOffsetCrossAxis = 0,
            float? fixedLineSpacing = null,
            bool useHorizontalOverhangMetrics = false,
            bool useVerticalOverhangMetrics = false,
            bool ceilContentBounds = false,
            bool cacheMetrics = true,
            RenderContext context = null,
            IDWriteTextFormat format = null
        )
        {
            if (context == null) context = widget.Context;
            return new TextLayout(
                context,
                format ?? context.TIMS().Format18,
                documentSource,
                orientation,
                sizeLimit,
                useBounds,
                flowDirection,
                arrangement,
                scaleX,
                scaleY,
                finalOffsetsMainAxis,
                finalOffsetCrossAxis,
                fixedLineSpacing,
                useHorizontalOverhangMetrics,
                useVerticalOverhangMetrics,
                ceilContentBounds,
                true,
                cacheMetrics
            );
        }

        public static TextLayout CreateTIMSTextLayout(
            this Widget widget,
            RichTextDocument document,
            ContentOrientation? orientation = null,
            float sizeLimit = 0,
            bool useBounds = false,
            ContentFlowDirection? flowDirection = null,
            ContentArrangement? arrangement = null,
            float scaleX = 1,
            float scaleY = 1,
            IEnumerable<float> finalOffsetsMainAxis = null,
            float finalOffsetCrossAxis = 0,
            float? fixedLineSpacing = null,
            bool useHorizontalOverhangMetrics = false,
            bool useVerticalOverhangMetrics = false,
            bool ceilContentBounds = false,
            bool cacheMetrics = true,
            RenderContext context = null,
            IDWriteTextFormat format = null
        )
        {
            if (context == null) context = widget.Context;
            return new TextLayout(
                context,
                format ?? context.TIMS().Format18,
                document,
                orientation,
                sizeLimit,
                useBounds,
                flowDirection,
                arrangement,
                scaleX,
                scaleY,
                finalOffsetsMainAxis,
                finalOffsetCrossAxis,
                fixedLineSpacing,
                useHorizontalOverhangMetrics,
                useVerticalOverhangMetrics,
                ceilContentBounds,
                true,
                cacheMetrics
            );
        }

        public static BitmapScaleDrawer CreateTIMSTextDrawer(
            this Widget widget,
            string text,
            float scaleX = 1,
            float scaleY = 1,
            float horizontalAlignment = 0,
            float verticalAlignment = 0,
            float finalOffsetCrossAxis = 0,
            ContentArrangement? arrangement = null,
            bool useHorizontalOverhangMetrics = false,
            bool useVerticalOverhangMetrics = false,
            bool bypassBakerOn1X = DefaultBypassBakerOn1X,
            bool cache = true,
            RenderContext context = null,
            IDWriteTextFormat format = null
        )
        {
            if (context == null) context = widget.Context;
            var layout = widget.CreateTIMSTextLayout(
                text,
                useHorizontalOverhangMetrics: useHorizontalOverhangMetrics,
                useVerticalOverhangMetrics: useVerticalOverhangMetrics,
                cacheMetrics: cache,
                format: format,
                context: context
            );

            return new BitmapScaleDrawer(
                context,
                layout,
                scaleX,
                scaleY,
                true,
                horizontalAlignment,
                verticalAlignment,
                finalOffsetCrossAxis,
                arrangement,
                bypassBakerOn1X,
                cache
            );
        }

        public static BitmapScaleDrawer CreateTIMSTextDrawer(
            this Widget widget,
            IValueSignal<RichTextDocument> documentSource,
            float scaleX = 1,
            float scaleY = 1,
            float horizontalAlignment = 0,
            float verticalAlignment = 0,
            float finalOffsetCrossAxis = 0,
            ContentArrangement? arrangement = null,
            bool useHorizontalOverhangMetrics = false,
            bool useVerticalOverhangMetrics = false,
            bool bypassBakerOn1X = DefaultBypassBakerOn1X,
            bool cache = true,
            RenderContext context = null,
            IDWriteTextFormat format = null
        )
        {
            if (context == null) context = widget.Context;
            var layout = widget.CreateTIMSTextLayout(
                documentSource,
                useHorizontalOverhangMetrics: useHorizontalOverhangMetrics,
                useVerticalOverhangMetrics: useVerticalOverhangMetrics,
                cacheMetrics: cache,
                format: format,
                context: context
            );

            return new BitmapScaleDrawer(
                context,
                layout,
                scaleX,
                scaleY,
                true,
                horizontalAlignment,
                verticalAlignment,
                finalOffsetCrossAxis,
                arrangement,
                bypassBakerOn1X,
                cache
            );
        }

        public static BitmapScaleDrawer CreateTIMSTextDrawer(
            this Widget widget,
            IContentMeasurableBoundsDrawer drawer,
            float scaleX = 1,
            float scaleY = 1,
            float horizontalAlignment = 0,
            float verticalAlignment = 0,
            float finalOffsetCrossAxis = 0,
            ContentArrangement? arrangement = null,
            bool bypassBakerOn1X = DefaultBypassBakerOn1X,
            bool cacheBaker = true,
            RenderContext context = null
        )
        {
            return new BitmapScaleDrawer(
                context ?? widget.Context,
                drawer,
                scaleX,
                scaleY,
                true,
                horizontalAlignment,
                verticalAlignment,
                finalOffsetCrossAxis,
                arrangement,
                bypassBakerOn1X,
                cacheBaker
            );
        }

        public static BitmapScaleDrawer CreateTIMSTextDrawer(
            this Widget widget,
            IEnumerable<BitmapScaleDrawer.DrawerProperties> drawerProperties,
            float spacing = 0,
            float horizontalAlignment = 0,
            float verticalAlignment = 0,
            ContentArrangement? arrangement = null,
            bool bypassBakerOn1X = DefaultBypassBakerOn1X,
            RenderContext context = null
        )
        {
            return new BitmapScaleDrawer(
                context ?? widget.Context,
                drawerProperties,
                spacing,
                true,
                horizontalAlignment,
                verticalAlignment,
                arrangement,
                bypassBakerOn1X
            );
        }
    }
}