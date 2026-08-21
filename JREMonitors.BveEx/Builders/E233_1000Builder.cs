using System;
using System.Collections.Generic;
using BveTypes.ClassWrappers;
using JREMonitors.BveEx.Builders.Base;
using JREMonitors.BveEx.Configs.Vehicle;
using JREMonitors.BveEx.Monitors;
using JREMonitors.BveEx.Utils;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Monitors;
using JREMonitors.Core.State;
using JREMonitors.E233;
using JREMonitors.E233.Constants;
using JREMonitors.E233.MeterScreen;
using JREMonitors.E233.TidScreen;
using JREMonitors.E233.TIMS.S00AA;
using JREMonitors.JRE.Constants;
using JREMonitors.JRE.Providers;
using DirectInputIds = JREMonitors.JRE.Constants.DirectInputIds;

namespace JREMonitors.BveEx.Builders
{
    // ReSharper disable once InconsistentNaming
    public class E233_1000Builder : E233BuilderBase<E233_1000Config>
    {
        protected override IEnumerable<MonitorProperties> CreateMonitorProperties(
            MonitorContext context, E233_1000Config vehicleConfig, DataHub dataHub)
        {
            var monitors = new List<MonitorProperties>();
            if (vehicleConfig.Monitors.TryGetValue("1", out var m1))
                monitors.Add(new MonitorProperties(
                    MonitorIds.Monitor1, vehicleConfig.GetExternalDisplayName(MonitorIds.Monitor1),
                    E233Config.Resolutions.Values,
                    vehicleConfig.MonitorResolutions["1"].GetResolution(m1.Resolution), m1,
                    vehicleConfig.Display, ResolveLighting(m1.Cab.Lighting),
                    renderContext => new Screen[]
                        { new S00AAScreen(renderContext), new MeterScreen1000(renderContext) }, ScreenIds.Meter
                ));
            if (vehicleConfig.Monitors.TryGetValue("2", out var m2))
                monitors.Add(new MonitorProperties(
                    MonitorIds.Monitor2, vehicleConfig.GetExternalDisplayName(MonitorIds.Monitor2),
                    E233Config.Resolutions.Values,
                    vehicleConfig.MonitorResolutions["2"].GetResolution(m2.Resolution), m2,
                    vehicleConfig.Display, ResolveLighting(m2.Cab.Lighting), CreateScreens, ScreenIds.X00AA
                ));
            if (vehicleConfig.Monitors.TryGetValue("3", out var m3))
                monitors.Add(new MonitorProperties(
                    MonitorIds.Monitor3, vehicleConfig.GetExternalDisplayName(MonitorIds.Monitor3),
                    E233Config.Resolutions.Values,
                    vehicleConfig.MonitorResolutions["3"].GetResolution(m3.Resolution), m3,
                    vehicleConfig.Display, ResolveLighting(m3.Cab.Lighting), CreateScreens, ScreenIds.Tid
                ));
            return monitors;

            IList<Screen> CreateScreens(RenderContext renderContext)
            {
                return E233Screens.CreateE233Screens1000(renderContext, vehicleConfig.VehicleName,
                    new[] { new TidScreen1000(renderContext) });
            }
        }

        protected override Dictionary<string, Func<int>> CreatePanelDataFallbacks(E233_1000Config config,
            VehicleBuildContext context,
            IReadOnlyDictionary<string, IReadOnlyList<int>> finalInputs)
        {
            var signalProvider = context.RootDataHub.Get<ISignalProvider<E233SignalSystem>>();
            var fallbacks = base.CreatePanelDataFallbacks(config, context, finalInputs);
            var atsPlugin = context.RootDataHub.Get<Lazy<AtsPlugin>>();
            for (var i = 0; i < LogicalInputIds.NormalAtcLampIds.Count; i++)
            {
                var signalLampId = LogicalInputIds.NormalAtcLampIds[i];
                var atc6LampId = DirectInputIds.Atc6LampIds[i];
                var datcLampId = DirectInputIds.DatcLampIds[i];
                fallbacks[signalLampId] = DispatchSignal(signalProvider,
                    new Dictionary<E233SignalSystem, Func<int>>
                    {
                        [E233SignalSystem.Atc6] =
                            SignalHelper.CreateSingleFallback(atsPlugin, atc6LampId, finalInputs),
                        [E233SignalSystem.Datc] =
                            SignalHelper.CreateSingleFallback(atsPlugin, datcLampId, finalInputs)
                    });
            }

            fallbacks[LogicalInputIds.AtcSpeedLimit] = DispatchSignal(signalProvider,
                new Dictionary<E233SignalSystem, Func<int>>
                {
                    [E233SignalSystem.Atc6] =
                        SignalHelper.CreateAtc6SpeedLimitFallback(atsPlugin, finalInputs),
                    [E233SignalSystem.Datc] =
                        SignalHelper.CreateSingleFallback(atsPlugin, DirectInputIds.DatcSpeedLimit, finalInputs)
                });
            return fallbacks;
        }
    }
}