using System;
using System.Collections.Generic;
using System.Numerics;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Layouts;
using JREMonitors.Core.Layouts.Text;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.Utils;
using JREMonitors.Core.Widgets;
using JREMonitors.E233.Buttons;
using JREMonitors.E233.Constants;
using Vortice.DirectWrite;
using Vortice.Mathematics;

namespace JREMonitors.E233.TIMS
{
    public class TIMSResources
    {
        public TIMSResources(RenderContext context)
        {
            Format18 = context.FontManager.GetOrCreateFormat(Fonts.MsGothicFamily, 18);
            TextButtonStyle = new TIMSButtonStyle(
                Constants.Buttons.IdleBackgroundColor,
                MonitorColors.White,
                null,
                Constants.Buttons.PressedBackgroundColor,
                "#333".ToColor4(),
                null,
                MonitorColors.White,
                Colors.Blue,
                null,
                0,
                true
            );
            HomeButtonStyle = new TIMSButtonStyle(
                Constants.Buttons.IdleBackgroundColor,
                null,
                null,
                Constants.Buttons.PressedBackgroundColor,
                "#333".ToColor4(),
                null,
                MonitorColors.White,
                "#333".ToColor4(),
                Colors.Blue,
                4,
                true
            );
            FooterIconButtonStyle = new TIMSButtonStyle(
                Constants.Buttons.IdleBackgroundColor,
                MonitorColors.White,
                null,
                Constants.Buttons.PressedBackgroundColor,
                "#333".ToColor4(),
                null,
                MonitorColors.White,
                "#333".ToColor4(),
                Colors.Blue,
                2,
                true
            );
        }

        public IDWriteTextFormat Format18 { get; }
        public TIMSButtonStyle TextButtonStyle { get; }
        public TIMSButtonStyle HomeButtonStyle { get; }
        public TIMSButtonStyle FooterIconButtonStyle { get; }
    }

    public static partial class TIMSExtensions
    {
        private static readonly PropertyKey TIMSResourcesKey = new PropertyKey("E233TIMSResources");

        public static TIMSResources TIMS(this RenderContext context)
        {
            if (!context.Properties.TryGetValue(TIMSResourcesKey, out var value))
            {
                value = new TIMSResources(context);
                context.Properties[TIMSResourcesKey] = value;
            }

            return (TIMSResources)value;
        }

        public static TIMSButton CreateTIMSFlexButton(
            this Widget widget,
            string text,
            float scaleX,
            float scaleY,
            ContentArrangement? arrangement = null,
            bool useHorizontalOverhangMetrics = true,
            float fixedLineSpacing = 0,
            bool cacheBaker = true,
            bool playPressAnimation = true,
            IValueSignal<bool> clickable = null,
            Action onClick = null
        )
        {
            var textLayout = new TextLayout(
                widget.Context,
                widget.Context.TIMS().Format18,
                text,
                fixedLineSpacing: fixedLineSpacing,
                arrangement: arrangement,
                useHorizontalOverhangMetrics: useHorizontalOverhangMetrics,
                useVerticalOverhangMetrics: false,
                isGdiCompatible: true
            );
            return widget.CreateTIMSFlexButton(textLayout, scaleX, scaleY, cacheBaker, playPressAnimation, clickable,
                onClick);
        }

        public static TIMSButton CreateTIMSFlexButton(
            this Widget widget,
            IValueSignal<RichTextDocument> documentSignal,
            float scaleX,
            float scaleY,
            ContentArrangement? arrangement = null,
            bool useHorizontalOverhangMetrics = true,
            float fixedLineSpacing = 0,
            bool cacheBaker = true,
            bool playPressAnimation = true,
            IValueSignal<bool> clickable = null,
            Action onClick = null
        )
        {
            var textLayout = new TextLayout(
                widget.Context,
                widget.Context.TIMS().Format18,
                documentSignal,
                fixedLineSpacing: fixedLineSpacing,
                arrangement: arrangement,
                useHorizontalOverhangMetrics: useHorizontalOverhangMetrics,
                useVerticalOverhangMetrics: false,
                isGdiCompatible: true
            );
            return widget.CreateTIMSFlexButton(textLayout, scaleX, scaleY, cacheBaker, playPressAnimation, clickable,
                onClick);
        }

        private static TIMSButton CreateTIMSFlexButton(
            this Widget widget,
            TextLayout textLayout,
            float scaleX,
            float scaleY,
            bool cacheBaker = true,
            bool playPressAnimation = true,
            IValueSignal<bool> clickable = null,
            Action onClick = null
        )
        {
            return widget.CreateTIMSFlexButton(new[]
            {
                new BitmapScaleDrawer.DrawerProperties(
                    textLayout,
                    scaleX,
                    scaleY,
                    cacheBaker: cacheBaker
                )
            }, playPressAnimation, clickable, onClick);
        }

        public static TIMSButton CreateTIMSFlexButton(this Widget widget,
            IEnumerable<BitmapScaleDrawer.DrawerProperties> drawerProperties,
            bool playPressAnimation = true, IValueSignal<bool> clickable = null, Action onClick = null)
        {
            var button = new TIMSButton(widget.Context, Vector2.Zero, LayoutLength.Flex(), LayoutLength.Flex(),
                widget.Context.TIMS().TextButtonStyle,
                new BitmapScaleDrawer(widget.Context, drawerProperties, 0, true, 0.5f,
                    0.5f), playPressAnimation: playPressAnimation);
            if (clickable != null) button.Clickable.Bind(clickable);

            button.OnClick += onClick;
            return button;
        }

        public static Row CreateFooterOptionsRow(this Widget widget, IList<Widget> widgets)
        {
            return new Row(widget.Context, 770, 454, 104, 46, 1, widgets, 1);
        }
    }
}