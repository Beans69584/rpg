using RPG.Common;
using RPG.Core.Player;
using RPG.World.Data;

namespace RPG.Combat
{
    /// <summary>
    /// Represents an entity within the combat system, handling both player and non-player characters.
    /// </summary>
    public class CombatEntity
    {
        /// <summary>
        /// Gets the name of the combat entity.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the base statistics of the combat entity, including attributes like strength and dexterity.
        /// </summary>
        public EntityStats Stats { get; }

        /// <summary>
        /// Gets or sets the current hit points of the combat entity.
        /// </summary>
        public int CurrentHP { get; set; }

        /// <summary>
        /// Gets the maximum hit points the combat entity can have.
        /// </summary>
        public int MaxHP { get; }

        /// <summary>
        /// Gets or sets whether the entity is in a defensive stance.
        /// </summary>
        public bool IsDefending { get; set; }

        /// <summary>
        /// Gets whether the entity is a player character.
        /// </summary>
        public bool IsPlayer { get; }

        /// <summary>
        /// Initialises a new instance of the <see cref="CombatEntity"/> class for a non-player character.
        /// </summary>
        /// <param name="entity">The base entity data.</param>
        /// <param name="worldData">The world data containing localised strings.</param>
        public CombatEntity(Entity entity, WorldData worldData)
        {
            Name = worldData.GetString(entity.NameId);
            Stats = entity.Stats;
            CurrentHP = MaxHP;
            IsPlayer = false;
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="CombatEntity"/> class for a player character.
        /// </summary>
        /// <param name="person">The person object representing the player character.</param>
        public CombatEntity(Person person)
        {
            Name = person.Name;
            Stats = new()
            {
                Strength = person.Strength,
                Dexterity = person.Dexterity,
                Intelligence = person.Intelligence,
                Defense = person.Constitution,
                Speed = person.Speed
            };
            CurrentHP = person.HitPoints;
            IsPlayer = true;
        }
    }
}