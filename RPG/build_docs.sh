#!/bin/bash

set -e

# Default parameters
ENVIRONMENT=${1:-local}
SCRIPT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SOURCE_PATH="$SCRIPT_ROOT/src"
HOST="localhost"
PORT=8080

# Parse named arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --host)
            HOST="$2"
            shift 2
            ;;
        --port)
            if ! [[ $2 =~ ^[0-9]+$ ]] || [ $2 -lt 1 ] || [ $2 -gt 65535 ]; then
                echo "Error: Port must be a number between 1 and 65535"
                exit 1
            fi
            PORT="$2"
            shift 2
            ;;
        --environment|-e)
            ENVIRONMENT="$2"
            shift 2
            ;;
        *)
            shift
            ;;
    esac
done

# Validate environment parameter
if [[ ! "$ENVIRONMENT" =~ ^(local|production|gitlab)$ ]]; then
    echo "Error: Environment must be either 'local', 'production', or 'gitlab'"
    exit 1
fi

# Setup paths
DOCS_PATH="$SCRIPT_ROOT"
LOGS_PATH="$DOCS_PATH/logs"
SITE_PATH="$DOCS_PATH/_site"
PUBLIC_PATH="$SCRIPT_ROOT/public"
CONFIG_PATH="$SCRIPT_ROOT/.config"
TIMESTAMP=$(date '+%Y-%m-%d_%H-%M-%S')
LOG_FILE="$LOGS_PATH/build_$TIMESTAMP.log"

# Initialize logging
write_log() {
    local message="$(date '+%Y-%m-%d %H:%M:%S'): $1"
    echo "$message"
    if [ -f "$LOG_FILE" ]; then
        echo "$message" >> "$LOG_FILE"
    fi
}

# Create required directories
for dir in "$DOCS_PATH" "$LOGS_PATH" "$CONFIG_PATH"; do
    if [ ! -d "$dir" ]; then
        mkdir -p "$dir"
        echo "Created directory: $dir"
    fi
done

# Create log file
touch "$LOG_FILE"
echo "Created log file: $LOG_FILE"

# Clean previous builds
write_log "Cleaning previous builds..."
for path in "$SITE_PATH" "$PUBLIC_PATH"; do
    if [ -d "$path" ]; then
        rm -rf "$path"
        write_log "Cleaned $(basename "$path") directory"
    fi
done

# Clean old logs except current
find "$LOGS_PATH" -type f ! -name "$(basename "$LOG_FILE")" -delete 2>/dev/null || true

# Verify dotnet installation
if ! command -v dotnet >/dev/null 2>&1; then
    write_log "ERROR: dotnet SDK is not installed or not in PATH"
    exit 1
fi

# Main build process
{
    write_log "Starting documentation build process..."

    # Initialize or restore tools
    if [ ! -f "$CONFIG_PATH/dotnet-tools.json" ]; then
        write_log "Initializing dotnet tool manifest..."
        (cd "$CONFIG_PATH" && dotnet new tool-manifest) || exit 1
        write_log "Installing DocFX..."
        (cd "$CONFIG_PATH" && dotnet tool install docfx) || exit 1
    else
        write_log "Restoring tools from manifest..."
        (cd "$CONFIG_PATH" && dotnet tool restore) || exit 1
    fi

    # Generate API metadata
    if [ -d "$SOURCE_PATH" ]; then
        write_log "Generating API metadata..."
        metadata_path="$DOCS_PATH/api"
        mkdir -p "$metadata_path"

        if ! dotnet docfx metadata "$DOCS_PATH/docfx.json" --force; then
            write_log "ERROR: API metadata generation failed"
            exit 1
        fi
        write_log "API metadata generation completed"
    else
        write_log "Warning: Source path not found, skipping API metadata generation"
    fi

    # Build documentation
    write_log "Building documentation..."
    if ! dotnet docfx "$DOCS_PATH/docfx.json"; then
        write_log "ERROR: Documentation build failed"
        exit 1
    fi

    # Handle environment-specific operations
    case "$ENVIRONMENT" in
        "local")
            write_log "Starting local server on $HOST:$PORT..."
            write_log "Press Ctrl+C to stop the server"
            trap 'write_log "Server stopped"; exit 0' INT
            dotnet docfx serve "$SITE_PATH" -n "$HOST" -p "$PORT"
            ;;
        "gitlab")
            write_log "Preparing for GitLab Pages..."
            if [ -d "$SITE_PATH" ]; then
                cp -r "$SITE_PATH" "$PUBLIC_PATH"
                write_log "Copied documentation to public directory for GitLab Pages"
            fi
            ;;
    esac

    write_log "Documentation build completed successfully"
    exit 0

} || {
    write_log "ERROR: Build process failed"
    exit 1
}
