-- Add trim helper function
local function trim(s)
    return s:match("^%s*(.-)%s*$")
end

-- Format time helper function
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
    name = "go",
    description = "Travel to a connected region",
    aliases = {"move", "travel"},
    usage = "go <region name>",
    category = "Navigation",
    execute = function(args, state)
        if not game:GetCurrentRegion() then
            game:Log("No world loaded!")
            return
        end

        -- Clear current location when traveling between regions
        if game:GetCurrentLocation() then
            game:SetCurrentLocation(nil)
        end

        local targetName = trim(args)
        if targetName == "" then
            game:Log("Where do you want to go?")
            return
        end

        local connections = game:GetConnectedRegions()
        local currentRegion = game:GetCurrentRegion()
        local targetRegion = nil
        local availableRegions = {}

        -- Find matching region using pairs() for Lua table iteration
        for _, region in pairs(connections) do
            table.insert(availableRegions, region.Name)
            
            if game:RegionNameMatches(region, targetName) then
                targetRegion = region
                break
            end
        end

        if not targetRegion then
            game:Log("Cannot travel to '" .. targetName .. "'")
            game:Log("Available destinations:")
            for _, name in ipairs(availableRegions) do
                game:Log("  - " .. name)
            end
            return
        end

        -- Calculate travel time
        local travelTime = game:CalculateTravelTime(currentRegion, targetRegion)
        
        -- Simulate travel time with progress UI (using default timeScale)
        game:SimulateTravelTimeWithProgress(travelTime)

        -- Update current region
        game:SetCurrentRegion(targetRegion)

        -- Show arrival information
        game:Log("You arrive at " .. targetRegion.Name)
        game:Log(targetRegion.Description)
        
        -- Show available locations
        game:Log("")
        game:Log("You see these locations:")
        local locations = game:GetLocationsInRegion()
        for _, location in pairs(locations) do
            game:Log("  - " .. location.Name)
        end
        
        -- Show new connections
        game:Log("")
        game:Log("From here you can travel to:")
        local newConnections = game:GetConnectedRegions()
        for _, connection in pairs(newConnections) do
            game:Log("  - " .. connection.Name)
        end
    end
})
