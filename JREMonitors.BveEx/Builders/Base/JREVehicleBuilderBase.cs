using System;
using System.Collections.Generic;
using BveTypes.ClassWrappers;
using JREMonitors.BveEx.Configs.Vehicle;
using JREMonitors.BveEx.Providers;
using JREMonitors.BveEx.Services.Car;
using JREMonitors.BveEx.Utils;
using JREMonitors.Core.Services;
using JREMonitors.JRE.Constants;
using JREMonitors.JRE.Providers;
using JREMonitors.JRE.Services;

namespace JREMonitors.BveEx.Builders.Base
{
    // ReSharper disable once InconsistentNaming
    public abstract class JREVehicleBuilderBase<TConfig, TSignal> : VehicleBuilderBase<TConfig>
        where TConfig : JREVehicleConfig<TSignal> where TSignal : struct, Enum
    {
        protected override void PopulateRootDataHub(VehicleBuildContext context, TConfig config)
        {
            var signalProvider = new BveSignalProvider<TSignal>(context.RootDataHub);
            signalProvider.SetActiveSignalSystem(config.DefaultSignalSystem);
            context.TickUpdateManager.Register(signalProvider);
            context.RootDataHub.Put(signalProvider);
            base.PopulateRootDataHub(context, config);
            var delayService = JREDelayServices.CreateJREDelayService();
            context.TickUpdateManager.Register(delayService);
            context.RootDataHub.Put(delayService);
        }

        protected override void PostPopulateRootDataHub(VehiclePostBuildContext context, TConfig vehicleConfig)
        {
            base.PostPopulateRootDataHub(context, vehicleConfig);
            var delayService = context.RootDataHub.Get<DelayService>();
            var speedProvider =
                new DelayedSpeedProvider(context.RootDataHub.Get<IVehicleStateProvider>(), delayService);
            context.TickUpdateManager.Register(speedProvider);
            context.RootDataHub.Put(speedProvider);
            var carStateService = new BveCarStateService(context.RootDataHub);
            context.TickUpdateManager.Register(carStateService);
            context.RootDataHub.Put(carStateService);
            var doorStateService = new BveDoorStateService(context.RootDataHub);
            context.TickUpdateManager.Register(doorStateService);
            context.JumpStationManager.Register(doorStateService);
            context.RootDataHub.Put(doorStateService);
            var passengerStateService = new BvePassengerStateService(context.RootDataHub);
            context.TickUpdateManager.Register(passengerStateService);
            context.JumpStationManager.Register(passengerStateService);
            context.RootDataHub.Put(passengerStateService);
        }

        protected override void Reconfigure(VehicleBuildContext context, TConfig oldConfig, TConfig newConfig)
        {
            base.Reconfigure(context, oldConfig, newConfig);
            if (!oldConfig.DefaultSignalSystem.Equals(newConfig.DefaultSignalSystem))
            {
                var signalProvider = context.RootDataHub.Get<BveSignalProvider<TSignal>>();
                signalProvider.SetActiveSignalSystem(newConfig.DefaultSignalSystem);
            }
        }

        protected override Dictionary<string, Func<int>> CreatePanelDataFallbacks(TConfig config,
            VehicleBuildContext context, IReadOnlyDictionary<string, IReadOnlyList<int>> finalInputs)
        {
            Scenario scenario = null;
            var atsPlugin = context.RootDataHub.Get<Lazy<AtsPlugin>>();
            if (!finalInputs.TryGetValue(DirectInputIds.TascEnabled, out var tascEnabledIndices))
                tascEnabledIndices = Array.Empty<int>();
            if (!finalInputs.TryGetValue(DirectInputIds.TascDisabled, out var tascDisabledIndices))
                tascDisabledIndices = Array.Empty<int>();
            var fallbacks = new Dictionary<string, Func<int>>
            {
                [DirectInputIds.ConstantSpeed] = () =>
                    GetScenario().Vehicle.Instruments.ConstantSpeedRelay.Output
                        .ConstantSpeedMode ==
                    ConstantSpeedMode.Enable
                        ? 1
                        : 0,
                [DirectInputIds.DeviceVoltage] = () => 105,
                [DirectInputIds.TascPower] = () => 1,
                [LogicalInputIds.TascTurnOff] = () =>
                {
                    if (!AtsPluginHelper.IsIndicesDisabled(tascEnabledIndices) &&
                        atsPlugin.Value.TryGetMaxPanelData(tascEnabledIndices, out var enabled))
                        return enabled > 0 ? 0 : 1;
                    if (!AtsPluginHelper.IsIndicesDisabled(tascDisabledIndices) &&
                        atsPlugin.Value.TryGetMaxPanelData(tascDisabledIndices, out var disabled))
                        return disabled > 0 ? 1 : 0;
                    return 1;
                },
                [DirectInputIds.VehicleDoorAllClosed] = () => atsPlugin.Value.Doors.AreAllClosed ? 1 : 0
            };
            return fallbacks;

            Scenario GetScenario()
            {
                if (scenario == null)
                    scenario = context.RootDataHub.Get<Scenario>();
                return scenario;
            }
        }

        public static Func<int> DispatchSignal(ISignalProvider<E233SignalSystem> signalProvider,
            IReadOnlyDictionary<E233SignalSystem, Func<int>> signalHandlers)
        {
            return () =>
            {
                var activeSystem = signalProvider.ActiveSignalSystem;
                if (activeSystem.HasValue && signalHandlers.TryGetValue(activeSystem.Value, out var handler))
                    return handler();
                return 0;
            };
        }
    }
}