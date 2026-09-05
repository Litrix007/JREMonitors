using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using JREMonitors.BveEx.Configs.Vehicle.TIMS;
using JREMonitors.BveEx.Services.Car;
using JREMonitors.BveEx.Utils;
using JREMonitors.Core.Utils;
using JREMonitors.E233.Constants;
using JREMonitors.JRE.Constants;
using Vortice.Mathematics;
using DirectInputIds = JREMonitors.E233.Constants.DirectInputIds;

namespace JREMonitors.BveEx.Configs.Vehicle
{
    public abstract class E233Config : JREVehicleConfig<E233SignalSystem>
    {
        // ReSharper disable once UnusedMember.Global
        [NodeConverters]
        public static readonly Dictionary<string, Func<JsonNode, string, JsonNode>> E233NodeConverters =
            new Dictionary<string, Func<JsonNode, string, JsonNode>>
            {
                ["tims.icCardPath"] = ConfigPath.NodeConverter,
                ["performanceCurves.*"] = ConfigPath.NodeConverter
            };

        public static readonly Dictionary<string, Size> Resolutions = new Dictionary<string, Size>
        {
            ["800x600"] = new Size(800, 600),
            ["1024x768"] = new Size(1024, 768),
            ["1440x1080"] = new Size(1440, 1080),
            ["1600x1200"] = new Size(1600, 1200),
            ["1920x1440"] = new Size(1920, 1440)
        };

        private static readonly Size DefaultResolution = new Size(1024, 768);
        private TIMSConfig _tims = new TIMSConfig();

        public override HashSet<string> AllowedInputs => base.AllowedInputs
            .Union(CarDoorIds.All(FormationSpecs.Values.Max(s => s.CarCount)))
            .Union(new[] { DirectInputIds.CatenaryVoltage })
            .ToHashSet();

        public override Dictionary<string, MonitorResolution> MonitorResolutions { get; } =
            new Dictionary<string, MonitorResolution>
            {
                ["1"] = new MonitorResolution(Resolutions, DefaultResolution),
                ["2"] = new MonitorResolution(Resolutions, DefaultResolution),
                ["3"] = new MonitorResolution(Resolutions, DefaultResolution)
            };

        public override Dictionary<string, int> MonitorIdOrders { get; } = new Dictionary<string, int>
        {
            [MonitorIds.Monitor1] = 0,
            [MonitorIds.Monitor2] = 1,
            [MonitorIds.Monitor3] = 2
        };

        public override HashSet<string> AllowedSounds => SoundIds.All;

        /// <summary>
        ///     TIMS相关配置。
        /// </summary>
        [JsonPropertyName("tims")]
        public TIMSConfig TIMS
        {
            get => _tims;
            set => _tims = value ?? new TIMSConfig();
        }

        /// <summary>
        ///     是否搭载了TASC设备。
        /// </summary>
        /// <remarks>
        ///     仅 <see cref="CanSetSupportsTasc" /> 为 true 的番台允许设置。
        ///     其余番台固定为 <see cref="DefaultSupportsTasc" />，且不允许设置。
        /// </remarks>
        public bool? SupportsTasc { get; set; }

        protected abstract bool DefaultSupportsTasc { get; }
        protected abstract bool CanSetSupportsTasc { get; }

        public bool EffectiveSupportsTasc =>
            CanSetSupportsTasc ? SupportsTasc ?? DefaultSupportsTasc : DefaultSupportsTasc;

        public override void Validate()
        {
            base.Validate();
            if (!CanSetSupportsTasc && SupportsTasc.HasValue)
                throw new JsonException($"supportsTasc is not supported for {VehicleName}");
        }

        public override bool ShouldFullRebuild(VehicleConfig newConfig)
        {
            if (!(newConfig is E233Config e233Config)) return false;
            return TIMS.TimeTableSecondsOffsetY != e233Config.TIMS.TimeTableSecondsOffsetY || !string.Equals(
                TIMS.TIMSFont18Family,
                e233Config.TIMS.TIMSFont18Family, StringComparison.OrdinalIgnoreCase);
        }

        public static class LightingDefaults
        {
            public const float NightAdaptationGain = 1.3f;
            public const float NightDimmingResponse = 0.55f;
            public const float CompressThresholdNight = 0.75f;
            public const float PanelContrastRatio = 1200.0f;
            public static readonly Color3 LeakColor = "#E8EFFF".ToColor3();
        }
    }
}