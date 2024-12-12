namespace RPG.Combat
{
    /// <summary>
    /// Defines the types of actions that can be performed in combat.
    /// </summary>
    public enum CombatActionType
    {
        /// <summary>
        /// Represents an offensive action against a target.
        /// </summary>
        Attack,

        /// <summary>
        /// Represents a defensive stance to reduce incoming damage.
        /// </summary>
        Defend,

        /// <summary>
        /// Represents the usage of an item from the inventory.
        /// </summary>
        UseItem,

        /// <summary>
        /// Represents an attempt to escape from combat.
        /// </summary>
        Flee
    }

    /// <summary>
    /// Represents a combat action performed by an entity during battle.
    /// </summary>
    public class CombatAction
    {
        /// <summary>
        /// Gets or sets the type of combat action being performed.
        /// </summary>
        public CombatActionType Type { get; set; }

        /// <summary>
        /// Gets or sets the entity performing the action.
        /// </summary>
        public required CombatEntity Source { get; set; }

        /// <summary>
        /// Gets or sets the entity targeted by the action.
        /// </summary>
        public required CombatEntity Target { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the item being used, if applicable.
        /// </summary>
        public string? ItemId { get; set; }
    }
}