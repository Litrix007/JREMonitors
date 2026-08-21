using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JREMonitors.BveEx.Utils
{
    public class ArrayableInt32DictionaryConverter : JsonConverter<Dictionary<string, IReadOnlyList<int>>>
    {
        public override Dictionary<string, IReadOnlyList<int>> Read(ref Utf8JsonReader reader, Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected a JSON object.");
            var dictionary = new Dictionary<string, IReadOnlyList<int>>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return dictionary;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException("Expected a property name.");
                var propertyName = reader.GetString();
                reader.Read();
                IReadOnlyList<int> value;
                switch (reader.TokenType)
                {
                    case JsonTokenType.Null:
                        value = Array.Empty<int>();
                        break;
                    case JsonTokenType.Number:
                        value = new[] { reader.GetInt32() };
                        break;
                    case JsonTokenType.StartArray:
                        var list = new List<int>();
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.EndArray) break;
                            if (reader.TokenType == JsonTokenType.Number) list.Add(reader.GetInt32());
                        }

                        value = list;
                        break;
                    default:
                        throw new JsonException(
                            $"Expected an integer or an array of integers for '{propertyName}'.");
                }

                dictionary[propertyName] = value;
            }

            throw new JsonException("Expected EndObject token.");
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<string, IReadOnlyList<int>> value,
            JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            foreach (var pair in value)
            {
                writer.WritePropertyName(pair.Key);
                if (pair.Value == null || pair.Value.Count == 0)
                {
                    writer.WriteNullValue();
                }
                else if (pair.Value.Count == 1)
                {
                    writer.WriteNumberValue(pair.Value[0]);
                }
                else
                {
                    writer.WriteStartArray();
                    foreach (var item in pair.Value) writer.WriteNumberValue(item);
                    writer.WriteEndArray();
                }
            }

            writer.WriteEndObject();
        }
    }
}