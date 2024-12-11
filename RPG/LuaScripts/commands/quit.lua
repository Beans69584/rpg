return CreateCommand({
    name = "exit",
    description = "Exit the game",
    usage = "exit",
    category = "Management",
    aliases = {"q", "quit", "e"},
    execute = function(args, state)
        state.Running = false
    end
})