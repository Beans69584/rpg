return CreateCommand({
    name = "look",
    description = "Examine your surroundings",
    aliases = {"examine", "l"},
    usage = "look",
    category = "Navigation",
    execute = function(args, state)
        local currentRegion = game:GetCurrentRegion()
        if not currentRegion then
            game:Log("No world loaded!")
            return
        end

        local currentLocation = game:GetCurrentLocation()
        
        -- Show region info if not in a specific location
        if not currentLocation then
            game:Log("")
            game:Log("You are in " .. currentRegion.Name)
            game:Log(currentRegion.Description)
            
            -- Show available locations
            game:Log("")
            game:Log("You see these locations:")
            local locations = game:GetLocationsInRegion()
            for _, location in pairs(locations) do
                game:Log("  - " .. location.Name)
            end
            
            -- Show connected regions
            game:Log("")
            game:Log("You can travel to:")
            local connections = game:GetConnectedRegions()
            for _, connection in pairs(connections) do
                game:Log("  - " .. connection.Name)
            end
        else
            -- Show location-specific info
            game:Log("")
            game:Log("You are at " .. currentLocation.Name)
            game:Log(currentLocation.Description)
            game:Log("(In " .. currentRegion.Name .. ")")
        end
    end
})
