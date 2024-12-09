return CreateCommand({
    name = "leave",
    description = "Exit the current location",
    aliases = {"exit"},
    usage = "leave",
    category = "Navigation",
    execute = function(args, state)
        if not game:GetCurrentLocation() then
            game:Log("You are not in any location.")
            return
        end

        game:SetCurrentLocation(nil)
        game:Log("You leave the location.")
        
        -- Show region info and available locations
        local region = game:GetCurrentRegion()
        if region then
            game:Log("")
            game:Log("You are in " .. region.Name)
            game:Log(region.Description)
            
            game:Log("")
            game:Log("You see these locations:")
            local locations = game:GetLocationsInRegion()
            for _, location in pairs(locations) do
                game:Log("  - " .. location.Name)
            end
        end
    end
})
