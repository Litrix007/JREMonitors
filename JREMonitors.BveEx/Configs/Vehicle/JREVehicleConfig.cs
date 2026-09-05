using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using JREMonitors.BveEx.Utils;
using JREMonitors.Core.Utils;
using JREMonitors.E233.TIMS;
using JREMonitors.E233.TIMS.ICCard;
using JREMonitors.JRE.Constants;

namespace JREMonitors.BveEx.Configs.Vehicle
{
    // ReSharper disable once InconsistentNaming
    public abstract class JREVehicleConfig<TSignal> : VehicleConfig where TSignal : struct, Enum
    {
        [NodeConverters]
        // ReSharper disable once UnusedMember.Global
        // ReSharper disable once StaticMemberInGenericType
        public static readonly Dictionary<string, Func<JsonNode, string, JsonNode>> JRENodeConverters =
            new Dictionary<string, Func<JsonNode, string, JsonNode>>
            {
                ["performanceCurves.*"] = ConfigPath.NodeConverter,
                ["vehicleParameters.*"] = ConfigPath.NodeConverter
            };

        private Dictionary<string, ConfigPath> _performanceCurves = new Dictionary<string, ConfigPath>();
        private Dictionary<string, ConfigPath> _vehicleParameters = new Dictionary<string, ConfigPath>();

        public override HashSet<string> AllowedInputs => base.AllowedInputs
            .Union(new[] { DirectInputIds.ConstantSpeed, DirectInputIds.DeviceVoltage }).ToHashSet();

        /// <summary>
        ///     默认信号系统名称。支持的值随车型变化。
        /// </summary>
        public TSignal? DefaultSignalSystem { get; set; }

        /// <summary>
        ///     默认编组名称。支持的值随车型变化。
        /// </summary>
        public string DefaultFormation { get; set; }

        /// <summary>
        ///     各编组所使用的性能曲线文件路径。
        /// </summary>
        /// <remarks>
        ///     <para>当某编组未配置时，将回退至车辆的初始性能。</para>
        /// </remarks>
        public Dictionary<string, ConfigPath> PerformanceCurves
        {
            get => _performanceCurves;
            set => _performanceCurves = value ?? new Dictionary<string, ConfigPath>();
        }

        /// <summary>
        ///     各编组使用的车辆参数路径。
        /// </summary>
        /// <remarks>
        ///     <para>当某编组未配置或解析失败时，将尝试回退至车辆文件中定义的的参数路径。</para>
        ///     <para>不会更新<c>[OneLeverCab]</c>、<c>[Cab]</c>、<c>[ViewPoint]</c>。</para>
        /// </remarks>
        public Dictionary<string, ConfigPath> VehicleParameters
        {
            get => _vehicleParameters;
            set => _vehicleParameters = value ?? new Dictionary<string, ConfigPath>();
        }

        public abstract TIMSDisplayMode DefaultDisplayMode { get; }

        public abstract IReadOnlyDictionary<string, TIMSFormationSpec> FormationSpecs { get; }

        public abstract HashSet<TSignal> AllowedSignalSystems { get; }

        public override void Validate()
        {
            base.Validate();
            var inputs = Inputs;
            if (inputs.ContainsKey(DirectInputIds.HoldSpeed) && !inputs.ContainsKey(DirectInputIds.ConstantSpeed))
                throw new JsonException(
                    $"When specifying the {nameof(DirectInputIds.HoldSpeed)} input value, the {nameof(DirectInputIds.ConstantSpeed)} input value must also be explicitly specified");

            if (inputs.ContainsKey(DirectInputIds.TascEnabled) && inputs.ContainsKey(DirectInputIds.TascDisabled))
                throw new JsonException(
                    $"Cannot both specify {nameof(DirectInputIds.TascEnabled)} and {nameof(DirectInputIds.TascDisabled)} at the same time.");

            ValidateFormationAndPerformanceCurves();
            ValidateSignalSystem();
        }

        private void ValidateFormationAndPerformanceCurves()
        {
            var defaultFormation = DefaultFormation;
            if (defaultFormation == null) throw new JsonException("Must specify defaultFormation.");
            if (!FormationSpecs.ContainsKey(defaultFormation))
                throw new JsonException($"Invalid defaultFormation '{defaultFormation}' for {VehicleName}.");
            foreach (var pair in PerformanceCurves)
            {
                if (!FormationSpecs.ContainsKey(pair.Key))
                    throw new JsonException(
                        $"(In {nameof(PerformanceCurves).ToCamelCase()}) Invalid formation '{pair.Key}' for {VehicleName}.");
                var path = pair.Value.GetAbsolutePath();
                if (path == null)
                    throw new JsonException($"{nameof(PerformanceCurves)} path '{pair.Value.Value}' is not found.");
            }

            foreach (var pair in VehicleParameters)
            {
                if (!FormationSpecs.ContainsKey(pair.Key))
                    throw new JsonException(
                        $"(In {nameof(VehicleParameters).ToCamelCase()}) Invalid formation '{pair.Key}' for {VehicleName}.");
                var path = pair.Value.GetAbsolutePath();
                if (path == null)
                    throw new JsonException($"{nameof(VehicleParameters)} path '{pair.Value.Value}' is not found.");
            }
        }

        private void ValidateSignalSystem()
        {
            if (!DefaultSignalSystem.HasValue) return;
            var allowedSignalSystems = AllowedSignalSystems;
            if (allowedSignalSystems == null || allowedSignalSystems.Count == 0)
                throw new JsonException(
                    $"defaultSignalSystem is not supported for {VehicleName}");

            if (!allowedSignalSystems.Contains(DefaultSignalSystem.Value))
                throw new JsonException(
                    $"Invalid defaultSignalSystem '{DefaultSignalSystem.Value}' for {VehicleName}.\n" +
                    $"Allowed values are: {string.Join(", ", allowedSignalSystems)}.");
        }
    }
}