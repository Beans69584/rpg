-- Helper function for formatting travel time
local function formatTravelTime(minutes)
    if minutes < 60 then
        return string.format("%d minutes", minutes)
    else
        local hours = math.floor(minutes / 60)
        local mins = minutes % 60
        if mins == 0 then
            return string.format("%d hours", hours)
        else
            return string.format("%d hours and %d minutes", hours, mins)
        end
    end
end

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
        
        if not currentLocation then
            -- Region view
            game:Log("")
            game:LogColor("=== " .. currentRegion.Name .. " ===", "Yellow")
            game:Log(currentRegion.Description)
            
            -- Show available locations with types
            game:Log("")
            game:LogColor("Locations:", "Cyan")
            local locations = game:GetLocationsInRegion()
            for _, location in pairs(locations) do
                game:Log(string.format("  - %s (%s)", location.Name, location.Type))
            end
            
            -- Show connected regions with travel times
            game:Log("")
            game:LogColor("Connected Regions:", "Cyan")
            local connections = game:GetConnectedRegions()
            for _, connection in pairs(connections) do
                local time = game:CalculateTravelTime(currentRegion, connection)
                game:Log(string.format("  - %s (%s away)", 
                    connection.Name, formatTravelTime(time)))
            end
        else
            -- Location view
            game:Log("")
            game:LogColor("=== " .. currentLocation.Name .. " ===", "Yellow")
            
            -- Check if we're in a building
            local currentBuilding = game:GetCurrentBuilding()
            if currentBuilding then
                game:LogColor("=== " .. currentBuilding.name .. " ===", "Yellow")
                game:Log(currentBuilding.description)
                game:Log("Type: " .. currentBuilding.type)

                -- Show NPCs in building
                local npcs = game:GetNPCsInBuilding(currentBuilding)
                if #npcs > 0 then
                    game:Log("")
                    game:LogColor("People here:", "Cyan")
                    for _, npc in pairs(npcs) do
                        game:Log(string.format("  - %s (Level %d)", npc.name, npc.level))
                        local dialogue = game:GetRandomNPCDialogue(npc)
                        game:LogColor('    "' .. dialogue .. '"', "Gray")
                    end
                end

                -- Show items in building
                local items = game:GetItemsInBuilding(currentBuilding)
                if #items > 0 then
                    game:Log("")
                    game:LogColor("Items:", "Cyan")
                    for _, item in pairs(items) do
                        game:Log("  - " .. item.name)
                        game:LogColor("    " .. item.description, "Gray")
                    end
                end
                return
            end

            game:Log(currentLocation.Description)
            game:Log("Location Type: " .. currentLocation.Type)
            game:Log("In " .. currentRegion.Name)

            -- Show NPCs
            local npcs = game:GetNPCsInLocation()
            if #npcs > 0 then
                game:Log("")
                game:LogColor("People here:", "Cyan")
                for _, npc in pairs(npcs) do
                    game:Log(string.format("  - %s (Level %d)", npc.name, npc.level))
                    local dialogue = game:GetRandomNPCDialogue(npc)
                    game:LogColor('    "' .. dialogue .. '"', "Gray")
                end
            end

            -- Show items
            local items = game:GetItemsInLocation()
            if #items > 0 then
                game:Log("")
                game:LogColor("Items:", "Cyan")
                for _, item in pairs(items) do
                    game:Log("  - " .. item.name)
                    game:LogColor("    " .. item.description, "Gray")
                end
            end

            -- Show buildings if this is a settlement
            local buildings = game:GetBuildings()
            if #buildings > 0 then
                game:Log("")
                game:LogColor("Buildings:", "Cyan")
                for _, building in pairs(buildings) do
                    game:Log(string.format("  - %s (%s)", building.name, building.type))
                    game:LogColor("    " .. building.description, "Gray")
                end
            end
        end
    end
})
