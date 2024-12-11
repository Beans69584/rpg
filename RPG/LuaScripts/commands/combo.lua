local core_utils = require("core_utils")

return CreateCommand({
    name = "combo",
    description = "Perform a combination of attacks",
    usage = "combo <target>",
    category = "Combat",
    execute = function(args, state)
        local target = core_utils.parseArgs(args)
        if target == "" then
            game:Log("Combo attack what?")
            return
        end

        -- Execute a sequence of attacks
        game:ExecuteCommand("attack", target)
        game:Sleep(500)  -- Small delay between attacks
        game:ExecuteCommand("fireball", target)
    end
})