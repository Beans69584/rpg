using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPG.Common
{
    [JsonConverter(typeof(Vector2JsonConverter))]
    public class Vector2
    {
        public float X { get; set; }
        public float Y { get; set; }

        public Vector2()
        {
            X = 0;
            Y = 0;
        }

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public override bool Equals(object? obj)
        {
            const float tolerance = 0.0001f;
            return obj is Vector2 other && Math.Abs(X - other.X) < tolerance && Math.Abs(Y - other.Y) < tolerance;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public override string ToString()
        {
            return $"{X},{Y}";
        }

        public static Vector2? Parse(string? s)
        {
            if (string.IsNullOrEmpty(s) || s.Split(',').Length != 2) return null;

            string[] parts = s.Split(',');
            return float.TryParse(parts[0], out float x) && float.TryParse(parts[1], out float y) ? new Vector2(x, y) : null;
        }
    }

    public class Vector2JsonConverter : JsonConverter<Vector2>
    {
        public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string? value = reader.GetString();
                Vector2? result = Vector2.Parse(value);
                if (result != null) return result;
            }
            throw new JsonException("Invalid Vector2 format");
        }

        public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }

        public bool CanConvertKey => true;

        public override Vector2 ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? value = reader.GetString();
            Vector2? result = Vector2.Parse(value);
            return result ?? throw new JsonException("Invalid Vector2 format for dictionary key");
        }

        public override void WriteAsPropertyName(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
        {
            writer.WritePropertyName(value.ToString());
        }
    }
}