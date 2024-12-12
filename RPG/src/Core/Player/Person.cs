using RPG.Common;
using RPG.World.Data;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPG.Core.Player
{
    /// <summary>
    /// Represents a base character within the game world with fundamental attributes and capabilities.
    /// </summary>
    public abstract class Person
    {
        /// <summary>
        /// Gets or sets the character's name. Defaults to "Unknown" if not specified.
        /// </summary>
        public string Name { get; set; } = "Unknown";

        /// <summary>
        /// Gets or sets the character's age in years.
        /// </summary>
        public int Age { get; set; }

        /// <summary>
        /// Gets or sets the character's current position in the game world using a 2D vector.
        /// </summary>
        public Vector2 Position { get; set; } = new Vector2(0, 0);

        /// <summary>
        /// Gets or sets the character's current hit points, representing their health.
        /// </summary>
        public int HitPoints { get; set; }

        /// <summary>
        /// Gets or sets the amount of gold the character possesses.
        /// </summary>
        public int Gold { get; set; }

        /// <summary>
        /// Gets or sets the character's Strength attribute, affecting physical combat and carrying capacity.
        /// </summary>
        public int Strength { get; set; }

        /// <summary>
        /// Gets or sets the character's Dexterity attribute, affecting accuracy and reflexes.
        /// </summary>
        public int Dexterity { get; set; }

        /// <summary>
        /// Gets or sets the character's Constitution attribute, affecting health and stamina.
        /// </summary>
        public int Constitution { get; set; }

        /// <summary>
        /// Gets or sets the character's Intelligence attribute, affecting magical abilities and knowledge.
        /// </summary>
        public int Intelligence { get; set; }

        /// <summary>
        /// Gets or sets the character's Wisdom attribute, affecting perception and judgement.
        /// </summary>
        public int Wisdom { get; set; }

        /// <summary>
        /// Gets or sets the character's Charisma attribute, affecting social interactions.
        /// </summary>
        public int Charisma { get; set; }

        /// <summary>
        /// Gets or sets the character's movement speed.
        /// </summary>
        public int Speed { get; set; }

        /// <summary>
        /// Gets or sets the character's inventory, containing a list of item identifiers.
        /// </summary>
        public List<string> Inventory { get; set; } = [];

        /// <summary>
        /// Gets or sets the character's equipped items, mapping equipment slots to item identifiers.
        /// </summary>
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

        /// <summary>
        /// Serialises the character's current state into a dictionary format.
        /// </summary>
        /// <returns>A dictionary containing all serialisable properties and their values.</returns>
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

        /// <summary>
        /// Restores the character's state from a previously serialised dictionary.
        /// </summary>
        /// <param name="state">A dictionary containing the property names and values to restore.</param>
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