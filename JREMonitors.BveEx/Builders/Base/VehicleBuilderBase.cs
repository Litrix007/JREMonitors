using System;
using System.Collections.Generic;
using BveEx.Extensions.Native;
using BveEx.Extensions.SoundFactory;
using BveTypes.ClassWrappers;
using JREMonitors.BveEx.Configs.Vehicle;
using JREMonitors.BveEx.Monitors;
using JREMonitors.BveEx.Providers;
using JREMonitors.BveEx.Registries;
using JREMonitors.BveEx.Utils;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Monitors;
using JREMonitors.Core.Providers;
using JREMonitors.Core.State;

namespace JREMonitors.BveEx.Builders.Base
{
    public abstract class VehicleBuilderBase<TVehicleConfig> : IVehicleBuilder where TVehicleConfig : VehicleConfig
    {
        void IVehicleBuilder.PopulateRootDataHub(VehicleBuildContext context, VehicleConfig vehicleConfig)
        {
            if (vehicleConfig is TVehicleConfig typedConfig)
                PopulateRootDataHub(context, typedConfig);
            else
                throw new ArgumentException(nameof(vehicleConfig));
        }

        void IVehicleBuilder.PopulateMonitorLocalDataHub(VehiclePostBuildContext context,
            Dictionary<string, Monitor> monitors,
            VehicleConfig vehicleConfig)
        {
            if (vehicleConfig is TVehicleConfig typedConfig)
                PopulateMonitorLocalDataHub(context, monitors, typedConfig);
            else
                throw new ArgumentException(nameof(vehicleConfig));
        }

        void IVehicleBuilder.PostPopulateRootDataHub(VehiclePostBuildContext context, VehicleConfig vehicleConfig)
        {
            if (vehicleConfig is TVehicleConfig typedConfig)
                PostPopulateRootDataHub(context, typedConfig);
            else
                throw new ArgumentException(nameof(vehicleConfig));
        }


        public IEnumerable<MonitorProperties> CreateMonitorProperties(MonitorContext context,
            VehicleConfig config, DataHub dataHub)
        {
            if (config is TVehicleConfig typedConfig)
                return CreateMonitorProperties(context, typedConfig, dataHub);
            throw new ArgumentException(nameof(config));
        }

        void IVehicleBuilder.Reconfigure(VehicleBuildContext context, VehicleConfig oldConfig, VehicleConfig newConfig)
        {
            if (newConfig is TVehicleConfig typedNew)
                Reconfigure(context, (TVehicleConfig)oldConfig, typedNew);
            else
                throw new ArgumentException(nameof(newConfig));
        }

        void IVehicleBuilder.PopulateMonitorLocalDataHub(VehicleBuildContext context,
            IReadOnlyCollection<Monitor> newMonitors, VehicleConfig config)
        {
            if (config is TVehicleConfig typedConfig)
                PopulateMonitorLocalDataHub(context, newMonitors, typedConfig);
            else
                throw new ArgumentException(nameof(config));
        }

        void IVehicleBuilder.OnMonitorsRemoved(VehicleBuildContext context, IList<Monitor> removedMonitors,
            VehicleConfig config)
        {
            OnMonitorsRemoved(context, removedMonitors, config);
        }

        public void ReconfigureMonitorLocal(VehicleBuildContext context, VehicleConfig oldConfig,
            VehicleConfig newConfig, IList<Monitor> monitors)
        {
            if (newConfig is TVehicleConfig typedNew)
                ReconfigureMonitorLocal(context, (TVehicleConfig)oldConfig, typedNew, monitors);
            else
                throw new ArgumentException(nameof(newConfig));
        }

        protected virtual void ReconfigureMonitorLocal(VehicleBuildContext context, TVehicleConfig oldConfig,
            TVehicleConfig newConfig, IList<Monitor> monitors)
        {
        }


