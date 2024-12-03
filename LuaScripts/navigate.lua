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

        -- Calculate and simulate travel time between regions
        local travelTime = game:CalculateTravelTime(currentRegion, targetRegion)
        game:SimulateTravelTimeWithProgress(travelTime)

        -- Update player position and display new region information
        game:SetCurrentRegion(targetRegion)
        game:Log("You arrive at " .. targetRegion.Name)
        game:Log(targetRegion.Description)
        game:Log("")

        -- Display available locations in the new region
        game:Log("You see these locations:")
        local locations = game:GetLocationsInRegion()
        for _, location in pairs(locations) do
            game:Log(" - " .. location.Name)
        end
        game:Log("")

        -- Display new connected regions
        game:Log("From here you can travel to:")
        local newConnections = game:GetConnectedRegions()
        for _, connection in pairs(newConnections) do
            game:Log(" - " .. connection.Name)
        end
    end
})