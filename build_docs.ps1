[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('local', 'production', 'gitlab')]
    [string]$Environment = 'local'
)

# Setup base paths and logging
$scriptRoot = $PSScriptRoot
$docsPath = Join-Path $scriptRoot "docs"
$logsPath = Join-Path $docsPath "logs"
$sitePath = Join-Path $docsPath "_site"
$publicPath = Join-Path $scriptRoot "public"
$configPath = Join-Path $scriptRoot ".config"
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$logFile = Join-Path $logsPath "build_$timestamp.log"

# Initialize logging
function Write-Log {
    param($Message)
    $logMessage = "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss'): $Message"
    Write-Host $logMessage
    if (Test-Path $logFile) {
        Add-Content -Path $logFile -Value $logMessage -ErrorAction Stop
    }
}

# Ensure directories exist
try {
    $paths = @($docsPath, $logsPath, $configPath)
    foreach ($path in $paths) {
        if (-not (Test-Path $path)) {
            New-Item -ItemType Directory -Force -Path $path | Out-Null
            Write-Host "Created directory: $path"
        }
    }
    
    # Create log file
    if (-not (Test-Path $logFile)) {
        New-Item -ItemType File -Force -Path $logFile | Out-Null
        Write-Host "Created log file: $logFile"
    }
} catch {
    Write-Error "Failed to create required paths: $_"
    exit 1
}

# Clean previous builds (excluding current log file)
try {
    Write-Log "Cleaning previous builds..."
    foreach ($path in @($sitePath, $publicPath)) {
        if (Test-Path $path) {
            Remove-Item -Path $path -Recurse -Force
            Write-Log "Cleaned $(Split-Path $path -Leaf) directory"
        }
    }
    
    Get-ChildItem -Path $logsPath -File | 
    Where-Object { $_.FullName -ne $logFile } | 
    ForEach-Object {
        Remove-Item $_.FullName -Force
        Write-Log "Cleaned old log: $($_.Name)"
    }
} catch {
    Write-Log "Warning: Cleanup failed: $_"
}

try {
    Write-Log "Starting documentation build process..."

    # Verify dotnet is installed
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw "dotnet SDK is not installed or not in PATH"
    }

    # Ensure tool manifest exists and restore tools
    if (-not (Test-Path (Join-Path $configPath "dotnet-tools.json"))) {
        Write-Log "Initializing dotnet tool manifest..."
        dotnet new tool-manifest
        if ($LASTEXITCODE -ne 0) { 
            throw "Failed to initialize tool manifest"
        }
        Write-Log "Installing DocFX..."
        dotnet tool install docfx
        if ($LASTEXITCODE -ne 0) { 
            throw "Failed to install DocFX"
        }
    } else {
        Write-Log "Restoring tools from manifest..."
        dotnet tool restore
        if ($LASTEXITCODE -ne 0) { 
            throw "Failed to restore tools"
        }
    }

    # Build the documentation
    Write-Log "Building documentation..."
    $buildResult = dotnet docfx (Join-Path $docsPath "docfx.json")
    if ($LASTEXITCODE -ne 0) { 
        throw "Documentation build failed: $buildResult"
    }

    # Handle environment-specific operations
    switch ($Environment) {
        'local' {
            Write-Log "Starting local server..."
            Write-Log "Press Ctrl+C to stop the server"
            try {
                & dotnet docfx serve $sitePath
            } catch {
                Write-Log "Server stopped: $_"
            }
        }
        'gitlab' {
            Write-Log "Preparing for GitLab Pages..."
            if (Test-Path $sitePath) {
                Copy-Item -Path $sitePath -Destination $publicPath -Recurse -Force
                Write-Log "Copied documentation to public directory for GitLab Pages"
            }
        }
    }

    Write-Log "Documentation build completed successfully"
    exit 0

} catch {
    Write-Log "ERROR: $_"
    exit 1
}