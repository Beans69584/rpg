local stringutils = {}

function stringutils.trim(s)
    return s:match("^%s*(.-)%s*$")
end

function stringutils.split(str, sep)
    sep = sep or '%s'
    local t = {}
    for field in string.gmatch(str, '[^'..sep..']+') do
        table.insert(t, field)
    end
    return t
end

function stringutils.formatList(items, separator)
    separator = separator or ", "
    return table.concat(items, separator)
end

return stringutils