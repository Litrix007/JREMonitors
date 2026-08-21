using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using JREMonitors.Core.Utils;
using Vortice.Mathematics;

namespace JREMonitors.BveEx.Converters
{
    public class Color3JsonConverter : JsonConverter<Color3>
    {
        public override Color3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                {
                    var hex = reader.GetString();
                    try
                    {
                        return hex.ToColor3();
                    }
                    catch (FormatException ex)
                    {
                        throw new JsonException($"Invalid hex format for Color3: {hex}", ex);
                    }
                }
                case JsonTokenType.StartArray:
                {
                    reader.Read();
                    var r = reader.GetSingle();
                    reader.Read();
                    var g = reader.GetSingle();
                    reader.Read();
                    var b = reader.GetSingle();
                    reader.Read();

                    if (reader.TokenType != JsonTokenType.EndArray)
                        throw new JsonException("Expected end of array for Color3.");
                    if (r > 1.0f || g > 1.0f || b > 1.0f) return new Color3(r / 255.0f, g / 255.0f, b / 255.0f);

                    return new Color3(r, g, b);
                }
                case JsonTokenType.StartObject:
                {
                    float r = 0, g = 0, b = 0;
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndObject)
                            break;

                        if (reader.TokenType == JsonTokenType.PropertyName)
                        {
                            var propName = reader.GetString().ToLower();
                            reader.Read();
                            if (propName == "r") r = reader.GetSingle();
                            else if (propName == "g") g = reader.GetSingle();
                            else if (propName == "b") b = reader.GetSingle();
                        }
                    }

                    return new Color3(r, g, b);
                }
                default:
                    throw new JsonException("Unexpected JSON token for Color3.");
            }
        }

        public override void Write(Utf8JsonWriter writer, Color3 value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("r", value.R);
            writer.WriteNumber("g", value.G);
            writer.WriteNumber("b", value.B);
            writer.WriteEndObject();
        }
    }
}