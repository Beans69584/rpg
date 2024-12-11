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
    name = "leave",
    description = "Leave the current building",
    aliases = {"exit"},
    usage = "leave",
    category = "Navigation",
    execute = function(args, state)
        local currentLocation = game:GetCurrentLocation()
        local currentBuilding = game:GetCurrentBuilding()
        
        if not currentBuilding then
            game:Log("You're not in a building.")
            return
        end
        
        -- Exit the building
        game:SetCurrentBuilding(nil)
        game:Log("You leave the " .. currentBuilding.name)
        
        -- Show location description
        game:Log("")
        game:LogColor("=== " .. currentLocation.Name .. " ===", "Yellow")
        game:Log(currentLocation.Description)
    end
})
