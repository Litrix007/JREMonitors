using System;
using System.Collections.Generic;
using System.Linq;
using JREMonitors.BveEx.Configs.Vehicle;
using JREMonitors.BveEx.Monitors;
using JREMonitors.BveEx.Providers;
using JREMonitors.BveEx.Services;
using JREMonitors.Core.Monitors;
using JREMonitors.Core.Providers;
using JREMonitors.E233;
using JREMonitors.E233.Constants;
using JREMonitors.E233.TIMS;
using DirectInputIds = JREMonitors.E233.Constants.DirectInputIds;

namespace JREMonitors.BveEx.Builders.Base
{
    public abstract class E233BuilderBase<TConfig> : JREVehicleBuilderBase<TConfig, E233SignalSystem>
        where TConfig : E233Config
    {
        protected static LightingProperties ResolveLighting(LightingConfig config)
        {
            if (!config.Enabled)
                return LightingProperties.Disabled;

            return new LightingProperties(
                config.Enabled,
                config.BrightExtent,
                config.ShadowExtent,
                config.NightAdaptationGain ?? E233Config.LightingDefaults.NightAdaptationGain,
                config.NightDimmingResponse ?? E233Config.LightingDefaults.NightDimmingResponse,
                config.CompressThresholdNight ?? E233Config.LightingDefaults.CompressThresholdNight,
                config.PanelContrastRatio ?? E233Config.LightingDefaults.PanelContrastRatio,
                config.LeakColor ?? E233Config.LightingDefaults.LeakColor,
                config.GlassReflectanceLight,
                config.GlassReflectanceDark,
                config.GlareColor,
                config.AmbientMax ?? 1f
            );
        }

        protected override void PopulateMonitorLocalDataHub(VehicleBuildContext context,
            Dictionary<string, Monitor> monitors, TConfig vehicleConfig)
        {
            base.PopulateMonitorLocalDataHub(context, monitors, vehicleConfig);
            foreach (var monitor in monitors.Values) monitor.LocalDataHub.Put(new E233MonitorStates());
            var interlockMediator = new E233MonitorInterlockMediator(monitors);
            context.TickUpdateManager.Register(interlockMediator);
            context.RootDataHub.Put(interlockMediator);
        }

        protected override void PopulateMonitorLocalDataHub(VehicleBuildContext context,
            IReadOnlyCollection<Monitor> monitors, TConfig vehicleConfig)
        {
            base.PopulateMonitorLocalDataHub(context, monitors, vehicleConfig);
            var mediator = context.RootDataHub.GetOrNull<E233MonitorInterlockMediator>();
            foreach (var monitor in monitors)
            {
                monitor.LocalDataHub.Put(new E233MonitorStates());
                mediator?.AddMonitor(monitor);
            }
        }

        protected override void OnMonitorsRemoved(VehicleBuildContext context, IList<Monitor> removedMonitors,
            VehicleConfig config)
        {
            base.OnMonitorsRemoved(context, removedMonitors, config);
            var mediator = context.RootDataHub.GetOrNull<E233MonitorInterlockMediator>();
            if (mediator == null) return;
            foreach (var monitor in removedMonitors) mediator.RemoveMonitor(monitor);
        }

        protected override void Reconfigure(VehicleBuildContext context, TConfig oldConfig, TConfig newConfig)
        {
            base.Reconfigure(context, oldConfig, newConfig);
            var timsService = context.RootDataHub.Get<TIMSService>();
            timsService.Reconfigure(newConfig.TIMS.VehicleDirection, newConfig.TIMS.BaseInteriorTemperature,
                newConfig.TIMS.ExternalTemperature, newConfig.TIMS.BaseHumidity);
            var icCardService = context.RootDataHub.Get<BveTIMSICCardService<E233SignalSystem>>();
            icCardService.Reconfigure(newConfig, newConfig.TIMS.ICCardPath?.GetAbsolutePath());
        }

        protected override void PostPopulateRootDataHub(VehiclePostBuildContext context, TConfig vehicleConfig)
        {
            base.PostPopulateRootDataHub(context, vehicleConfig);
            var timsConfig = vehicleConfig.TIMS;
            var timsService = new BveTIMSService(context.RootDataHub, timsConfig.VehicleDirection,
                timsConfig.BaseInteriorTemperature, timsConfig.ExternalTemperature, timsConfig.BaseHumidity);
            context.TickUpdateManager.Register(timsService);
            context.JumpStationManager.Register(timsService);
            context.RootDataHub.Put<TIMSService>(timsService);
            var icCardService = new BveTIMSICCardService<E233SignalSystem>(
                context.RootDataHub,
                vehicleConfig,
                timsConfig.ICCardPath?.GetAbsolutePath()
            );
            context.TickUpdateManager.Register(icCardService, icCardService.BeforeDeps.OfType<ITickUpdatable>(),
                icCardService.AfterDeps.OfType<ITickUpdatable>());
            context.JumpStationManager.Register(icCardService, icCardService.BeforeDeps.OfType<IJumpStationListener>(),
                icCardService.AfterDeps.OfType<IJumpStationListener>());
            context.RootDataHub.Put(icCardService, true);
        }

        protected override Dictionary<string, Func<int>> CreatePanelDataFallbacks(TConfig config,
            VehicleBuildContext context, IReadOnlyDictionary<string, IReadOnlyList<int>> finalInputs)
        {
            var fallbacks = base.CreatePanelDataFallbacks(config, context, finalInputs);
            fallbacks[DirectInputIds.CatenaryVoltage] = () => 1500;
            return fallbacks;
        }
    }
}