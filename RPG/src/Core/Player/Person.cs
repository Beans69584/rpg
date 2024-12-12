using RPG.Common;
using RPG.World.Data;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPG.Core.Player
{
    public abstract class Person
    {
        public string Name { get; set; } = "Unknown";
        public int Age { get; set; }
        public Vector2 Position { get; set; } = new Vector2(0, 0);
        public int HitPoints { get; set; }
        public int Gold { get; set; }
        public int Strength { get; set; }
        public int Dexterity { get; set; }
        public int Constitution { get; set; }
        public int Intelligence { get; set; }
        public int Wisdom { get; set; }
        public int Charisma { get; set; }
        public int Speed { get; set; }
        public List<string> Inventory { get; set; } = [];
        public Dictionary<string, string> Equipment { get; set; } = [];

        /// <summary>
        /// Restores state from dynamic properties
        /// </summary>
        internal void RestoreDynamicState(Dictionary<string, JsonElement> state, JsonSerializerOptions options)
        {
            foreach (PropertyInfo prop in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                try
                {
                    if (!prop.CanWrite || prop.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                        continue;

                    if (state.TryGetValue(prop.Name, out JsonElement element))
                    {
                        object? value = JsonSerializer.Deserialize(element.GetRawText(), prop.PropertyType, options);
                        prop.SetValue(this, value);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to restore property {prop.Name}: {ex.Message}");
                }
            }
        }

        public virtual Dictionary<string, object?> SerializeState()
        {
            Dictionary<string, object?> state = [];
            foreach (PropertyInfo prop in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.CanRead && prop.GetCustomAttribute<JsonIgnoreAttribute>() == null)
                {
                    state[prop.Name] = prop.GetValue(this);
                }
            }
            return state;
        }

        public virtual void RestoreState(Dictionary<string, object?> state)
        {
            foreach (PropertyInfo prop in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.CanWrite && state.ContainsKey(prop.Name) && prop.GetCustomAttribute<JsonIgnoreAttribute>() == null)
                {
                    prop.SetValue(this, state[prop.Name]);
                }
            }
        }
    }
}