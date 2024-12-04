return CreateCommand({
    name = "clear",
    description = "Clear the log",
    usage = "clear",
    category = "Chat",
    aliases = {"c"},
    execute = function(args)
        game:ClearLog()
    end
})