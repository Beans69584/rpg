using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using RPG.Common;
using RPG.Core.Player;
using RPG.Core.Player.Common;

namespace RPG.Player.Common
{
    public class PersonJsonConverter : JsonConverter<Person>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(Person).IsAssignableFrom(typeToConvert);
        }

        public override Person? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            using JsonDocument doc = JsonDocument.ParseValue(ref reader);
            JsonElement root = doc.RootElement;

            // Read the type discriminator
            if (!root.TryGetProperty("Type", out JsonElement typeProperty))
            {
                throw new JsonException("Type discriminator missing");
            }

            string typeString = typeProperty.GetString() ??
                throw new JsonException("Invalid type discriminator");

            // Create the appropriate type based on the discriminator
            Person? person = typeString switch
            {
                "Warrior" => new Warrior(),
                "Barbarian" => new Barbarian(),
                "Paladin" => new Paladin(),
                "Sorcerer" => new Sorcerer(),
                "Conjurer" => new Conjurer(),
                "SithSorcerer" => new SithSorcerer(),
                _ => throw new JsonException($"Unknown person type: {typeString}")
            };

            // Deserialize the properties
            foreach (JsonProperty property in root.EnumerateObject())
            {
                switch (property.Name)
                {
                    case "Name":
                        person.Name = property.Value.GetString() ?? "";
                        break;
                    case "Age":
                        person.Age = property.Value.GetInt32();
                        break;
                    case "Position":
                        person.Position = JsonSerializer.Deserialize<Vector2>(property.Value.GetRawText(), options) ?? new Vector2();
                        break;
                    case "HitPoints":
                        person.HitPoints = property.Value.GetInt32();
                        break;
                    case "Gold":
                        person.Gold = property.Value.GetInt32();
                        break;
                    case "Strength":
                        person.Strength = property.Value.GetInt32();
                        break;
                    case "Dexterity":
                        person.Dexterity = property.Value.GetInt32();
                        break;
                    case "Constitution":
                        person.Constitution = property.Value.GetInt32();
                        break;
                    case "Intelligence":
                        person.Intelligence = property.Value.GetInt32();
                        break;
                    case "Wisdom":
                        person.Wisdom = property.Value.GetInt32();
                        break;
                    case "Charisma":
                        person.Charisma = property.Value.GetInt32();
                        break;
                    case "Inventory":
                        person.Inventory = JsonSerializer.Deserialize<List<string>>(property.Value.GetRawText(), options) ?? [];
                        break;
                    case "Equipment":
                        person.Equipment = JsonSerializer.Deserialize<Dictionary<string, string>>(property.Value.GetRawText(), options) ?? [];
                        break;
                }

                // Handle class-specific properties
                if (person is Warrior warrior)
                {
                    if (property.Name == "CombatSkill")
                        warrior.CombatSkill = property.Value.GetInt32();
                }
                else if (person is Magician magician)
                {
                    if (property.Name == "MagicPower")
                        magician.MagicPower = property.Value.GetInt32();
                    else if (property.Name == "KnownSpells")
                        magician.KnownSpells = JsonSerializer.Deserialize<List<Spell>>(property.Value.GetRawText(), options) ?? [];
                }
                else if (person is Barbarian barbarian)
                {
                    if (property.Name == "BerserkLevel")
                        barbarian.BerserkLevel = property.Value.GetInt32();
                }
                else if (person is Paladin paladin)
                {
                    if (property.Name == "HolyPower")
                        paladin.HolyPower = property.Value.GetInt32();
                }
                else if (person is Sorcerer sorcerer && property.Name == "EvilLevel")
                {
                    sorcerer.EvilLevel = property.Value.GetInt32();
                }
            }

            return person;
        }

        public override void Write(Utf8JsonWriter writer, Person value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            // Write type discriminator
            writer.WriteString("Type", value.GetType().Name);

            // Write base properties
            writer.WriteString("Name", value.Name);
            writer.WriteNumber("Age", value.Age);
            writer.WritePropertyName("Position");
            JsonSerializer.Serialize(writer, value.Position, options);
            writer.WriteNumber("HitPoints", value.HitPoints);
            writer.WriteNumber("Gold", value.Gold);
            writer.WriteNumber("Strength", value.Strength);
            writer.WriteNumber("Dexterity", value.Dexterity);
            writer.WriteNumber("Constitution", value.Constitution);
            writer.WriteNumber("Intelligence", value.Intelligence);
            writer.WriteNumber("Wisdom", value.Wisdom);
            writer.WriteNumber("Charisma", value.Charisma);

            writer.WritePropertyName("Inventory");
            JsonSerializer.Serialize(writer, value.Inventory, options);
            writer.WritePropertyName("Equipment");
            JsonSerializer.Serialize(writer, value.Equipment, options);

            // Write class-specific properties
            if (value is Warrior warrior)
            {
                writer.WriteNumber("CombatSkill", warrior.CombatSkill);
            }
            else if (value is Magician magician)
            {
                writer.WriteNumber("MagicPower", magician.MagicPower);
                writer.WritePropertyName("KnownSpells");
                JsonSerializer.Serialize(writer, magician.KnownSpells, options);
            }
            else if (value is Barbarian barbarian)
            {
                writer.WriteNumber("BerserkLevel", barbarian.BerserkLevel);
            }
            else if (value is Paladin paladin)
            {
                writer.WriteNumber("HolyPower", paladin.HolyPower);
            }
            else if (value is Sorcerer sorcerer)
            {
                writer.WriteNumber("EvilLevel", sorcerer.EvilLevel);
            }

            writer.WriteEndObject();
        }
    }
}