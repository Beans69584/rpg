return {
    name = "Test Middleware Plugin",
    author = "Test",
    version = "1.0.0",
    type = "Middleware",
    initialize = function(self, context)
        context:LogPlugin("Test Middleware", "Initializing middleware plugin")
    end,
    processCommand = function(self, context, command)
        context:LogPlugin("Test Middleware", "Processing command")
        return command
    end,
}