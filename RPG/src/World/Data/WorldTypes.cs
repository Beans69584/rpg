using System;
using System.Collections.Generic;

namespace RPG.World.Data
{
    /// <summary>
    /// Represents the header information for a world save file.
    /// </summary>
    public class Header
    {
        /// <summary>
        /// Gets or sets the magic identifier for the file format. Default value is "RPGW".
        /// </summary>
        public string Magic { get; set; } = "RPGW";

        /// <summary>
        /// Gets or sets the localisation ID for the world name.
        /// </summary>
        public int NameId { get; set; }

        /// <summary>
        /// Gets or sets the localisation ID for the world description.
        /// </summary>
        public int DescriptionId { get; set; }

        /// <summary>
        /// Gets or sets the version of the world format. Default value is "1.0".
        /// </summary>
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// Gets or sets the UTC timestamp when the world was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the seed used for world generation.
        /// </summary>
        public string Seed { get; set; } = "";

        /// <summary>
        /// Gets or sets the total number of regions in the world.
        /// </summary>
        public int RegionCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of NPCs in the world.
        /// </summary>
        public int NPCCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of items in the world.
        /// </summary>
        public int ItemCount { get; set; }
    }

    /// <summary>
    /// Defines the various roles that NPCs can fulfil in the game world.
    /// </summary>
    public enum NPCRole
    {
        /// <summary>
        /// A common townsperson or village resident.
        /// </summary>
        Villager,

        /// <summary>
        /// An NPC who runs a shop and sells goods.
        /// </summary>
        Shopkeeper,

        /// <summary>
        /// An NPC who manages an inn or tavern.
        /// </summary>
        Innkeeper,

        /// <summary>
        /// A town guard or security officer.
        /// </summary>
        Guard,

        /// <summary>
        /// An NPC who provides quests to the player.
        /// </summary>
        QuestGiver,

        /// <summary>
        /// An NPC skilled in creating or repairing items.
        /// </summary>
        Craftsman,

        /// <summary>
        /// A religious or spiritual figure.
        /// </summary>
        Priest,

        /// <summary>
        /// A travelling trader or merchant.
        /// </summary>
        Merchant,

        /// <summary>
        /// An NPC who can train the player in skills.
        /// </summary>
        Trainer,

        /// <summary>
        /// An educated NPC who provides knowledge and information.
        /// </summary>
        Scholar
    }

    /// <summary>
    /// Represents a character or creature entity in the game world.
    /// </summary>
    public class Entity
    {
        /// <summary>
        /// Gets or sets the localisation ID for the entity's name.
        /// </summary>
        public int NameId { get; set; }

        /// <summary>
        /// Gets or sets the level of the entity.
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Gets or sets the hit points of the entity.
        /// </summary>
        public int HP { get; set; }

        /// <summary>
        /// Gets or sets the role of the NPC.
        /// </summary>
        public NPCRole Role { get; set; }

        /// <summary>
        /// Gets or sets the profession of the entity.
        /// </summary>
        public ProfessionType Profession { get; set; }

        /// <summary>
        /// Gets or sets the stats of the entity.
        /// </summary>
        public EntityStats Stats { get; set; } = new();

        /// <summary>
        /// Gets or sets the profession-specific stats of the entity.
        /// </summary>
        public Dictionary<string, int> ProfessionStats { get; set; } = [];

