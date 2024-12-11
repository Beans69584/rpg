local function trim(s)
    return s:match("^%s*(.-)%s*$")
end

return CreateCommand({
    name = "interact",
    description = "Interact with an NPC or item in the current building",
    aliases = {"talk", "use"},
    usage = "interact <npc or item name>",
    category = "Interaction",
    execute = function(args, state)
        local currentBuilding = game:GetCurrentBuilding()
        if not currentBuilding then
            game:Log("You need to be in a building to interact with things.")
            return
        end

        local targetName = trim(args)
        if targetName == "" then
            game:Log("What do you want to interact with?")
            return
        end

        -- Try to find matching NPC
        local npcs = game:GetNPCsInBuilding(currentBuilding)
        game:Log(string.format("Debug: Found %d NPCs to check", #npcs))
        
        for _, npc in pairs(npcs) do
            game:Log(string.format("Debug: Checking NPC: '%s' against target: '%s'", 
                npc.name:lower(), targetName:lower()))
                
            -- More flexible name matching
            if npc.name:lower():find(targetName:lower(), 1, true) then
                -- Handle NPC interaction based on building type
                game:LogColor("=== Talking to " .. npc.name .. " ===", "Yellow")
                
                if currentBuilding.type == "Inn" then
                    game:Log("The innkeeper can provide you with:")
                    game:LogColor("1. Rest (10 gold)", "Cyan")
                    game:LogColor("2. Meal (5 gold)", "Cyan")
                    game:LogColor("3. Information", "Cyan")
                    
                    local choice = game:AskQuestion("What would you like? (1-3)")
                    if choice == "1" then
                        game:Sleep(1000)
                        game:LogColor("You feel well-rested!", "Green")
                        game:SetPlayerHP(game:GetPlayerMaxHP())
                        game:TakeGold(10)
                    elseif choice == "2" then
                        game:Sleep(500)
                        game:LogColor("The meal was satisfying.", "Green")
                    elseif choice == "3" then
                        game:LogColor(game:GetRandomNPCDialogue(npc), "White")
                    end
                    
                elseif currentBuilding.type == "Blacksmith" then
                    game:Log("The blacksmith offers to:")
                    game:LogColor("1. Repair equipment", "Cyan")
                    game:LogColor("2. Sell weapons", "Cyan")
                    game:LogColor("3. Buy materials", "Cyan")
                    
                    local choice = game:AskQuestion("What would you like? (1-3)")
                    if choice == "1" then
                        game:LogColor("Your equipment has been repaired.", "Green")
                    elseif choice == "2" then
                        game:Log("Available weapons:")
                        local items = game:GetItemsInBuilding(currentBuilding)
                        for i, item in pairs(items) do
                            game:LogColor(i .. ". " .. item.name, "Cyan")
                        end
                    elseif choice == "3" then
                        game:LogColor("The blacksmith will buy any metals you have.", "White")
                    end
                    
                elseif currentBuilding.type == "Temple" then
                    game:Log("The priest offers to:")
                    game:LogColor("1. Heal wounds (donation)", "Cyan")
                    game:LogColor("2. Bless", "Cyan")
                    game:LogColor("3. Seek guidance", "Cyan")
                    
                    local choice = game:AskQuestion("What would you like? (1-3)")
                    if choice == "1" then
                        game:Sleep(1000)
                        game:LogColor("Your wounds are healed!", "Green")
                        game:SetPlayerHP(game:GetPlayerMaxHP())
                    elseif choice == "2" then
                        game:LogColor("You feel blessed.", "Green")
                    elseif choice == "3" then
                        game:LogColor(game:GetRandomNPCDialogue(npc), "White")
                    end
                    
                else
                    -- Generic NPC interaction
                    game:LogColor(game:GetRandomNPCDialogue(npc), "White")
                end
                return
            end
        end

        -- Try to find matching item
        local items = game:GetItemsInBuilding(currentBuilding)
        for _, item in pairs(items) do
            if item.name:lower() == targetName:lower() then
                game:LogColor("=== Examining " .. item.name .. " ===", "Yellow")
                game:Log(item.description)
                
                if currentBuilding.type == "Shop" or 
                   currentBuilding.type == "Market" or 
                   currentBuilding.type == "Trading Post" then
                    game:LogColor("This item is for sale.", "Cyan")
                elseif currentBuilding.type == "Blacksmith" then
                    game:LogColor("The blacksmith can improve this item.", "Cyan")
                end
                return
            end
        end

        game:Log("Cannot find '" .. targetName .. "' to interact with.")
    end
})
