local core_utils = {}

function core_utils.parseArgs(input)
    local args = {}
    if not input or input == "" then
        return args
    end

    -- Handle quoted arguments and regular space-separated args
    local pattern = '"([^"]+)"|([^%s]+)'
    for quoted, unquoted in input:gmatch(pattern) do
        table.insert(args, quoted or unquoted)
    end
    return args
end

function core_utils.parseNumber(value, default)
    local num = tonumber(value)
    return num or default
end

function core_utils.getArg(args, index, default)
    return args[index] or default
end

return core_utils
