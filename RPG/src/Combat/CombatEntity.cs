using RPG.Common;
using RPG.Core.Player;
using RPG.World.Data;

namespace RPG.Combat
{
    public class CombatEntity
    {
        public string Name { get; }
        public EntityStats Stats { get; }
        public int CurrentHP { get; set; }
        public int MaxHP { get; }
        public bool IsDefending { get; set; }
        public bool IsPlayer { get; }

        public CombatEntity(Entity entity, WorldData worldData)
        {
            Name = worldData.GetString(entity.NameId);
            Stats = entity.Stats;
            CurrentHP = MaxHP;
            IsPlayer = false;
        }

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