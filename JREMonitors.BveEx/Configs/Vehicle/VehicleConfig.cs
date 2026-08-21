using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using JREMonitors.BveEx.Registries;
using JREMonitors.BveEx.Utils;

namespace JREMonitors.BveEx.Configs.Vehicle
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(E233_0Config), "E233-0")]
    [JsonDerivedType(typeof(E233_1000Config), "E233-1000")]
    [JsonDerivedType(typeof(E233_3000Config), "E233-3000")]
    [JsonDerivedType(typeof(E233_5000Config), "E233-5000")]
    public abstract class VehicleConfig
    {
        // ReSharper disable once UnusedMember.Global
        [NodeConverters] public static readonly Dictionary<string, Func<JsonNode, string, JsonNode>> NodeConverters =
            new Dictionary<string, Func<JsonNode, string, JsonNode>>
            {
                ["outputs.sound.*"] = ConfigPath.NodeConverter
            };

        private DisplayConfig _display = new DisplayConfig();
        private HashSet<string> _inputPresetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, IReadOnlyList<int>> _inputs = new Dictionary<string, IReadOnlyList<int>>();
        private Dictionary<string, MonitorConfig> _monitors = new Dictionary<string, MonitorConfig>();
        private OutputsConfig _outputs = new OutputsConfig();

        /// <summary>
        ///     使用的面板输入预设，支持的值随车型变化。
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         预设包含了一组预定义的输入索引，若车辆使用了对应的插件可直接引用。
        ///     </para>
        /// </remarks>
        [JsonPropertyName("inputPresets")]
        public HashSet<string> InputPresetNames
        {
            get => _inputPresetNames;
            set => _inputPresetNames = value?.Where(n => n != null).ToHashSet(StringComparer.OrdinalIgnoreCase) ??
                                       new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        ///     定义输入索引，键为该输入类型的名称，值为对应的面板索引。
        /// </summary>
        /// <remarks>
        ///     <para>部分输入已内置默认行为，未配置该输入时将回退到默认行为。</para>
        ///     <para>当值为 <c>-1</c> 时，该输入对应的值恒为 <c>0</c>。</para>
        ///     <para>当值为 <c>-2</c> 时，该输入对应的值恒为 <c>1</c>。</para>
        /// </remarks>
        [JsonConverter(typeof(ArrayableInt32DictionaryConverter))]
        public Dictionary<string, IReadOnlyList<int>> Inputs
        {
            get => _inputs;
            set => _inputs = value?.Where(p => p.Value != null).ToDictionary(p => p.Key, p => p.Value) ??
                             new Dictionary<string, IReadOnlyList<int>>();
        }

        /// <summary>
        ///     监视器配置。
        /// </summary>
        public Dictionary<string, MonitorConfig> Monitors
        {
            get => _monitors;
            set => _monitors = value?.Where(p => p.Value != null).ToDictionary(p => p.Key, p => p.Value) ??
                               new Dictionary<string, MonitorConfig>();
        }

        /// <summary>
        ///     显示/界面相关配置。
        /// </summary>
        public DisplayConfig Display
        {
            get => _display;
            set => _display = value ?? new DisplayConfig();
        }

        /// <summary>
        ///     输出配置。
        /// </summary>
        public OutputsConfig Outputs
        {
            get => _outputs;
            set => _outputs = value ?? new OutputsConfig();
        }

        /// <summary>
        ///     是否启用配置热重载。
        /// </summary>
        /// <remarks>
        ///     <para>启用后，当配置文件发生变更时，游戏将自动加载并应用新配置。</para>
        ///     <para>该属性仅在初始化阶段确定，后续修改该属性不会切换热重载行为。</para>
        /// </remarks>
        public bool EnableHotReload { get; set; }

        public abstract string VehicleName { get; }
        public abstract HashSet<string> AllowedBuiltinInputPresets { get; }
        public virtual HashSet<string> AllowedInputs => BuiltinInputIds.All;
        public abstract Dictionary<string, MonitorResolution> MonitorResolutions { get; }
        public abstract Dictionary<string, int> MonitorIdOrders { get; }
        public abstract HashSet<string> AllowedSounds { get; }

        public virtual void Validate()
        {
            foreach (var monitor in Monitors.Values) monitor.Validate();
            foreach (var key in InputPresetNames)
            {
                if (AllowedBuiltinInputPresets.Contains(key)) continue;
                throw new JsonException($"Invalid input preset '{key}' for {VehicleName}.");
            }

            foreach (var key in Inputs.Keys)
            {
                if (AllowedInputs.Contains(key)) continue;
                throw new JsonException($"Invalid input id '{key}' for {VehicleName}.");
            }

            foreach (var pair in Monitors)
            {
                if (!MonitorResolutions.TryGetValue(pair.Key, out var monitorResolution))
                    throw new JsonException($"Invalid monitor name '{pair.Key}' for {VehicleName}.");

                if (!monitorResolution.IsValidResolution(pair.Value.Resolution))
                    throw new JsonException(
                        $"Invalid monitor resolution '{pair.Value.Resolution}' for {VehicleName} Monitor {pair.Key}.");
            }

            foreach (var pair in Outputs.Sound)
            {
                if (!AllowedSounds.Contains(pair.Key))
                    throw new JsonException($"Invalid sound '{pair.Key}' for {VehicleName}.");

                if (pair.Value == null) throw new JsonException($"Sound '{pair.Key}' cannot be null.");
            }
        }

        public string GetExternalDisplayName(string monitorId)
        {
            return $"JREMonitors {VehicleName} {monitorId}";
        }
    }

    public struct MonitorResolution
    {
        private readonly Dictionary<string, Size> _resolutions;
        private readonly Size _defaultResolution;

        public MonitorResolution(Dictionary<string, Size> resolutions, Size defaultResolution)
        {
            _resolutions = resolutions;
            _defaultResolution = defaultResolution;
        }

        public bool IsValidResolution(string resolutionString)
        {
            return resolutionString == null || _resolutions.ContainsKey(resolutionString);
        }

        public Size GetResolution(string resolutionString)
        {
            return resolutionString == null ? _defaultResolution : _resolutions[resolutionString];
        }
    }
}