using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JREMonitors.BveEx.Configs.Vehicle
{
    public class ConfigPath
    {
        /// <summary>
        ///     配置文件的绝对路径，由反序列化时的节点转换器自动填充。
        /// </summary>
        public string AbsoluteConfigPath { get; set; }

        /// <summary>
        ///     文件路径，相对于配置文件所在目录，也支持绝对路径。
        /// </summary>
        public string Value { get; set; }

        public string GetAbsolutePath()
        {
            if (string.IsNullOrEmpty(Value)) return null;

            string fullPath;
            if (string.IsNullOrEmpty(AbsoluteConfigPath))
            {
                fullPath = Path.GetFullPath(Value);
            }
            else
            {
                var directory = Path.GetDirectoryName(AbsoluteConfigPath);
                fullPath = Path.GetFullPath(Path.Combine(directory ?? string.Empty, Value));
            }

            return File.Exists(fullPath) ? fullPath : null;
        }

        public static JsonNode NodeConverter(JsonNode node, string absoluteConfigPath)
        {
            if (node.GetValueKind() != JsonValueKind.String) return node;
            var value = node.GetValue<string>();
            return new JsonObject
            {
                ["absoluteConfigPath"] = absoluteConfigPath,
                ["value"] = value
            };
        }
    }
}