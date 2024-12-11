local core = require("core")

return CreateCommand({
    name = "damage",
    description = "Damage yourself",
    usage = "damage [amount]",
    category = "Combat",
    aliases = {"d"},
    execute = function(args)
        local parsed = parseArgs(args)
        local amount = core.parseNumber(parsed:get(1))
        if amount == nil then
            game:Log("Invalid amount")
        else
            local currentHP = game:GetPlayerHP()
            game:SetPlayerHP(currentHP - amount)
            game:Log(core.formatHealthChange(amount, "took"))
        end
    end
})