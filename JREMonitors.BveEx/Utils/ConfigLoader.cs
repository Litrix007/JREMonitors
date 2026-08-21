using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using JREMonitors.BveEx.Converters;

namespace JREMonitors.BveEx.Utils
{
    public static class ConfigLoader
    {
        private static readonly JsonDocumentOptions DocumentOptions = new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        private static readonly Dictionary<JsonNode, string> NodeFilePaths = new Dictionary<JsonNode, string>();
        private static readonly HashSet<string> ReadFilePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static JsonSerializerOptions
            CreateSerializerOptions(IJsonTypeInfoResolver jsonTypeInfoResolver)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                Converters =
                {
                    new Vector2JsonConverter(), new Color3JsonConverter(),
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                },
                AllowOutOfOrderMetadataProperties = true
            };
            if (jsonTypeInfoResolver != null) options.TypeInfoResolver = jsonTypeInfoResolver;

            return options;
        }

        public static T LoadConfig<T>(string[] filePaths, out IReadOnlyList<string> readFilePaths,
            IJsonTypeInfoResolver jsonTypeInfoResolver = null, bool cascadeParent = true)
            where T : class
        {
            readFilePaths = Array.Empty<string>();
            try
            {
                JsonObject mergedObj = null;
                foreach (var filePath in filePaths)
                {
                    var node = LoadAndMerge(filePath, false, null, cascadeParent);
                    if (node is JsonObject obj)
                    {
                        if (mergedObj == null)
                            mergedObj = obj;
                        else
                            MergeJsonObjects(mergedObj, obj);
                    }
                    else if (node != null)
                    {
                        throw new InvalidOperationException(
                            $"The root of configuration file '{filePath}' must be a JSON object.");
                    }
                }

                if (mergedObj == null)
                    throw new FileNotFoundException(
                        $"Config file not found in any of the paths: {string.Join(", ", filePaths)}");
                var baseType = typeof(T);
                var discriminatorPropName = NodeConvertersResolver.GetTypeDiscriminatorPropertyName(baseType);
                var typeDiscriminator = mergedObj[discriminatorPropName]?.ToString();
                var converters = NodeConvertersResolver.GetConvertersForType(baseType, typeDiscriminator);
                Dictionary<string[], Func<JsonNode, string, JsonNode>> parsedConverters = null;
                if (converters != null && converters.Count > 0)
                {
                    parsedConverters = new Dictionary<string[], Func<JsonNode, string, JsonNode>>();
                    foreach (var kv in converters)
                    {
                        var pattern = kv.Key.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                        parsedConverters[pattern] = kv.Value;
                    }
                }

                if (parsedConverters != null) ApplyConverters(mergedObj, new List<string>(), parsedConverters);

                CleanReplaceKeys(mergedObj);
                var result = JsonSerializer.Deserialize<T>(mergedObj.ToJsonString(),
                    CreateSerializerOptions(jsonTypeInfoResolver));
                readFilePaths = ReadFilePaths.ToList();
                return result;
            }
            finally
            {
                NodeFilePaths.Clear();
                ReadFilePaths.Clear();
            }
        }

        public static T LoadConfig<T>(string[] filePaths, IJsonTypeInfoResolver jsonTypeInfoResolver = null,
            bool cascadeParent = true)
            where T : class
        {
            return LoadConfig<T>(filePaths, out _, jsonTypeInfoResolver, cascadeParent);
        }

        public static T LoadConfig<T>(string filePath, IJsonTypeInfoResolver jsonTypeInfoResolver = null,
            bool cascadeParent = true)
            where T : class
        {
            return LoadConfig<T>(new[] { filePath }, jsonTypeInfoResolver, cascadeParent);
        }

        public static void WriteConfig<T>(string filePath, T config, IJsonTypeInfoResolver jsonTypeInfoResolver = null)
            where T : class
        {
            var options = CreateSerializerOptions(jsonTypeInfoResolver);
            options.WriteIndented = true;
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(filePath, JsonSerializer.Serialize(config, options));
        }

        private static JsonNode LoadAndMerge(string filePath, bool mustExists, HashSet<string> visited,
            bool cascadeParent)
        {
            var absolutePath = Path.GetFullPath(filePath);
            if (visited == null) visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!visited.Add(absolutePath))
                throw new InvalidOperationException($"Circular dependency detected: {absolutePath}");

            if (!File.Exists(absolutePath))
            {
                if (!mustExists) return null;
                throw new FileNotFoundException($"Config file not found: {absolutePath}");
            }

            ReadFilePaths.Add(absolutePath);
            var jsonText = File.ReadAllText(absolutePath);
            var childNode = JsonNode.Parse(jsonText, documentOptions: DocumentOptions);

            if (childNode != null) RegisterPaths(childNode, absolutePath);

            if (!(childNode is JsonObject childObj)) return childNode;
            var parentPathAttr = childObj["parent"]?.ToString();
            if (!cascadeParent || string.IsNullOrEmpty(parentPathAttr)) return childNode;
            var parentPath = Path.Combine(Path.GetDirectoryName(absolutePath), parentPathAttr);

            var parentNode = LoadAndMerge(parentPath, true, visited, cascadeParent);
            if (!(parentNode is JsonObject parentObj)) return childNode;

            var baseObj = CloneAndMap(parentObj).AsObject();
            MergeJsonObjects(baseObj, childObj);
            baseObj.Remove("parent");
            return baseObj;
        }

        private static void MergeJsonObjects(JsonObject target, JsonObject source)
        {
            foreach (var property in source.ToArray())
            {
                var key = property.Key;
                var value = property.Value;
                var isReplaceMode = key.EndsWith("=");
                var cleanKey = isReplaceMode ? key.TrimEnd('=') : key;
                if (target.TryGetPropertyValue(cleanKey, out var existingValue))
                {
                    if (isReplaceMode)
                        target[cleanKey] = CloneAndMap(value);
                    else
                        switch (existingValue)
                        {
                            case JsonObject existingObj when value is JsonObject newObj:
                                MergeJsonObjects(existingObj, newObj);
                                break;
                            case JsonArray existingArr when value is JsonArray newArr:
                                MergeJsonArraysAppend(existingArr, newArr);
                                break;
                            default:
                                target[cleanKey] = CloneAndMap(value);
                                break;
                        }
                }
                else
                {
                    target[cleanKey] = CloneAndMap(value);
                }
            }
        }

        private static void MergeJsonArraysAppend(JsonArray target, JsonArray source)
        {
            foreach (var item in source) target.Add(CloneAndMap(item));
        }

        private static void RegisterPaths(JsonNode node, string filePath)
        {
            if (node == null) return;
            NodeFilePaths[node] = filePath;

            switch (node)
            {
                case JsonObject obj:
                {
                    foreach (var prop in obj) RegisterPaths(prop.Value, filePath);

                    break;
                }
                case JsonArray arr:
                {
                    foreach (var item in arr) RegisterPaths(item, filePath);

                    break;
                }
            }
        }

        private static JsonNode CloneAndMap(JsonNode node)
        {
            if (node == null) return null;
            var clone = node.DeepClone();
            CopyPathMappings(node, clone);
            return clone;
        }

        private static void CopyPathMappings(JsonNode source, JsonNode target)
        {
            if (source == null || target == null) return;
            if (NodeFilePaths.TryGetValue(source, out var path)) NodeFilePaths[target] = path;

            if (source is JsonObject sObj && target is JsonObject tObj)
            {
                foreach (var sProp in sObj)
                {
                    var key = sProp.Key;
                    var cleanKey = key.EndsWith("=") ? key.TrimEnd('=') : key;
                    if (tObj.TryGetPropertyValue(cleanKey, out var tValue)) CopyPathMappings(sProp.Value, tValue);
                }
            }
            else if (source is JsonArray sArr && target is JsonArray tArr)
            {
                var count = Math.Min(sArr.Count, tArr.Count);
                for (var i = 0; i < count; i++) CopyPathMappings(sArr[i], tArr[i]);
            }
        }

        private static void ApplyConverters(
            JsonNode node,
            List<string> currentPath,
            Dictionary<string[], Func<JsonNode, string, JsonNode>> parsedConverters)
        {
            switch (node)
            {
                case JsonObject obj:
                {
                    foreach (var prop in obj.ToArray())
                    {
                        var key = prop.Key;
                        var cleanKey = key.EndsWith("=") ? key.TrimEnd('=') : key;
                        currentPath.Add(cleanKey);
                        var childNode = obj[key];
                        if (childNode != null)
                        {
                            var replaced = Replace(childNode, currentPath, parsedConverters);
                            if (replaced != childNode) obj[key] = childNode = replaced;

                            ApplyConverters(childNode, currentPath, parsedConverters);
                        }

                        currentPath.RemoveAt(currentPath.Count - 1);
                    }

                    break;
                }
                case JsonArray arr:
                {
                    var count = arr.Count;
                    for (var i = 0; i < count; i++)
                    {
                        currentPath.Add(i.ToString());
                        var childNode = arr[i];
                        if (childNode != null)
                        {
                            var replaced = Replace(childNode, currentPath, parsedConverters);
                            if (replaced != childNode) arr[i] = childNode = replaced;

                            ApplyConverters(childNode, currentPath, parsedConverters);
                        }

                        currentPath.RemoveAt(currentPath.Count - 1);
                    }

                    break;
                }
            }
        }

        private static JsonNode Replace(
            JsonNode childNode,
            List<string> currentPath,
            Dictionary<string[], Func<JsonNode, string, JsonNode>> parsedConverters)
        {
            var childPathArray = currentPath.ToArray();
            foreach (var pair in parsedConverters)
                if (IsPatternMatch(childPathArray, pair.Key))
                    if (NodeFilePaths.TryGetValue(childNode, out var originalPath))
                    {
                        var result = pair.Value(childNode, originalPath);
                        NodeFilePaths[result] = originalPath;
                        return result;
                    }

            return childNode;
        }

        private static bool IsPatternMatch(string[] currentPath, string[] pattern)
        {
            if (currentPath.Length != pattern.Length) return false;
            for (var i = 0; i < currentPath.Length; i++)
            {
                var pat = pattern[i];
                if (pat != "*" && !string.Equals(currentPath[i], pat, StringComparison.OrdinalIgnoreCase)) return false;
            }

            return true;
        }

        private static void CleanReplaceKeys(JsonNode node)
        {
            switch (node)
            {
                case JsonObject obj:
                {
                    foreach (var property in obj.ToArray())
                    {
                        var key = property.Key;
                        var value = property.Value;

                        if (value != null) CleanReplaceKeys(value);

                        if (!key.EndsWith("=")) continue;
                        var cleanKey = key.TrimEnd('=');
                        obj.Remove(key);
                        obj[cleanKey] = value;
                    }

                    break;
                }
                case JsonArray arr:
                {
                    var count = arr.Count;
                    for (var i = 0; i < count; i++)
                        if (arr[i] != null)
                            CleanReplaceKeys(arr[i]);

                    break;
                }
            }
        }
    }
}