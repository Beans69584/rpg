-- plugins/lua/test_theme.lua
return {
    name = "Test Theme Plugin",
    author = "Test",
    version = "1.0.0",
    type = "Theme",

    initialize = function(self, context)
        context:LogPlugin("Test Theme", "Initializing theme plugin")
        
        local theme = {
            name = "Test Theme",
            borderCharacters = {
                topLeft = "◆",
                topRight = "◆",
                bottomLeft = "◆",
                bottomRight = "◆",
                horizontal = "=",
                vertical = "|"
            },
            colors = {
                border = ConsoleColor.Red,
                title = ConsoleColor.Yellow,
                text = ConsoleColor.Green
            }
        }
        
        context.PluginManager:RegisterTheme(theme)
        context:LogPlugin("Test Theme", "Theme registered successfully")
    end,

    shutdown = function(self, context)
        context:LogPlugin("Test Theme", "Plugin shutting down")
    end
}