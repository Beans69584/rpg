local calculations = {
    -- Calculate base damage 
    calculate_damage = function(attacker, defender)
        local base_damage = attacker.attack - defender.defense
        if base_damage < 0 then base_damage = 0 end
        return base_damage
    end,

    -- Calculate if attack hits
    calculate_hit = function(attacker, defender)
        local hit_chance = attacker.accuracy - defender.evasion
        if hit_chance < 10 then hit_chance = 10 end
        if hit_chance > 95 then hit_chance = 95 end
        
        return math.random(1, 100) <= hit_chance
    end,

    -- Calculate critical hit
    calculate_critical = function(attacker)
        local crit_chance = attacker.crit_rate or 5
        if crit_chance > 50 then crit_chance = 50 end
        
        return math.random(1, 100) <= crit_chance
    end,

    -- Apply damage modifiers (like weapon type effectiveness, status effects, etc)
    apply_modifiers = function(base_damage, attacker, defender)
        local final_damage = base_damage
        
        -- Critical hit bonus
        if calculations.calculate_critical(attacker) then
            final_damage = final_damage * 1.5
        end
        
        -- TODO: Add other modifiers like:
        -- Weapon effectiveness
        -- Status effect modifiers
        -- Equipment bonuses
        -- etc.
        
        return math.floor(final_damage)
    end
}

return calculations