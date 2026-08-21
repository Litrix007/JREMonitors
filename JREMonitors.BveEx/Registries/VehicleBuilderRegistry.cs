using System;
using System.Collections.Generic;
using JREMonitors.BveEx.Builders;
using JREMonitors.BveEx.Builders.Base;
using JREMonitors.BveEx.Configs.Vehicle;

namespace JREMonitors.BveEx.Registries
{
    public static class VehicleBuilderRegistry
    {
        private static readonly Dictionary<Type, IVehicleBuilder> Builders = new Dictionary<Type, IVehicleBuilder>
        {
            { typeof(E233_0Config), new E233_0Builder() },
            { typeof(E233_1000Config), new E233_1000Builder() },
            { typeof(E233_3000Config), new E233_3000Builder() },
            { typeof(E233_5000Config), new E233_5000Builder() }
        };

        public static IVehicleBuilder GetBuilder(VehicleConfig config)
        {
            return Builders.TryGetValue(config.GetType(), out var builder)
                ? builder
                : throw new NotSupportedException($"No builder registered for {config.VehicleName}");
        }
    }
}