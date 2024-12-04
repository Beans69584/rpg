local function trim(s)
    return s:match("^%s*(.-)%s*$")
end

return CreateCommand({
    name = "rename",
    description = "Rename the player",
    aliases = {"name"},
    usage = "rename",
    category = "Player",
    execute = function(args, state)
        local newName = trim(game:AskQuestion("What would you like to be called?"))
        if newName == "" then
            game:Log("You must enter a name!")
            return
        end

        game:SetPlayerName(newName)
        game:Log("You are now known as " .. newName)
    end
})