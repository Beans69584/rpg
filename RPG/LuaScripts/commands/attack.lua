local messages = require("combat_helpers.messages")
local calc = require("combat_helpers.calculations")
local core_utils = require("core_utils")

return CreateCommand({
    name = "attack",
    description = "Attack a target",
    usage = "attack <target>",
    category = "Combat",
    execute = function(args, state)
        local parsedArgs = core_utils.parseArgs(args)
        local target = core_utils.getArg(parsedArgs, 1)
        
        if not target then
            game:Log(messages.status.no_target)
            return
        end

        local damage = calc.calculateDamage(game:GetPlayerLevel())
        game:Log(string.format(messages.attack.hit, target, damage))
    end
})