        protected virtual void PopulateRootDataHub(VehicleBuildContext context, TVehicleConfig config)
        {
            var atsPlugin = context.RootDataHub.Get<Lazy<AtsPlugin>>();
            var finalInputs = BuildFinalInputs(config);
            var panelDataProvider = new BvePanelDataProvider(atsPlugin, finalInputs,
                CreatePanelDataFallbacks(config, context, finalInputs));
            context.RootDataHub.Put(panelDataProvider);
        }

        protected Dictionary<string, IReadOnlyList<int>> BuildFinalInputs(TVehicleConfig config)
        {
            var finalInputs = new Dictionary<string, IReadOnlyList<int>>();
            foreach (var inputPresetName in config.InputPresetNames)
            {
                if (!BuiltinInputPresetRegistry.InputPresets.TryGetValue(inputPresetName, out var inputPreset))
                    continue;

                foreach (var pair in inputPreset)
                    if (!finalInputs.ContainsKey(pair.Key))
                        finalInputs[pair.Key] = pair.Value;
            }

            foreach (var pair in config.Inputs)
                if (AtsPluginHelper.IsIndicesDisabled(pair.Value))
                    finalInputs.Remove(pair.Key);
                else
                    finalInputs[pair.Key] = pair.Value;

            return finalInputs;
        }

        protected virtual void Reconfigure(VehicleBuildContext context, TVehicleConfig oldConfig,
            TVehicleConfig newConfig)
        {
            var finalInputs = BuildFinalInputs(newConfig);
            var panelDataProvider = context.RootDataHub.Get<BvePanelDataProvider>();
            panelDataProvider.Reconfigure(finalInputs, CreatePanelDataFallbacks(newConfig, context, finalInputs));
            if (!Equals(oldConfig.Outputs.Sound, newConfig.Outputs.Sound))
            {
                context.RootDataHub.Get<BveSoundProvider>().Reconfigure(newConfig.Outputs.Sound);
            }
        }

        protected virtual void PopulateMonitorLocalDataHub(VehiclePostBuildContext context,
            Dictionary<string, Monitor> monitors,
            TVehicleConfig vehicleConfig)
        {
            PopulateMonitorLocalDataHub(context, monitors.Values, vehicleConfig);
        }

        protected virtual void PopulateMonitorLocalDataHub(VehicleBuildContext context,
            IReadOnlyCollection<Monitor> monitors, TVehicleConfig vehicleConfig)
        {
            var soundProvider = context.RootDataHub.GetOrNull<ISoundProvider>();
            if (soundProvider != null)
                foreach (var monitor in monitors)
                    monitor.LocalDataHub.Put(new MonitorSoundController(soundProvider));
        }

        protected virtual void OnMonitorsRemoved(VehicleBuildContext context, IList<Monitor> removedMonitors,
            VehicleConfig config)
        {
        }

        protected virtual void PostPopulateRootDataHub(VehiclePostBuildContext context, TVehicleConfig vehicleConfig)
        {
            var native = context.RootDataHub.Get<INative>();
            var panelDataProvider = context.RootDataHub.Get<BvePanelDataProvider>();
            var scenario = context.RootDataHub.Get<Scenario>();
            var vehicleStateProvider = new BveVehicleStateProvider(native, panelDataProvider, scenario);
            context.TickUpdateManager.Register(vehicleStateProvider);
            context.JumpStationManager.Register(vehicleStateProvider);
            context.RootDataHub.Put(vehicleStateProvider);
            context.RootDataHub.Put(new BveSoundProvider(context.RootDataHub, vehicleConfig.Outputs.Sound), true);
        }

        protected virtual Dictionary<string, Func<int>> CreatePanelDataFallbacks(TVehicleConfig config,
            VehicleBuildContext context, IReadOnlyDictionary<string, IReadOnlyList<int>> finalInputs)
        {
            return null;
        }

        protected abstract IEnumerable<MonitorProperties> CreateMonitorProperties(
            MonitorContext context,
            TVehicleConfig config,
            DataHub dataHub
        );
    }
}