using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPG.Common
{
    /// <summary>
    /// Represents a 2D vector with X and Y coordinates in float-point precision.
    /// </summary>
    [JsonConverter(typeof(Vector2JsonConverter))]
    public class Vector2
    {
        /// <summary>
        /// Gets or sets the X coordinate of the vector.
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// Gets or sets the Y coordinate of the vector.
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// Initialises a new instance of the Vector2 class with coordinates (0,0).
        /// </summary>
        public Vector2()
        {
            X = 0;
            Y = 0;
        }

        /// <summary>
        /// Initialises a new instance of the Vector2 class with specified coordinates.
        /// </summary>
        /// <param name="x">The X coordinate of the vector.</param>
        /// <param name="y">The Y coordinate of the vector.</param>
        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Determines whether this vector is equal to another vector within a small tolerance.
        /// </summary>
        /// <param name="obj">The object to compare with the current vector.</param>
        /// <returns>true if the specified object is equal to the current vector within a tolerance of 0.0001; otherwise, false.</returns>
        public override bool Equals(object? obj)
        {
            const float tolerance = 0.0001f;
            return obj is Vector2 other && Math.Abs(X - other.X) < tolerance && Math.Abs(Y - other.Y) < tolerance;
        }

        /// <summary>
        /// Generates a hash code for this vector.
        /// </summary>
        /// <returns>A hash code value that represents this vector.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        /// <summary>
        /// Converts the vector to its string representation in the format "X,Y".
        /// </summary>
        /// <returns>A string that represents this vector in the format "X,Y".</returns>
        public override string ToString()
        {
            return $"{X},{Y}";
        }

        /// <summary>
        /// Attempts to parse a string into a Vector2 object.
        /// </summary>
        /// <param name="s">The string to parse, expected in the format "X,Y".</param>
        /// <returns>A new Vector2 if parsing was successful; otherwise, null.</returns>
        public static Vector2? Parse(string? s)
        {
            if (string.IsNullOrEmpty(s) || s.Split(',').Length != 2) return null;

            string[] parts = s.Split(',');
            return float.TryParse(parts[0], out float x) && float.TryParse(parts[1], out float y) ? new Vector2(x, y) : null;
        }
    }

    /// <summary>
    /// Provides JSON conversion functionality for the Vector2 class.
    /// </summary>
    public class Vector2JsonConverter : JsonConverter<Vector2>
    {
        /// <summary>
        /// Reads and converts JSON to a Vector2 object.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">The serialiser options.</param>
        /// <returns>A Vector2 object converted from JSON.</returns>
        /// <exception cref="JsonException">Thrown when the JSON is in an invalid format for Vector2.</exception>
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

        /// <summary>
        /// Writes a Vector2 object to JSON.
        /// </summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="value">The Vector2 to convert.</param>
        /// <param name="options">The serialiser options.</param>
        public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }

        /// <summary>
        /// Gets a value indicating whether this converter can convert dictionary keys.
        /// </summary>
        public bool CanConvertKey => true;

        /// <summary>
        /// Reads and converts JSON property name to a Vector2 object.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">The serialiser options.</param>
        /// <returns>A Vector2 object converted from the JSON property name.</returns>
        /// <exception cref="JsonException">Thrown when the JSON property name is in an invalid format for Vector2.</exception>
        public override Vector2 ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? value = reader.GetString();
            Vector2? result = Vector2.Parse(value);
            return result ?? throw new JsonException("Invalid Vector2 format for dictionary key");
        }

        /// <summary>
        /// Writes a Vector2 object as a JSON property name.
        /// </summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="value">The Vector2 to convert.</param>
        /// <param name="options">The serialiser options.</param>
        public override void WriteAsPropertyName(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
        {
            writer.WritePropertyName(value.ToString());
        }
    }
}