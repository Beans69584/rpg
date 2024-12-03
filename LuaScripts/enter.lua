return CreateCommand({
    name = "enter",
    description = "Enter a location in the current region",
    aliases = {"visit"},
    usage = "enter <location name>",
    category = "Navigation",
    execute = function(args, state)
        local currentRegion = game:GetCurrentRegion()
        if not currentRegion then
            game:Log("No region loaded!")
            return
        end

        local targetName = args:match("^%s*(.-)%s*$")
        if targetName == "" then
            game:Log("Enter where?")
            game:Log("Available locations:")
            local locations = game:GetLocationsInRegion()
            for _, location in pairs(locations) do
                game:Log("  - " .. location.Name)
            end
            return
        end

        local targetLocation = nil
        local locations = game:GetLocationsInRegion()
        for _, location in pairs(locations) do
            if game:LocationNameMatches(location, targetName) then
                targetLocation = location
                break
            end
        end

        if not targetLocation then
            game:Log("Cannot find location: " .. targetName)
            game:Log("Available locations:")
            for _, location in pairs(locations) do
                game:Log("  - " .. location.Name)
            end
            return
        end

        game:NavigateToLocation(targetLocation)
    end
})
