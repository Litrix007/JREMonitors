using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace JREMonitors.BveEx.Utils
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method, Inherited = false)]
    public class NodeConvertersAttribute : Attribute
    {
    }

    public static class NodeConvertersResolver
    {
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                                           BindingFlags.DeclaredOnly;

        private static readonly Dictionary<Type, Dictionary<string, Type>> BaseToDerivedMapCache =
            new Dictionary<Type, Dictionary<string, Type>>();

        private static readonly Dictionary<Type, Dictionary<string, Func<JsonNode, string, JsonNode>>>
            TypeConvertersCache =
                new Dictionary<Type, Dictionary<string, Func<JsonNode, string, JsonNode>>>();

        public static string GetTypeDiscriminatorPropertyName(Type baseType)
        {
            var polyAttr = baseType.GetCustomAttribute<JsonPolymorphicAttribute>(true);
            return polyAttr?.TypeDiscriminatorPropertyName ?? "type";
        }

        private static Dictionary<string, Type> GetOrBuildDerivedMap(Type baseType)
        {
            if (BaseToDerivedMapCache.TryGetValue(baseType, out var map)) return map;

            map = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

            var attributes = baseType.GetCustomAttributes<JsonDerivedTypeAttribute>(true);
            foreach (var attr in attributes)
                if (attr.TypeDiscriminator is string discriminator)
                    map[discriminator] = attr.DerivedType;

            BaseToDerivedMapCache[baseType] = map;
            return map;
        }

        private static Dictionary<string, Func<JsonNode, string, JsonNode>> GetLocalTypeConverters(Type type)
        {
            if (TypeConvertersCache.TryGetValue(type, out var converters)) return converters;

            foreach (var field in type.GetFields(Flags))
                if (field.GetCustomAttribute<NodeConvertersAttribute>() != null)
                {
                    converters = field.GetValue(null) as Dictionary<string, Func<JsonNode, string, JsonNode>>;
                    break;
                }

            if (converters == null)
                foreach (var prop in type.GetProperties(Flags))
                    if (prop.GetCustomAttribute<NodeConvertersAttribute>() != null)
                    {
                        converters = prop.GetValue(null) as Dictionary<string, Func<JsonNode, string, JsonNode>>;
                        break;
                    }

            if (converters == null)
                foreach (var method in type.GetMethods(Flags))
                    if (method.GetCustomAttribute<NodeConvertersAttribute>() != null &&
                        method.GetParameters().Length == 0)
                    {
                        converters = method.Invoke(null, null) as Dictionary<string, Func<JsonNode, string, JsonNode>>;
                        break;
                    }

            TypeConvertersCache[type] = converters;
            return converters;
        }

        public static Dictionary<string, Func<JsonNode, string, JsonNode>> GetConvertersForType(Type baseType,
            string discriminator)
        {
            var targetType = baseType;
            if (!string.IsNullOrEmpty(discriminator))
            {
                var derivedMap = GetOrBuildDerivedMap(baseType);
                if (derivedMap.TryGetValue(discriminator, out var concreteType)) targetType = concreteType;
            }

            var inheritanceChain = new List<Type>();
            var current = targetType;
            while (current != null)
            {
                inheritanceChain.Add(current);
                if (current == baseType) break;
                current = current.BaseType;
            }

            inheritanceChain.Reverse();
            var mergedConverters = new Dictionary<string, Func<JsonNode, string, JsonNode>>();
            foreach (var type in inheritanceChain)
            {
                var localConverters = GetLocalTypeConverters(type);
                if (localConverters == null) continue;
                foreach (var pair in localConverters) mergedConverters[pair.Key] = pair.Value;
            }

            return mergedConverters;
        }
    }
}