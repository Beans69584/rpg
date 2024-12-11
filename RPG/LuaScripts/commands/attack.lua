local messages = require("combat_helpers.messages")
local calc = require("combat_helpers.calculations")

return CreateCommand({
    name = "attack",
    description = "Attack a target",
    usage = "attack <target>",
    category = "Combat",
    execute = function(args, state)
        local target = args:match("^%s*(.-)%s*$")
        if target == "" then
            game:Log(messages.status.no_target)
            return
        end

        local damage = calc.calculateDamage(game:GetPlayerLevel())
        game:Log(string.format(messages.attack.hit, target, damage))
    end
})