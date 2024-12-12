namespace RPG.Combat
{
    public enum CombatActionType
    {
        Attack,
        Defend,
        UseItem,
        Flee
    }

    public class CombatAction
    {
        public CombatActionType Type { get; set; }
        public required CombatEntity Source { get; set; }
        public required CombatEntity Target { get; set; }
        public string? ItemId { get; set; }
    }
}