using System;
using System.Collections.Generic;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Monitors;
using JREMonitors.Core.Utils;
using JREMonitors.E233.Constants;
using JREMonitors.E233.TidScreen;
using JREMonitors.E233.TIMS;
using JREMonitors.E233.TIMS.C00AA;
using JREMonitors.E233.TIMS.C01AX;
using JREMonitors.E233.TIMS.D00AA;
using JREMonitors.E233.TIMS.D01AX;
using JREMonitors.E233.TIMS.D02AA;
using JREMonitors.E233.TIMS.D05AA;
using JREMonitors.E233.TIMS.D05AB;
using JREMonitors.E233.TIMS.S00AA;
using JREMonitors.E233.TIMS.S00AB;
using JREMonitors.E233.TIMS.X00AA;
using Vortice.Mathematics;

namespace JREMonitors.E233
{
    public static class E233Screens
    {
        public static IList<Screen> CreateE233Screens(
            RenderContext context,
            TIMSVehicleSpec spec,
            Func<TIMSVehicleSpec, D00AAScreen> d00aasScreenFactory,
            Func<TIMSVehicleSpec, D01AXScreen> d01axScreenFactory,
            Func<TIMSVehicleSpec, D05AAScreen> d05aasScreenFactory,
            Func<TIMSVehicleSpec, D05ABScreen> d05absScreenFactory,
            Func<TIMSVehicleSpec, C00AAScreen> c00aasScreenFactory,
            Func<TidChangeToTIMSWarningScreen> tidChangeToTimsWarningScreenFactory,
            IEnumerable<Screen> customScreens = null
        )
        {
            var screens = new List<Screen>
            {
                new X00AAScreen(context, spec),
                new S00AAScreen(context),
                new S00ABScreen(context, spec),
                d00aasScreenFactory(spec),
                d01axScreenFactory(spec),
                d05aasScreenFactory(spec),
                d05absScreenFactory(spec),
                new D02AAScreen(context, spec),
                c00aasScreenFactory(spec),
                new C01AAScreen(context, spec)
            };
            if (spec.HasGreenCar) screens.Add(new C01ABScreen(context, spec));

            screens.AddRange(new[] { tidChangeToTimsWarningScreenFactory() });
            if (customScreens != null) screens.AddRange(customScreens);
            return screens;
        }

        public static IList<Screen> CreateE233Screens0(
            RenderContext context,
            string vehicleName,
            IEnumerable<Screen> customScreens = null
        )
        {
            return CreateE233Screens(
                context,
                new TIMSVehicleSpec(vehicleName,
                    TIMSFormationSpecs.FormationSpecs0,
                    false,
                    false,
                    null,
                    new D01AXSpec(false,
                        false,
                        false,
                        null,
                        null),
                    new C01AASpec(true),
                    false,
                    true),
                spec => new D00AAScreen(context, new D00AAButtonGroup0(context, spec)),
                spec => new D01AXScreen(context, spec, new D01AXTrainTypeButtonGroup0(context)),
                spec => new D05AAScreen(context, spec, new D05AABrakeInformation0(context, spec)),
                spec => new D05ABScreen(context, spec),
                spec => new C00AAScreen(context, new C00AAButtonGroup(context, spec)),
                () => new TidChangeToTIMSWarningScreen(context, Images.MeterScreenWithSafetyLamps0,
                    Images.TidScreenWithSafetyLamps0),
                customScreens
            );
        }

        public static IList<Screen> CreateE233Screens1000(
            RenderContext context,
            string vehicleName,
            IEnumerable<Screen> customScreens = null
        )
        {
            return CreateE233Screens(
                context,
                new TIMSVehicleSpec(vehicleName,
                    TIMSFormationSpecs.FormationSpecs1000,
                    true,
                    false,
                    null,
                    new D01AXSpec(true,
                        false,
                        true,
                        null,
                        null),
                    new C01AASpec(false),
                    false,
                    false),
                spec => new D00AAScreen(context, new D00AAButtonGroup1000(context, spec)),
                spec => new D01AXScreen(context, spec, new D01AXTrainTypeButtonGroup(context, false)),
                spec => new D05AAScreen(context, spec, new D05AABrakeInformation1000(context, spec)),
                spec => new D05ABScreen(context, spec, new D05ABDataTable1000(context, spec)),
                spec => new C00AAScreen(context, new C00AAButtonGroup1000(context, spec)),
                () => new TidChangeToTIMSWarningScreen(context, Images.MeterScreenWithSafetyLamps1000,
                    Images.TidScreenWithSafetyLamps1000),
                customScreens
            );
        }

        public static IList<Screen> CreateE233Screens3000(
            RenderContext context,
            string vehicleName,
            IEnumerable<Screen> customScreens = null
        )
        {
            return CreateE233Screens(
                context,
                new TIMSVehicleSpec(vehicleName,
                    TIMSFormationSpecs.FormationSpecs3000,
                    false,
                    true,
                    Colors.LimeGreen.ToColor3(),
                    new D01AXSpec(false,
                        false,
                        false,
                        Colors.Red.ToColor3(),
                        MonitorColors.White.ToColor3()),
                    new C01AASpec(false),
                    true,
                    false),
                spec => new D00AAScreen(context, new D00AAButtonGroup(context, spec)),
                spec => new D01AXScreen(context, spec, new D01AXTrainTypeButtonGroup(context, true)),
                spec => new D05AAScreen(context, spec, new D05AABrakeInformation0(context, spec)),
                spec => new D05ABScreen(context, spec),
                spec => new C00AAScreen(context, new C00AAButtonGroup3000(context, spec)),
                () => new TidChangeToTIMSWarningScreen(context, Images.MeterScreenWithSafetyLamps3000,
                    Images.TidScreenWithSafetyLamps3000),
                customScreens
            );
        }

        public static IList<Screen> CreateE233Screens5000(
            RenderContext context,
            string vehicleName,
            IEnumerable<Screen> customScreens = null
        )
        {
            return CreateE233Screens(
                context,
                new TIMSVehicleSpec(vehicleName,
                    TIMSFormationSpecs.FormationSpecs5000,
                    false,
                    false,
                    null,
                    new D01AXSpec(false,
                        false,
                        false,
                        null,
                        null),
                    new C01AASpec(true),
                    false,
                    false),
                spec => new D00AAScreen(context, new D00AAButtonGroup5000(context, spec)),
                spec => new D01AXScreen(context, spec, new D01AXTrainTypeButtonGroup(context, true)),
                spec => new D05AAScreen(context, spec, new D05AABrakeInformation0(context, spec)),
                spec => new D05ABScreen(context, spec),
                spec => new C00AAScreen(context, new C00AAButtonGroup5000(context, spec)),
                () => new TidChangeToTIMSWarningScreen(context, Images.MeterScreenWithSafetyLamps5000,
                    Images.TidScreenWithSafetyLamps5000),
                customScreens
            );
        }
    }
}