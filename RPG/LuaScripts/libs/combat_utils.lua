local utils = {}

function utils.calculateDamage(level, weapon)
    return level * 10 + (weapon and weapon.damage or 0)
end

function utils.isValidTarget(target)
    return target and target ~= ""
end

return utils