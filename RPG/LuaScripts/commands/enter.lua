local core_utils = require("core_utils")

return CreateCommand({
    name = "enter",
    description = "Enter a location or building",
    aliases = {"visit"},
    usage = "enter <location or building name>",
    category = "Navigation",
    execute = function(args, state)
        local currentRegion = game:GetCurrentRegion()
        if not currentRegion then
            game:Log("No world loaded!")
            return
        end

        local currentLocation = game:GetCurrentLocation()
        local targetName = core_utils.parseArgs(args)

        -- If we're in a location, try to enter buildings
        if currentLocation then
            if targetName == "" then
                game:Log("Enter which building?")
                local buildings = game:GetBuildings()
                if #buildings > 0 then
                    game:LogColor("Available buildings:", "Cyan")
                    for _, building in pairs(buildings) do
                        game:Log(string.format("  - %s (%s)", building.name, building.type))
                    end
                else
                    game:Log("There are no buildings here to enter.")
                end
                return
            end

            -- Find matching building and enter it
            local targetBuilding = nil
            local buildings = game:GetBuildings()
            for _, building in pairs(buildings) do
                if building.name:lower() == targetName:lower() then
                    targetBuilding = building
                    break
                end
            end

            if not targetBuilding then
                game:Log("Cannot find building: " .. targetName)
                if #buildings > 0 then
                    game:LogColor("Available buildings:", "Cyan")
                    for _, building in pairs(buildings) do
                        game:Log(string.format("  - %s (%s)", building.name, building.type))
                    end
                end
                return
            end

            -- Enter the building
            game:SetCurrentBuilding(targetBuilding)
            game:Log("")
            game:LogColor("=== " .. targetBuilding.name .. " ===", "Yellow")
            game:Log(targetBuilding.description)
            game:Log("Type: " .. targetBuilding.type)

            -- Show NPCs in building
            local npcs = game:GetNPCsInBuilding(targetBuilding)
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
            local items = game:GetItemsInBuilding(targetBuilding)
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

        -- Not in a location, handle location entry
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
            game:LogColor("Available locations:", "Cyan")
            for _, location in pairs(locations) do
                game:Log(string.format("  - %s (%s)", location.Name, location.Type))
            end
            return
        end

        -- Navigate to the location
        game:NavigateToLocation(targetLocation)
        
        -- Display location information
        game:Log("")
        game:LogColor("=== " .. targetLocation.Name .. " ===", "Yellow")
        game:Log(targetLocation.Description)
        game:Log("Location Type: " .. targetLocation.Type)
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
})