        /// <summary>
        /// Gets or sets the references to the dialogue trees associated with the entity.
        /// </summary>
        public List<int> DialogueTreeRefs { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of quests available from the entity.
        /// </summary>
        public List<int> AvailableQuests { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of quests completed by the entity.
        /// </summary>
        public List<int> CompletedQuests { get; set; } = [];

        /// <summary>
        /// Gets or sets the flags associated with the entity.
        /// </summary>
        public Dictionary<string, bool> Flags { get; set; } = [];
    }

    /// <summary>
    /// Defines the various professions available to players and NPCs.
    /// </summary>
    public enum ProfessionType
    {
        /// <summary>
        /// A skilled fighter specialising in physical combat.
        /// </summary>
        Warrior,

        /// <summary>
        /// A practitioner of traditional magic arts.
        /// </summary>
        Magician,

        /// <summary>
        /// A holy warrior combining combat skills with divine magic.
        /// </summary>
        Paladin,

        /// <summary>
        /// A fierce warrior who draws power from rage.
        /// </summary>
        Barbarian,

        /// <summary>
        /// A mage specialising in summoning creatures and elementals.
        /// </summary>
        Conjurer,

        /// <summary>
        /// A spellcaster focusing on powerful destructive magic.
        /// </summary>
        Sorcerer,

        /// <summary>
        /// A dark magic user drawing power from forbidden sources.
        /// </summary>
        SithSorcerer,

        /// <summary>
        /// A merchant NPC profession.
        /// </summary>
        Merchant_NPC,

        /// <summary>
        /// An innkeeper NPC profession.
        /// </summary>
        Innkeeper_NPC,

        /// <summary>
        /// A blacksmith NPC profession.
        /// </summary>
        Blacksmith_NPC
    }

    /// <summary>
    /// Represents a dialogue tree structure for NPC interactions.
    /// </summary>
    public class DialogueTree
    {
        /// <summary>
        /// Gets or sets the ID of the root node in the dialogue tree.
        /// </summary>
        public int RootNodeId { get; set; }

        /// <summary>
        /// Gets or sets the collection of dialogue nodes in the tree.
        /// </summary>
        public Dictionary<int, DialogueNode> Nodes { get; set; } = [];

        /// <summary>
        /// Gets or sets the flags associated with the dialogue tree.
        /// </summary>
        public Dictionary<string, bool> Flags { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of required flags for the dialogue tree.
        /// </summary>
        public List<string> RequiredFlags { get; set; } = [];
    }

    /// <summary>
    /// Represents a single node in a dialogue tree.
    /// </summary>
    public class DialogueNode
    {
        /// <summary>
        /// Gets or sets the localisation ID for the dialogue text.
        /// </summary>
        public int TextId { get; set; }

        /// <summary>
        /// Gets or sets the list of responses available from this node.
        /// </summary>
        public List<DialogueResponse> Responses { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of actions to be performed when this node is selected.
        /// </summary>
        public List<DialogueAction> Actions { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of required flags for this node.
        /// </summary>
        public List<string> RequiredFlags { get; set; } = [];

        /// <summary>
        /// Gets or sets the script to be executed when this node is selected.
        /// </summary>
        public string? OnSelectScript { get; set; }
    }

    /// <summary>
    /// Represents a response option in a dialogue node.
    /// </summary>
    public class DialogueResponse
    {
        /// <summary>
        /// Gets or sets the localisation ID for the response text.
        /// </summary>
        public int TextId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the next node to navigate to after this response.
        /// </summary>
        public int NextNodeId { get; set; }

        /// <summary>
        /// Gets or sets the list of required flags for this response.
        /// </summary>
        public List<string> RequiredFlags { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of actions to be performed when this response is selected.
        /// </summary>
        public List<DialogueAction> Actions { get; set; } = [];

        /// <summary>
        /// Gets or sets the condition to be evaluated for this response.
        /// </summary>
        public string? Condition { get; set; }
    }

    /// <summary>
    /// Represents an action to be performed during a dialogue.
    /// </summary>
    public class DialogueAction
    {
        /// <summary>
        /// Gets or sets the type of action (e.g., SetFlag, GiveQuest, GiveItem).
        /// </summary>
        public string Type { get; set; } = "";

        /// <summary>
        /// Gets or sets the target of the action (e.g., Flag name, Quest ID, Item ID).
        /// </summary>
        public string Target { get; set; } = "";

        /// <summary>
        /// Gets or sets the value associated with the action (e.g., Flag value, quantity).
        /// </summary>
        public string Value { get; set; } = "";
    }

    /// <summary>
    /// Represents a quest in the game world.
    /// </summary>
    public class Quest
    {
        /// <summary>
        /// Gets or sets the localisation ID for the quest name.
        /// </summary>
        public int NameId { get; set; }

        /// <summary>
        /// Gets or sets the localisation ID for the quest description.
        /// </summary>
        public int DescriptionId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the NPC who gives the quest.
        /// </summary>
        public int GiverId { get; set; }

        /// <summary>
        /// Gets or sets the type of the quest.
        /// </summary>
        public QuestType Type { get; set; }

        /// <summary>
        /// Gets or sets the status of the quest.
        /// </summary>
        public QuestStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the list of objectives for the quest.
        /// </summary>
        public List<QuestObjective> Objectives { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of stages for the quest.
        /// </summary>
        public List<QuestStage> Stages { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of rewards for completing the quest.
        /// </summary>
        public List<Reward> Rewards { get; set; } = [];

        /// <summary>
        /// Gets or sets the flags associated with the quest.
        /// </summary>
        public Dictionary<string, bool> Flags { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of required flags for the quest.
        /// </summary>
        public List<string> RequiredFlags { get; set; } = [];

        /// <summary>
        /// Gets or sets the minimum level required to start the quest.
        /// </summary>
        public int MinLevel { get; set; }

        /// <summary>
        /// Gets or sets the list of prerequisite quests for this quest.
        /// </summary>
        public List<int> PrerequisiteQuests { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of follow-up quests for this quest.
        /// </summary>
        public List<int> FollowUpQuests { get; set; } = [];
    }

    /// <summary>
    /// Represents an objective within a quest.
    /// </summary>
    public class QuestObjective
    {
        /// <summary>
        /// Gets or sets the localisation ID for the objective description.
        /// </summary>
        public int DescriptionId { get; set; }

        /// <summary>
        /// Gets or sets the type of objective (e.g., Collect, Kill, Talk, Explore).
        /// </summary>
        public string Type { get; set; } = "";

        /// <summary>
        /// Gets or sets the target of the objective (e.g., Item ID, NPC ID, Location ID).
        /// </summary>
        public string Target { get; set; } = "";

        /// <summary>
        /// Gets or sets the required quantity for the objective.
        /// </summary>
        public int Required { get; set; }

        /// <summary>
        /// Gets or sets the current progress of the objective.
        /// </summary>
        public int Current { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the objective is complete.
        /// </summary>
        public bool IsComplete { get; set; }

        /// <summary>
        /// Gets or sets the list of rewards for completing the objective.
        /// </summary>
        public List<Reward> Rewards { get; set; } = [];
    }

    /// <summary>
    /// Represents a stage within a quest.
    /// </summary>
    public class QuestStage
    {
        /// <summary>
        /// Gets or sets the localisation ID for the stage description.
        /// </summary>
        public int DescriptionId { get; set; }

        /// <summary>
        /// Gets or sets the references to the dialogue trees associated with the stage.
        /// </summary>
        public List<int> DialogueTreeRefs { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of required flags for the stage.
        /// </summary>
        public List<string> RequiredFlags { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of actions to be performed upon completing the stage.
        /// </summary>
        public List<DialogueAction> CompletionActions { get; set; } = [];
    }

    /// <summary>
    /// Defines the various types of quests available in the game.
    /// </summary>
    public enum QuestType
    {
        /// <summary>
        /// A main storyline quest.
        /// </summary>
        Main,

        /// <summary>
        /// A side quest.
        /// </summary>
        Side,

        /// <summary>
        /// A daily quest.
        /// </summary>
        Daily,

        /// <summary>
        /// A world event quest.
        /// </summary>
        World,

        /// <summary>
        /// A guild-related quest.
        /// </summary>
        Guild,

        /// <summary>
        /// A reputation-based quest.
        /// </summary>
        Reputation
    }

    /// <summary>
    /// Defines the various statuses a quest can have.
    /// </summary>
    public enum QuestStatus
    {
        /// <summary>
        /// The quest is available to be started.
        /// </summary>
        Available,

        /// <summary>
        /// The quest is currently active.
        /// </summary>
        Active,

        /// <summary>
        /// The quest has been completed.
        /// </summary>
        Complete,

        /// <summary>
        /// The quest has failed.
        /// </summary>
        Failed
    }

    /// <summary>
    /// Represents an item in the game world.
    /// </summary>
    public class Item
    {
        /// <summary>
        /// Gets or sets the localisation ID for the item name.
        /// </summary>
        public int NameId { get; set; }

        /// <summary>
        /// Gets or sets the localisation ID for the item description.
        /// </summary>
        public int DescriptionId { get; set; }

        /// <summary>
        /// Gets or sets the stats of the item.
        /// </summary>
        public ItemStats Stats { get; set; } = new();
    }

    /// <summary>
    /// Represents a point of interest along a route.
    /// </summary>
    public class RoutePoint
    {
        /// <summary>
        /// Gets or sets the localisation ID for the route point description.
        /// </summary>
        public int DescriptionId { get; set; }

        /// <summary>
        /// Gets or sets the localisation ID for the directions to the route point.
        /// </summary>
        public int DirectionsId { get; set; }

        /// <summary>
        /// Gets or sets the list of landmarks associated with the route point.
        /// </summary>
        public List<Landmark> Landmarks { get; set; } = [];
    }

    /// <summary>
    /// Represents the stats of an entity.
    /// </summary>
    public class EntityStats
    {
        /// <summary>
        /// Gets or sets the strength stat of the entity.
        /// </summary>
        public int Strength { get; set; }

        /// <summary>
        /// Gets or sets the dexterity stat of the entity.
        /// </summary>
        public int Dexterity { get; set; }

        /// <summary>
        /// Gets or sets the intelligence stat of the entity.
        /// </summary>
        public int Intelligence { get; set; }

        /// <summary>
        /// Gets or sets the defence stat of the entity.
        /// </summary>
        public int Defense { get; set; }

        /// <summary>
        /// Gets or sets the speed stat of the entity.
        /// </summary>
        public int Speed { get; set; }
    }

    /// <summary>
    /// Represents the stats of an item.
    /// </summary>
    public class ItemStats
    {
        /// <summary>
        /// Gets or sets the value of the item.
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// Gets or sets the weight of the item.
        /// </summary>
        public int Weight { get; set; }

        /// <summary>
        /// Gets or sets the durability of the item.
        /// </summary>
        public int Durability { get; set; }

        /// <summary>
        /// Gets or sets the type of the item.
        /// </summary>
        public ItemType Type { get; set; }
    }

    /// <summary>
    /// Defines the various types of items in the game.
    /// </summary>
    public enum ItemType
    {
        /// <summary>
        /// A weapon item.
        /// </summary>
        Weapon,

        /// <summary>
        /// An armour item.
        /// </summary>
        Armor,

        /// <summary>
        /// A consumable item.
        /// </summary>
        Consumable,

        /// <summary>
        /// A quest item.
        /// </summary>
        Quest,

        /// <summary>
        /// A miscellaneous item.
        /// </summary>
        Misc
    }
}