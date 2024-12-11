local core = require("core")

return CreateCommand({
    name = "heal",
    description = "Heal yourself",
    usage = "heal [amount]",
    category = "Combat",
    aliases = {"h"},
    execute = function(args)
        local parsed = parseArgs(args)
        local amount = core.parseNumber(parsed:get(1))
        if amount == nil then
            game:Log("Invalid amount")
        else
            local currentHP = game:GetPlayerHP()
            if currentHP == game:GetPlayerMaxHP() then
                game:Log("You are already at full health")
                return
            end
            if currentHP + amount < 0 then
                game:Log("You can't heal for a negative amount")
                return
            end
            if currentHP + amount > game:GetPlayerMaxHP() then
                amount = game:GetPlayerMaxHP() - currentHP
            end
            game:SetPlayerHP(currentHP + amount)
            game:ClearLog()
            game:Log(core.formatHealthChange(amount, "healed"))
        end
    end
})