return CreateCommand({
    name = "say",
    description = "Say something",
    usage = "say [message]",
    category = "Chat",
    aliases = {"s"},
    execute = function(args, state)
        if args == "" then
            state.GameLog:Add("Say what?")
        else
            state.GameLog:Add(string.format("%s says: %s", state.PlayerName, args))
        end
    end
})