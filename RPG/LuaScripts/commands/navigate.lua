-- Utility function to remove leading and trailing whitespace from a string
-- @param s (string) The input string to trim
-- @return (string) The trimmed string with whitespace removed from both ends
local function trim(s)
    return s:match("^%s*(.-)%s*$")
end

-- Formats a duration in minutes into a human-readable string
-- @param minutes (number) The number of minutes to format
-- @return (string) A formatted string like "X minutes" or "X hours and Y minutes"
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

-- Creates and returns a command object for handling player movement between regions
-- This command allows players to travel between connected regions in the game world
return CreateCommand({
    -- Basic command properties
    name = "go",                     -- Primary command name
    description = "Travel to a connected region", -- Command description
    aliases = {"move", "travel"},    -- Alternative command names
    usage = "go <region name>",      -- Usage instructions
    category = "Navigation",         -- Command category for help systems

    -- Main command execution function
    -- @param args (string) The arguments passed to the command (region name)
    -- @param state (table) The current game state
    execute = function(args, state)
        -- Check if a world is currently loaded
        if not game:GetCurrentRegion() then
            game:Log("No world loaded!")
            return
        end

        -- Clear current location if player is in one
        if game:GetCurrentLocation() then
            game:SetCurrentLocation(nil)
        end

        -- Process and validate the target region name
        local targetName = trim(args)
        if targetName == "" then
            game:Log("Where do you want to go?")
            return
        end

        -- Get list of connected regions and find the target
        local connections = game:GetConnectedRegions()
        local currentRegion = game:GetCurrentRegion()
        local targetRegion = nil
        local availableRegions = {}

        -- Search for matching region and build list of available destinations
        for _, region in pairs(connections) do
            table.insert(availableRegions, region.Name)
            if game:RegionNameMatches(region, targetName) then
                targetRegion = region
                break
            end
        end

        -- Handle case where target region wasn't found
        if not targetRegion then
            game:Log("Cannot travel to '" .. targetName .. "'")
            game:Log("Available destinations:")
            for _, name in ipairs(availableRegions) do
                game:Log(" - " .. name)
            end
            return
        end

        -- Get the route information
        local route = game:GetRoute(currentRegion, targetRegion)
        
        -- Define consistent border width
        local borderWidth = 40
        local function makeBorder(symbol, color)
            game:LogColor("+" .. string.rep(symbol, borderWidth - 2) .. "+", color)
        end

        local function makeTextLine(text, color)
            local padding = borderWidth - #text - 4
            game:LogColor("| " .. text .. string.rep(" ", padding) .. " |", color)
        end

        -- Start the journey
        makeBorder("-", "Cyan")
        makeTextLine("Beginning journey to " .. targetRegion.Name, "Cyan")
        makeBorder("-", "Cyan")
        game:Log("")

        -- Show route details with improved formatting
        for _, point in pairs(route) do
            game:LogColor("-> " .. point.description, "Yellow")
            game:LogColor("   " .. point.directions, "Gray")
            
            -- Check for landmarks with improved visibility
            local landmarks = game:GetLandmarksAtPoint(point)
            if #landmarks > 0 then
                game:LogColor("   Landmarks:", "Magenta")
                for _, landmark in pairs(landmarks) do
                    local color = "White"
                    if landmark.type == "Settlement" then
                        color = "Green"
                    elseif landmark.type == "Danger" then
                        color = "Red"
                    elseif landmark.type == "Interest" then
                        color = "Yellow"
                    end
                    game:LogColor("   * " .. landmark.name .. ": " .. landmark.description, color)
                end
            end
            
            game:Log("")
            game:Sleep(1500) -- Slightly longer pause for readability
        end

        -- Simulate travel time with the improved progress bar
        local travelTime = game:CalculateTravelTime(currentRegion, targetRegion)
        game:SimulateTravelTimeWithProgress(travelTime)

        -- Arrival message (matched with look.lua style)
        game:Log("")
        game:LogColor("=== " .. targetRegion.Name .. " ===", "Yellow")
        game:Log(targetRegion.Description)
        
        -- Show available locations
        game:Log("")
        game:LogColor("Locations:", "Cyan")
        local locations = game:GetLocationsInRegion()
        for _, location in pairs(locations) do
            local icon = "*"
            if location.Type == "Settlement" then icon = "H"
            elseif location.Type == "Dungeon" then icon = "D"
            elseif location.Type == "Shop" then icon = "S"
            end
            game:Log(string.format("  - %s (%s)", location.Name, location.Type))
        end
        
        -- Show connected regions
        game:Log("")
        game:LogColor("Connected Regions:", "Cyan")
        local newConnections = game:GetConnectedRegions()
        for _, connection in pairs(newConnections) do
            local time = game:CalculateTravelTime(targetRegion, connection)
            game:Log(string.format("  - %s (%s away)", 
                connection.Name, formatTravelTime(time)))
        end

        -- Update the current region after successful navigation
        game:SetCurrentRegion(targetRegion)

    end
})