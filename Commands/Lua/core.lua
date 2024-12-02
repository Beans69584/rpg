local core = {}

-- Player related helpers
function core.getPlayerInfo()
    return {
        name = game:GetPlayerName(),
        hp = game:GetPlayerHP(),
        maxHp = game:GetPlayerMaxHP(),
        level = game:GetPlayerLevel()
    }
end

-- Argument handling helpers
function core.parseNumber(value, default)
    local num = tonumber(value)
    return num or default
end

-- String utilities
function core.formatHealthChange(amount, action)
    local player = core.getPlayerInfo()
    return string.format("%s %s %d health. Current HP: %d/%d",
        player.name,
        action,
        amount,
        player.hp,
        player.maxHp)
end

return core
