-- Combat message templates
local messages = {
    -- Attack messages
    attack = {
        hit = "%s attacks for %d damage!",
        miss = "%s's attack misses %s!",
        dodge = "%s dodges %s's attack!",
        block = "%s blocks %s's attack!",
        critical = "%s lands a critical hit on %s for %d damage!"
    },
    
    -- Status messages 
    status = {
        dead = "%s has been defeated!",
        stunned = "%s is stunned!",
        poisoned = "%s is poisoned and takes %d damage!",
        bleeding = "%s is bleeding and takes %d damage!",
        no_target = "Attack what?"
    },

    -- Combat state messages
    combat = {
        enter = "%s enters combat with %s!",
        exit = "Combat with %s ends!",
        turn_start = "%s's turn!",
        turn_end = "%s ends their turn."
    }
}

return messages